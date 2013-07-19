using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.Drivers;
using Orchard.Environment.Extensions;
using OrchardHUN.ExternalPages.Models;
using OrchardHUN.ExternalPages.Services.Bitbucket;

namespace OrchardHUN.ExternalPages.Drivers
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketMarkdownPagePartDriver : ContentPartDriver<MarkdownPagePart>
    {
        private readonly IBitbucketService _bitbucketService;


        public BitbucketMarkdownPagePartDriver(IBitbucketService bitbucketService)
        {
            _bitbucketService = bitbucketService;
        }
	
			
        protected override DriverResult Display(MarkdownPagePart part, string displayType, dynamic shapeHelper)
        {
            if (!part.RepoPath.StartsWith("bitbucket.org")) return null;

            return ContentShape("Parts_MarkdownPage_BitbucketEditLink",
                () =>
                {
                    var segments = part.RepoPath.Split('/');
                    var localPath = string.Join("/", segments.Skip(3));

                    var repo = _bitbucketService.RepositoryDataRepository.Table
                        .Where(
                            record => record.AccountName == segments[1] &&
                            record.Slug == segments[2]
                            )
                        .ToArray()
                        .Where(record => record.UrlMappings().Any(mapping => localPath.StartsWith(mapping.RepoPath))) // This can't be parsed by IRepository
                        .FirstOrDefault();

                    if (repo == null) return shapeHelper.Empty(); // Comes from Helpful Libraries

                    var repoLink = "https://bitbucket.org/" + repo.AccountName + "/" + repo.Slug + "/src/" + repo.LastCheckedNode + "/" + localPath + "?at=default";

                    return shapeHelper.Parts_MarkdownPage_BitbucketEditLink(RepositoryFileLink: repoLink);
                });
        }
    }
}