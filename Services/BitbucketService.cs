using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using OrchardHUN.Bitbucket.Models;
using RestSharp;

namespace OrchardHUN.Bitbucket.Services
{
    public class BitbucketService : IBitbucketService
    {
        private readonly IContentManager _contentManager;


        public BitbucketService(IContentManager contentManager)
        {
            _contentManager = contentManager;
        }


        public void CheckChangesets(IRepositorySettings settings)
        {
            var client = new RestClient("https://api.bitbucket.org/1.0/");
            client.Authenticator = new HttpBasicAuthenticator(settings.Username, settings.Password);
            var request = new RestRequest("repositories/{accountname}/{slug}/changesets?limit=50");
            request.AddUrlSegment("accountname", settings.AccountName);
            request.AddUrlSegment("slug", settings.Slug);
            var response = client.Execute<ChangesetsResponse>(request);
        }
    }

    public class ChangesetsResponse
    {
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
}