using Orchard;
using Orchard.Data;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Services
{
    public interface IBitbucketService : IDependency
    {
        IRepository<BitbucketRepositorySettingsRecord> SettingsRepository { get; }
        void CheckChangesets(int repositoryId);
        void ProcessNextPendingChangeset();
    }
}
