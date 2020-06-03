using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Security;
using OrchardHUN.ExternalPages.Models;
using Piedone.HelpfulLibraries.Tasks.Jobs;
using Piedone.HelpfulLibraries.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private readonly IEncryptionService _encryptionService;

        public IRepository<BitbucketRepositoryDataRecord> RepositoryDataRepository { get { return _repository; } }


        public BitbucketService(
            IBitbucketApiService apiService,
            IRepository<BitbucketRepositoryDataRecord> repository,
            IJobManager jobManager,
            IFileProcessor fileProcessor,
            IContentManager contentManager,
            IEncryptionService encryptionService)
        {
            _apiService = apiService;
            _jobManager = jobManager;
            _repository = repository;
            _fileProcessor = fileProcessor;
            _contentManager = contentManager;
            _encryptionService = encryptionService;
        }


        public void Repopulate(int repositoryId)
        {
            var repoData = GetRepositoryDataOrThrow(repositoryId);
            var repoSettings = new BitbucketRepositorySettings(repoData, _encryptionService);

            var lastChangeset = _apiService.FetchFromRepo<CommitsResponse>(repoSettings, "commits/master").Values.FirstOrDefault();

            if (lastChangeset == null) return;

            Func<string, List<FolderSrcFile>> recursivelyFetchFileList = null;
            recursivelyFetchFileList =
                (path) =>
                {
                    var responseData = _apiService.FetchFromRepo<FolderSrcResponse>(repoSettings, UriHelper.Combine("src", lastChangeset.Hash.ToString(), path, "/"));

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
                                    lastChangeset.Hash,
                                    jobFiles,
                                    true);

                _jobManager.CreateJob(Industry, jobContext, 99);
            }

            repoData.LastCheckedNode = lastChangeset.Hash;
            repoData.LastProcessedNode = "";
            repoData.LastProcessedRevision = -1;
        }

        public void CheckChangesets(int repositoryId)
        {
            var repoData = GetRepositoryDataOrThrow(repositoryId);

            if (string.IsNullOrEmpty(repoData.LastCheckedNode))
            {
                throw new InvalidOperationException("The repository with the id " + repositoryId + " should be populated first.");
            }

            var commits = _apiService
                .FetchFromRepo<CommitsResponse>(new BitbucketRepositorySettings(repoData, _encryptionService), "commits/master")
                .Values;

            // So the oldest ones are at the top.
            commits.Reverse();

            var lastChangeset = commits.Where(commit => commit.Hash == repoData.LastCheckedNode).SingleOrDefault();
            if (lastChangeset != null)
            {
                var lastChangesetIndex = commits.IndexOf(lastChangeset);
                commits.RemoveRange(0, lastChangesetIndex + 1);
            }

            if (commits.Count == 0) return;

            repoData.LastCheckedNode = commits.Last().Hash;

            var urlMappings = repoData.UrlMappings();

            // Enumerating the commits in reverse so older commits will be processed first. Thus if the same files are
            // changed new changes will overwrite old ones.
            foreach (var commit in commits)
            {
                var diffStats = _apiService
                    .FetchFromRepo<DiffStat>(new BitbucketRepositorySettings(repoData, _encryptionService), "diffstat/" + commit.Hash)
                    .Values;
                var diffs = diffStats.Where(diffStat => urlMappings.Any(mapping => diffStat.New?.Path.StartsWith(mapping.RepoPath) == true));
                var diffCount = diffs.Count();

                if (diffCount != 0)
                {
                    var jobFiles = new List<UpdateJobFile>(diffCount);
                    foreach (var diff in diffs)
                    {
                        if (diff.Status != "renamed")
                        {
                            var type = UpdateJobfileType.Added;
                            if (diff.Status == "modified") type = UpdateJobfileType.Modified;
                            else if (diff.Status == "removed") type = UpdateJobfileType.Removed;

                            jobFiles.Add(new UpdateJobFile(diff.New.Path, type));
                        }
                        else
                        {
                            jobFiles.Add(new UpdateJobFile(diff.Old.Path, UpdateJobfileType.Removed));
                            jobFiles.Add(new UpdateJobFile(diff.New.Path, UpdateJobfileType.Added));
                        }
                    }

                    var jobContext = new UpdateJobContext(
                                        repositoryId,
                                        commit.Hash,
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

            // The following code was here but since revisions were removed from the BB API we should handle it somehow
            // else.
            // If it's a repopulation we'll have multiple jobs with the same revision for each mapping, that's why the
            // second clause.
            //if (jobContext.Revision <= repoData.LastProcessedRevision && !jobContext.IsRepopulation)
            //{
            //    _jobManager.Done(job);
            //    return;
            //}

            _fileProcessor.ProcessFiles(jobContext);

            repoData.LastProcessedNode = jobContext.Node;
            _jobManager.Done(job);
        }

        public void Delete(int repositoryId)
        {
            var repoRecord = _repository.Get(repositoryId);

            if (repoRecord != null) _repository.Delete(repoRecord);

            var pageContentType =
                repoRecord != null &&
                !string.IsNullOrEmpty(repoRecord.PageContentTypeName) ? repoRecord.PageContentTypeName : WellKnownConstants.DefaultRepoPageContentType;

            // This won't scale, but works fine up to a couple hundred pages.
            var pages = _contentManager
                .Query(pageContentType)
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