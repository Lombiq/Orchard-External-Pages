using Orchard.Environment;
using Orchard.Environment.Extensions;
using Orchard.Services;
using Orchard.Settings;
using Orchard.Tasks.Scheduling;
using OrchardHUN.ExternalPages.Models;
using Piedone.HelpfulLibraries.DependencyInjection;
using Piedone.HelpfulLibraries.Tasks;
using Orchard.ContentManagement;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public class BitbucketChangesetUpdater : IScheduledTaskHandler, IOrchardShellEvents
    {
        private const string TaskType = "OrchardHUN.ExternalPages.BitbucketChangesetUpdate";

        private readonly IBitbucketService _bitbucketService;
        private readonly IResolve<ILockFile> _lockFileResolve;
        private readonly IScheduledTaskManager _scheduledTaskManager;
        private readonly IClock _clock;
        private readonly ISiteService _siteService;


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
                    if (repository.WasChecked()) _bitbucketService.CheckChangesets(repository.Id);
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