﻿using System;
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


        // For initial population if there are more than 50 changesets, TODO
        //public void Populate(int repositoryId)
        //{
        //}

        public void CheckChangesets(int repositoryId)
        {
            var settings = GetRepositorySettingsOrThrow(repositoryId);

            var restObjects = PrepareRest(settings, "changesets?limit=50");

            var response = restObjects.Client.Execute<ChangesetsResponse>(restObjects.Request);
            BitbucketService.ThrowIfBadResponse(restObjects.Request, response);

            var changesets = response.Data.Changesets;

            if (!String.IsNullOrEmpty(settings.LastNode))
            {
                var lastChangeset = changesets.Where(changeset => changeset.RawNode == settings.LastNode).SingleOrDefault();
                if (lastChangeset != null)
                {
                    var lastChangesetIndex = changesets.IndexOf(lastChangeset);
                    changesets.RemoveRange(0, lastChangesetIndex + 1);
                }
            }

            if (changesets.Count == 0) return;

            settings.LastNode = changesets.Last().RawNode;

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
            public string RawNode { get; set; }
            public DateTime UtcTimestamp { get; set; }
            public List<ChangesetFile> Files { get; set; }
        }

        public class ChangesetFile
        {
            public string Type { get; set; }
            public string File { get; set; }
        }

        public class SrcResponse
        {
            public string Node { get; set; }
            public string Data { get; set; }
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
            client.Authenticator = new HttpBasicAuthenticator(settings.Username, settings.Password);
            var request = new RestRequest("repositories/{accountname}/{slug}/" + path);
            request.AddUrlSegment("accountname", settings.AccountName);
            request.AddUrlSegment("slug", settings.Slug);

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
            if (response.ResponseStatus == ResponseStatus.Completed) return;

            if (response.ResponseStatus == ResponseStatus.TimedOut)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " timed out.", response.ErrorException);

            if (response.ResponseStatus == ResponseStatus.Error)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " failed with the following status: " + response.StatusDescription, response.ErrorException);
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

                var localPath = file.Path.Replace(mapping.RepoPath, mapping.LocalPath).Replace("Index", "").Replace(".md", "");
                ContentItem page = null;

                if (file.Type == UpdateJobfileType.Added || file.Type == UpdateJobfileType.Modified)
                {
                    var restObjects = PrepareRest(_settings, "src/" + _jobContext.Revision + "/" + file.Path);
                    var response = restObjects.Client.Execute<SrcResponse>(restObjects.Request);
                    BitbucketService.ThrowIfBadResponse(restObjects.Request, response);

                    var src = response.Data;

                    if (file.Type == UpdateJobfileType.Modified) page = FetchPage(file.Path);

                    if (page == null)
                    {
                        page = _service._contentManager.New(WellKnownConstants.RepoPageContentType);

                        var autoroutePart = page.As<AutoroutePart>();
                        autoroutePart.CustomPattern = localPath;
                        autoroutePart.UseCustomPattern = true;
                        autoroutePart.DisplayAlias = localPath;
                        page.As<MarkdownPagePart>().RepoPath = file.Path;

                        _service._contentManager.Create(page);
                    }

                    page.As<MarkdownPagePart>().Text = src.Data;

                    // Searching for the (first) title in the markdown text
                    var lines = Regex.Split(src.Data, "\r\n|\r|\n");
                    for (int i = lines.Length - 1; i > 0; i--)
                    {
                        // If this line consists of just equals signs, the above line is a title
                        if (Regex.IsMatch(lines[i], "^[=]*$")) page.As<TitlePart>().Title = lines[i - 1];
                    }

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