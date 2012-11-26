using Orchard.Environment;
using Orchard.Environment.Extensions;
using Orchard.Services;
using Orchard.Tasks.Scheduling;
using OrchardHUN.ExternalPages.Models;
using Piedone.HelpfulLibraries.DependencyInjection;
using Piedone.HelpfulLibraries.Tasks;

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


        public BitbucketChangesetUpdater(
            IBitbucketService bitbucketService,
            IResolve<ILockFile> lockFileResolve,
            IScheduledTaskManager scheduledTaskManager,
            IClock clock)
        {
            _bitbucketService = bitbucketService;
            _lockFileResolve = lockFileResolve;
            _scheduledTaskManager = scheduledTaskManager;
            _clock = clock;
        }


        public void Process(ScheduledTaskContext context)
        {
            if (context.Task.TaskType != TaskType) return;

            using (var lockFile = _lockFileResolve.Value)
            {
                if (!lockFile.TryAcquire(TaskType)) return;

                foreach (var repository in _bitbucketService.RepositoryDataRepository.Table)
                {
                    if (repository.WasChecked()) _bitbucketService.CheckChangesets(repository.Id);
                }

                Renew();
            }
        }

        public void Activated()
        {
            Renew();
        }

        public void Terminating()
        {
        }

        private void Renew()
        {
            _scheduledTaskManager.CreateTaskIfNew(TaskType, _clock.UtcNow.AddMinutes(10), null);
        }
    }
}