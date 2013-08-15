using Orchard.ContentManagement;
using Orchard.Environment;
using Orchard.Environment.Extensions;
using Orchard.Services;
using Orchard.Settings;
using Orchard.Tasks.Scheduling;
using OrchardHUN.ExternalPages.Models;
using Piedone.HelpfulLibraries.DependencyInjection;
using Piedone.HelpfulLibraries.Tasks;
using System;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.Tasks;
using Orchard.Exceptions;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketChangesetUpdater : IScheduledTaskHandler, IOrchardShellEvents
    {
        private const string TaskType = "OrchardHUN.ExternalPages.BitbucketChangesetUpdater";

        private readonly IBitbucketService _bitbucketService;
        private readonly IResolve<ILockFile> _lockFileResolve;
        private readonly IScheduledTaskManager _scheduledTaskManager;
        private readonly IClock _clock;
        private readonly ISiteService _siteService;

        public ILogger Logger { get; set; }


        public BitbucketChangesetUpdater(
            IBitbucketService bitbucketService,
            IResolve<ILockFile> lockFileResolve,
            IScheduledTaskManager scheduledTaskManager,
            IClock clock,
            ISiteService siteService)
        {
            _bitbucketService = bitbucketService;
            _lockFileResolve = lockFileResolve;
            _scheduledTaskManager = scheduledTaskManager;
            _clock = clock;
            _siteService = siteService;

            Logger = NullLogger.Instance;
        }


        public void Process(ScheduledTaskContext context)
        {
            if (context.Task.TaskType != TaskType) return;

            using (var lockFile = _lockFileResolve.Value)
            {
                if (!lockFile.TryAcquire(TaskType)) return;

                Renew(true);

                foreach (var repository in _bitbucketService.RepositoryDataRepository.Table)
                {
                    try
                    {
                        if (repository.WasChecked()) _bitbucketService.CheckChangesets(repository.Id);
                    }
                    catch (Exception ex)
                    {
                        if (ex.IsFatal()) throw;

                        Logger.Error(ex, "Exception when processing checking changesets on Bitbucket for the repository " + repository.AccountName + "/" + repository.Slug);
                    }
                }
            }
        }

        public void Activated()
        {
            Renew(false);
        }

        public void Terminating()
        {
        }

        private void Renew(bool calledFromTaskProcess)
        {
            _scheduledTaskManager.CreateTaskIfNew(TaskType, _clock.UtcNow.AddMinutes(_siteService.GetSiteSettings().As<BitbucketSettingsPart>().MinutesBetweenPulls), null, calledFromTaskProcess);
        }
    }
}