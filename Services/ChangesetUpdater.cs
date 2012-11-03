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
    public class ChangesetUpdater : IScheduledTaskHandler, IOrchardShellEvents
    {
        private const string TaskType = "OrchardHUN.Bitbucket.ChangesetUpdate";

        private readonly IRepository<RepositorySettingsRecord> _repository;
        private readonly IBitbucketService _bitbucketService;
        private readonly ILockFileManager _lockFileManager;
        private readonly IScheduledTaskManager _scheduledTaskManager;
        private readonly IClock _clock;


        public ChangesetUpdater(
            IRepository<RepositorySettingsRecord> repository,
            IBitbucketService bitbucketService,
            ILockFileManager lockFileManager,
            IScheduledTaskManager scheduledTaskManager,
            IClock clock)
        {
            _repository = repository;
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

                foreach (var repository in _repository.Table)
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
            // Checking if there are tasks with the same type scheduled in the future.
            var outdatedTaskCount = _scheduledTaskManager.GetTasks(TaskType, _clock.UtcNow).Count();
            var taskCount = _scheduledTaskManager.GetTasks(TaskType).Count();
            if (taskCount != 0 && taskCount - outdatedTaskCount > 0) return;

            _scheduledTaskManager.CreateTask(TaskType, _clock.UtcNow.AddMinutes(1), null); 
        }
    }
}