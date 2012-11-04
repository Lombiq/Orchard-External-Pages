using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard;
using Orchard.Data;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Services
{
    public interface IBitbucketService : IDependency
    {
        IRepository<BitbucketRepositorySettingsRecord> SettingsRepository { get; }
        void CheckChangesets(int repositoryId);
    }
}
