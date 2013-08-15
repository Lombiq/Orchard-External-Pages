using System;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.Tasks;
using Orchard.Exceptions;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketChangesetProcessorTask : IBackgroundTask
    {
        private readonly IBitbucketService _bitbucketService;

        public ILogger Logger { get; set; }


        public BitbucketChangesetProcessorTask(IBitbucketService bitbucketService)
        {
            _bitbucketService = bitbucketService;

            Logger = NullLogger.Instance;
        }


        public void Sweep()
        {
            try
            {
                _bitbucketService.ProcessNextPendingChangeset();
            }
            catch (Exception ex)
            {
                if (ex.IsFatal()) throw;

                Logger.Error(ex, "Exception when processing the next Bitbucket changeset.");
            }
        }
    }
}