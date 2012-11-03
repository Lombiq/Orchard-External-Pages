using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.Data;
using OrchardHUN.Bitbucket.Models;
using Piedone.HelpfulLibraries.Tasks.Jobs;
using RestSharp;

namespace OrchardHUN.Bitbucket.Services
{
    public class BitbucketService : IBitbucketService
    {
        private readonly IRepository<RepositorySettingsRecord> _repository;
        private readonly IJobManager _jobManager;
        private readonly IContentManager _contentManager;


        public BitbucketService(
            IRepository<RepositorySettingsRecord> repository,
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
            var repository = GetRepositoryOrThrow(repositoryId);

            var client = new RestClient("https://api.bitbucket.org/1.0/");
            client.Authenticator = new HttpBasicAuthenticator(repository.Username, repository.Password);
            var request = new RestRequest("repositories/{accountname}/{slug}/changesets?limit=50");
            request.AddUrlSegment("accountname", repository.AccountName);
            request.AddUrlSegment("slug", repository.Slug);

            var response = client.Execute<ChangesetsResponse>(request).Data;

            var changesets = response.Changesets;

            if (!String.IsNullOrEmpty(repository.LastNode))
            {
                var lastChangeset = changesets.Where(changeset => changeset.RawNode == repository.LastNode).SingleOrDefault();
                if (lastChangeset != null)
                {
                    var lastChangesetIndex = changesets.IndexOf(lastChangeset);
                    changesets.RemoveRange(0, lastChangesetIndex + 1);
                }
            }

            if (changesets.Count == 0) return;

            repository.LastNode = changesets.Last().RawNode;

            var urlMappings = repository.UrlMappings();

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

                    _jobManager.CreateJob(WellKnownConstants.JobIndustry, jobContext); 
                }
            }
        }


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


        private RepositorySettingsRecord GetRepositoryOrThrow(int id)
        {
            var repository = _repository.Get(id);
            if (repository == null) throw new ArgumentException("No repository exists with the following id: " + id);
            return repository;
        }
    }


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
}