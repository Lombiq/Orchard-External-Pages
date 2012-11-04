using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Data;
using Orchard.Environment;
using Orchard.Services;
using Orchard.Tasks.Scheduling;
using OrchardHUN.Bitbucket.Models;
using Piedone.HelpfulLibraries.Tasks;
using Piedone.HelpfulLibraries.Tasks.Jobs;

namespace OrchardHUN.Bitbucket.Services
{
    public class BitbucketChangesetUpdater : IScheduledTaskHandler, IOrchardShellEvents
    {
        private const string TaskType = "OrchardHUN.Bitbucket.ChangesetUpdate";

        private readonly IBitbucketService _bitbucketService;
        private readonly ILockFileManager _lockFileManager;
        private readonly IScheduledTaskManager _scheduledTaskManager;
        private readonly IClock _clock;


        public BitbucketChangesetUpdater(
            IBitbucketService bitbucketService,
            ILockFileManager lockFileManager,
            IScheduledTaskManager scheduledTaskManager,
            IClock clock)
        {
            _bitbucketService = bitbucketService;
            _lockFileManager = lockFileManager;
            _scheduledTaskManager = scheduledTaskManager;
            _clock = clock;
        }


        public void Process(ScheduledTaskContext context)
        {
            if (context.Task.TaskType != TaskType) return;

            using (var lockFile = _lockFileManager.TryAcquireLock(TaskType, 0))
            {
                if (lockFile == null) return;

                foreach (var repository in _bitbucketService.SettingsRepository.Table)
                {
                    _bitbucketService.CheckChangesets(repository.Id);
                }

                CreateTask();
            }
        }

        public void Activated()
        {
            CreateTask();
        }

        public void Terminating()
        {
        }

        private void CreateTask()
        {
            _scheduledTaskManager.CreateTaskIfNew(TaskType, _clock.UtcNow.AddMinutes(10), null); 
        }
    }
}