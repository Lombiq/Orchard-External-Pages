using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Environment.Extensions;
using OrchardHUN.ExternalPages.Models;
using Piedone.HelpfulLibraries.Libraries.Utilities;
using Piedone.HelpfulLibraries.Tasks.Jobs;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketService : IBitbucketService
    {
        private const string Industry = "OrchardHUN.ExternalPages.Bitbucket.Changesets";

        private readonly IBitbucketApiService _apiService;
        private readonly IRepository<BitbucketRepositoryDataRecord> _repository;
        private readonly IJobManager _jobManager;
        private readonly IFileProcessor _fileProcessor;
        private readonly IContentManager _contentManager;

        public IRepository<BitbucketRepositoryDataRecord> RepositoryDataRepository { get { return _repository; } }


        public BitbucketService(
            IBitbucketApiService apiService,
            IRepository<BitbucketRepositoryDataRecord> repository,
            IJobManager jobManager,
            IFileProcessor fileProcessor,
            IContentManager contentManager)
        {
            _apiService = apiService;
            _jobManager = jobManager;
            _repository = repository;
            _fileProcessor = fileProcessor;
            _contentManager = contentManager;
        }


        public void Repopulate(int repositoryId)
        {
            var repoData = GetRepositoryDataOrThrow(repositoryId);

            var lastChangeset = _apiService.FetchFromRepo<ChangesetsResponse>(repoData, "changesets?limit=1").Changesets.FirstOrDefault();

            if (lastChangeset == null) return;

            Func<string, List<FolderSrcFile>> recursivelyFetchFileList = null;
            recursivelyFetchFileList =
                (path) =>
                {
                    var responseData = _apiService.FetchFromRepo<FolderSrcResponse>(repoData, UriHelper.Combine("src", lastChangeset.Revision.ToString(), path));

                    if (responseData.Directories == null) throw new ApplicationException("The path " + path + " was not found in the repo.");

                    if (responseData.Directories.Count == 0) return responseData.Files;

                    var files = new List<FolderSrcFile>();
                    foreach (var directory in responseData.Directories)
                    {
                        files.AddRange(recursivelyFetchFileList(UriHelper.Combine(path, directory)));
                    }
                    files.AddRange(responseData.Files);

                    return files;
                };

            foreach (var mapping in repoData.UrlMappings())
            {
                var jobFiles = new List<UpdateJobFile>();

                if (!mapping.RepoPath.IsMarkdownFilePath())
                {
                    var files = recursivelyFetchFileList(mapping.RepoPath);
                    foreach (var file in files)
                    {
                        jobFiles.Add(new UpdateJobFile(file.Path, UpdateJobfileType.AddedOrModified));
                    }
                }
                else
                {
                    jobFiles.Add(new UpdateJobFile(mapping.RepoPath, UpdateJobfileType.AddedOrModified));
                }

                var jobContext = new UpdateJobContext(
                                    repositoryId,
                                    lastChangeset.Node,
                                    lastChangeset.Revision,
                                    jobFiles,
                                    true);

                _jobManager.CreateJob(Industry, jobContext, 99);
            }

            repoData.LastCheckedNode = lastChangeset.Node;
            repoData.LastCheckedRevision = lastChangeset.Revision;
            repoData.LastProcessedNode = "";
            repoData.LastProcessedRevision = -1;
        }

        public void CheckChangesets(int repositoryId)
        {
            var repoData = GetRepositoryDataOrThrow(repositoryId);

            if (String.IsNullOrEmpty(repoData.LastCheckedNode)) throw new InvalidOperationException("The repository with the id " + repositoryId + " should be populated first.");

            var changesets = _apiService.FetchFromRepo<ChangesetsResponse>(repoData, "changesets?limit=50").Changesets;

            var lastChangeset = changesets.Where(changeset => changeset.Node == repoData.LastCheckedNode).SingleOrDefault();
            if (lastChangeset != null)
            {
                var lastChangesetIndex = changesets.IndexOf(lastChangeset);
                changesets.RemoveRange(0, lastChangesetIndex + 1);
            }

            if (changesets.Count == 0) return;

            lastChangeset = changesets.Last();
            repoData.LastCheckedNode = lastChangeset.Node;
            repoData.LastCheckedRevision = lastChangeset.Revision;

            var urlMappings = repoData.UrlMappings();

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
                                        changeset.Node,
                                        changeset.Revision,
                                        jobFiles,
                                        false);

                    _jobManager.CreateJob(Industry, jobContext, 0);
                }
            }
        }

        public void ProcessNextPendingChangeset()
        {
            var job = _jobManager.TakeOnlyJob(Industry);

            if (job == null) return;

            var jobContext = job.Context<UpdateJobContext>();

            var repoData = _repository.Get(jobContext.RepositoryId);

            if (repoData == null)
            {
                // Repository was deleted since the job was created
                _jobManager.Done(job);
                return;
            }

            // If it's a repopulation we'll have multiple jobs with the same revsion for each mapping, that's why the second clause
            if (jobContext.Revision <= repoData.LastProcessedRevision && !jobContext.IsRepopulation)
            {
                _jobManager.Done(job);
                return;
            }

            _fileProcessor.ProcessFiles(jobContext);

            repoData.LastProcessedNode = jobContext.Node;
            repoData.LastProcessedRevision = jobContext.Revision;
            _jobManager.Done(job);
        }

        public void Delete(int repositoryId)
        {
            var repoRecord = _repository.Get(repositoryId);
            if (repoRecord != null) _repository.Delete(repoRecord);

            var pages = _contentManager
                .Query(WellKnownConstants.RepoPageContentType)
                .Where<MarkdownPagePartRecord>(record => record.RepoPath.StartsWith(UriHelper.Combine("bitbucket.org", repoRecord.AccountName, repoRecord.Slug)))
                .List();

            foreach (var page in pages)
            {
                _contentManager.Remove(page);
            }
        }


        private BitbucketRepositoryDataRecord GetRepositoryDataOrThrow(int id)
        {
            var repository = _repository.Get(id);
            if (repository == null) throw new ArgumentException("No repository exists with the following id: " + id);
            return repository;
        }
    }
}