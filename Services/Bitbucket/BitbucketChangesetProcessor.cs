using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Environment.Extensions;
using Orchard.Tasks;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketChangesetProcessor : IBackgroundTask
    {
        private readonly IBitbucketService _bitbucketService;


        public BitbucketChangesetProcessor(IBitbucketService bitbucketService)
        {
            _bitbucketService = bitbucketService;
        }


        public void Sweep()
        {
            _bitbucketService.ProcessNextPendingChangeset();
        }
    }
}