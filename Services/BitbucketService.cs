using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Autoroute.Models;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Environment.Extensions;
using OrchardHUN.ExternalPages.Models;
using Piedone.HelpfulLibraries.Tasks.Jobs;
using RestSharp;
using Orchard.Core.Title.Models;
using Orchard.Core.Common.Models;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace OrchardHUN.ExternalPages.Services
{

    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketService : IBitbucketService
    {
        private const string Industry = "OrchardHUN.ExternalPages.Bitbucket.Changesets";
        private readonly IRepository<BitbucketRepositorySettingsRecord> _repository;
        private readonly IJobManager _jobManager;
        private readonly IContentManager _contentManager;

        public IRepository<BitbucketRepositorySettingsRecord> SettingsRepository { get { return _repository; } }


        public BitbucketService(
            IRepository<BitbucketRepositorySettingsRecord> repository,
            IJobManager jobManager,
            IContentManager contentManager)
        {
            _jobManager = jobManager;
            _repository = repository;
            _contentManager = contentManager;
        }


        public void Populate(int repositoryId)
        {
            var settings = GetRepositorySettingsOrThrow(repositoryId);

            if (!String.IsNullOrEmpty(settings.LastNode)) throw new InvalidOperationException("The repository with the id " + repositoryId + " is already populated.");

            var changesetRestObjects = PrepareRest(settings, "changesets?limit=1");
            var changesetResponse = changesetRestObjects.Client.Execute<ChangesetsResponse>(changesetRestObjects.Request);
            ThrowIfBadResponse(changesetRestObjects.Request, changesetResponse);
            var lastChangeset = changesetResponse.Data.Changesets.FirstOrDefault();

            if (lastChangeset == null) return;

            Func<string, List<FolderSrcFile>> recursivelyFetchFileList = null;
            recursivelyFetchFileList =
                (path) =>
                {
                    var restObjects = PrepareRest(settings, "src/" + lastChangeset.Revision + "/" + path);
                    var response = restObjects.Client.Execute<FolderSrcResponse>(restObjects.Request);
                    var responseData = response.Data;
                    ThrowIfBadResponse(restObjects.Request, response);

                    if (responseData.Directories.Count == 0) return responseData.Files;

                    var files = new List<FolderSrcFile>();
                    foreach (var directory in responseData.Directories)
                    {
                        files.AddRange(recursivelyFetchFileList(path + directory + "/"));
                    }
                    files.AddRange(responseData.Files);
                    return files;
                };

            foreach (var mapping in settings.UrlMappings())
            {
                var files = recursivelyFetchFileList(mapping.RepoPath + "/");

                var jobFiles = new List<UpdateJobFile>(files.Count);
                foreach (var file in files)
                {
                    jobFiles.Add(new UpdateJobFile(file.Path, UpdateJobfileType.Added));
                }

                var jobContext = new UpdateJobContext(
                                    repositoryId,
                                    lastChangeset.Revision,
                                    jobFiles);

                _jobManager.CreateJob(Industry, jobContext);
            }

            settings.LastNode = lastChangeset.Node;
        }

        public void CheckChangesets(int repositoryId)
        {
            var settings = GetRepositorySettingsOrThrow(repositoryId);

            if (String.IsNullOrEmpty(settings.LastNode)) throw new InvalidOperationException("The repository with the id " + repositoryId + " should be populated first.");

            var restObjects = PrepareRest(settings, "changesets?limit=50");

            var response = restObjects.Client.Execute<ChangesetsResponse>(restObjects.Request);
            ThrowIfBadResponse(restObjects.Request, response);

            var changesets = response.Data.Changesets;

            var lastChangeset = changesets.Where(changeset => changeset.Node == settings.LastNode).SingleOrDefault();
            if (lastChangeset != null)
            {
                var lastChangesetIndex = changesets.IndexOf(lastChangeset);
                changesets.RemoveRange(0, lastChangesetIndex + 1);
            }

            if (changesets.Count == 0) return;

            settings.LastNode = changesets.Last().Node;

            var urlMappings = settings.UrlMappings();

            foreach (var changeset in changesets.Where(changeset => changeset.Branch == "default"))
            {
                var files = changeset.Files.Where(file => urlMappings.Any(mapping => file.File.StartsWith(mapping.RepoPath)));
                var fileCount = files.Count();

                if (fileCount != 0)
                {
                    var jobFiles = new List<UpdateJobFile>(fileCount);
                    foreach (var file in files)
                    {
                        var type = UpdateJobfileType.Added;
                        if (file.Type == "modified") type = UpdateJobfileType.Modified;
                        else if (file.Type == "removed") type = UpdateJobfileType.Removed;

                        jobFiles.Add(new UpdateJobFile(file.File, type));
                    }

                    var jobContext = new UpdateJobContext(
                                        repositoryId,
                                        changeset.Revision,
                                        jobFiles);

                    _jobManager.CreateJob(Industry, jobContext);
                }
            }
        }

        public void ProcessNextPendingChangeset()
        {
            var job = _jobManager.TakeJob(Industry);

            if (job == null) return;

            var jobContext = job.Context<UpdateJobContext>();

            var settings = _repository.Get(jobContext.RepositoryId);

            if (settings == null)
            {
                // Repository was deleted since the job was created
                _jobManager.Done(job);
                return;
            }

            var fileProcessor = new FileProcessor(this, settings, jobContext);
            foreach (var file in jobContext.Files)
            {
                fileProcessor.Process(file);
            }

            _jobManager.Done(job);
        }


        #region BitbucketAPIClasses
        public class ChangesetsResponse
        {
            /// <remarks>
            /// Changesets are ordered by date, so the first one is the oldest
            /// </remarks>
            public List<Changeset> Changesets { get; set; }
        }

        public class Changeset
        {
            public string Branch { get; set; }
            public int Revision { get; set; }
            public string Node { get; set; }
            public DateTime UtcTimestamp { get; set; }
            public List<ChangesetFile> Files { get; set; }
        }

        public class ChangesetFile
        {
            public string Type { get; set; }
            public string File { get; set; }
        }

        public class FileSrcResponse
        {
            public string Node { get; set; }
            public string Data { get; set; }
        }

        public class FolderSrcResponse
        {
            public string Node { get; set; }
            public List<string> Directories { get; set; }
            public List<FolderSrcFile> Files { get; set; }
        }

        public class FolderSrcFile
        {
            public int Size { get; set; }
            public string Path { get; set; }
            public DateTime UtcTimestamp { get; set; }
        }
        #endregion

        #region JobClasses
        public class UpdateJobContext
        {
            public int RepositoryId { get; private set; }
            public int Revision { get; private set; }
            public IEnumerable<UpdateJobFile> Files { get; set; }

            public UpdateJobContext(int repositoryId, int revision, IEnumerable<UpdateJobFile> files)
            {
                RepositoryId = repositoryId;
                Revision = revision;
                Files = files;
            }
        }

        public class UpdateJobFile
        {
            public string Path { get; private set; }
            public UpdateJobfileType Type { get; private set; }

            public UpdateJobFile(string path, UpdateJobfileType type)
            {
                Path = path;
                Type = type;
            }
        }

        public enum UpdateJobfileType
        {
            Added,
            Modified,
            Removed
        }
        #endregion


        private BitbucketRepositorySettingsRecord GetRepositorySettingsOrThrow(int id)
        {
            var repository = _repository.Get(id);
            if (repository == null) throw new ArgumentException("No repository exists with the following id: " + id);
            return repository;
        }

        private static RestObjects PrepareRest(BitbucketRepositorySettingsRecord settings, string path)
        {
            var client = new RestClient("https://api.bitbucket.org/1.0/");
            if (!String.IsNullOrEmpty(settings.Username)) client.Authenticator = new HttpBasicAuthenticator(settings.Username, settings.Password);
            var request = new RestRequest("repositories/" + settings.AccountName + "/" + settings.Slug + "/" + path);
            return new RestObjects(client, request);
        }


        private class RestObjects
        {
            public RestClient Client { get; private set; }
            public RestRequest Request { get; private set; }

            public RestObjects(RestClient client, RestRequest request)
            {
                Client = client;
                Request = request;
            }
        }

        private static void ThrowIfBadResponse(RestRequest request, IRestResponse response)
        {
            if (response.ResponseStatus == ResponseStatus.TimedOut)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " timed out.", response.ErrorException);

            if (response.ResponseStatus == ResponseStatus.Error)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " failed with the following status: " + response.StatusDescription, response.ErrorException);

            if (response.ResponseStatus == ResponseStatus.Aborted)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " was aborted.", response.ErrorException);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " is unauthorized.", response.ErrorException);
        }

        private class FileProcessor
        {
            private readonly BitbucketService _service;
            private readonly BitbucketRepositorySettingsRecord _settings;
            private readonly IEnumerable<UrlMapping> _urlMappings;
            private readonly UpdateJobContext _jobContext;


            public FileProcessor(
                BitbucketService service,
                BitbucketRepositorySettingsRecord settings,
                UpdateJobContext jobContext)
            {
                _service = service;
                _settings = settings;
                _urlMappings = settings.UrlMappings();
                _jobContext = jobContext;
            }


            public void Process(UpdateJobFile file)
            {
                // Only dealing with Markdown files currently
                if (!file.Path.EndsWith(".md")) return;

                var mapping = _urlMappings.Where(urlMapping => file.Path.StartsWith(urlMapping.RepoPath)).FirstOrDefault();
                if (mapping == null) return;

                var localPath = file.Path;
                if (!String.IsNullOrEmpty(mapping.RepoPath)) localPath = localPath.Replace(mapping.RepoPath, mapping.LocalPath);
                else localPath = mapping.LocalPath + "/" + localPath;
                localPath = localPath.Replace("Index", "").Replace(".md", "");

                ContentItem page = null;

                if (file.Type == UpdateJobfileType.Added || file.Type == UpdateJobfileType.Modified)
                {
                    var restObjects = PrepareRest(_settings, "src/" + _jobContext.Revision + "/" + file.Path);
                    var response = restObjects.Client.Execute<FileSrcResponse>(restObjects.Request);
                    BitbucketService.ThrowIfBadResponse(restObjects.Request, response);

                    var src = response.Data;

                    if (file.Type == UpdateJobfileType.Modified) page = FetchPage(file.Path);

                    var isNew = page == null;

                    if (isNew)
                    {
                        page = _service._contentManager.New(WellKnownConstants.RepoPageContentType);

                        var autoroutePart = page.As<AutoroutePart>();
                        autoroutePart.CustomPattern = localPath;
                        autoroutePart.UseCustomPattern = true;
                        autoroutePart.DisplayAlias = localPath;
                        page.As<MarkdownPagePart>().RepoPath = file.Path;
                    }

                    page.As<MarkdownPagePart>().Text = src.Data;

                    // Searching for the (first) title in the markdown text
                    var lines = Regex.Split(src.Data, "\r\n|\r|\n");
                    int i = 1;
                    var titleFound = false;
                    while (!titleFound && i < lines.Length)
                    {
                        // If this line consists of just equals signs, the above line is a title
                        if (Regex.IsMatch(lines[i], "^[=]*$"))
                        {
                            page.As<TitlePart>().Title = lines[i - 1];
                            titleFound = true;
                        }

                        i++;
                    }

                    // This is needed after the title is set, because slug generation needs it
                    if (isNew)  _service._contentManager.Create(page);

                    _service._contentManager.Publish(page);
                    _service._contentManager.Flush();
                }
                else if (file.Type == UpdateJobfileType.Removed)
                {
                    page = FetchPage(file.Path);

                    if (page == null) return;

                    _service._contentManager.Remove(page);
                }
            }


            private ContentItem FetchPage(string repoPath)
            {
                return _service._contentManager
                            .Query(WellKnownConstants.RepoPageContentType)
                            .Where<MarkdownPagePartRecord>(record => record.RepoPath == repoPath)
                            .List()
                            .FirstOrDefault();
            }
        }
    }
}