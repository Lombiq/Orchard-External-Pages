using Orchard.Environment.Extensions;
using Orchard.Tasks;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketChangesetProcessorTask : IBackgroundTask
    {
        private readonly IBitbucketService _bitbucketService;


        public BitbucketChangesetProcessorTask(IBitbucketService bitbucketService)
        {
            _bitbucketService = bitbucketService;
        }


        public void Sweep()
        {
            _bitbucketService.ProcessNextPendingChangeset();
        }
    }
}