using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard;
using Orchard.Data;
using OrchardHUN.Bitbucket.Models;

namespace OrchardHUN.Bitbucket.Services
{
    public interface IBitbucketService : IDependency
    {
        IRepository<RepositorySettingsRecord> SettingsRepository { get; }
        void CheckChangesets(int repositoryId);
    }
}
