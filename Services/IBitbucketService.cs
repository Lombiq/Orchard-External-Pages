﻿using Orchard;
using Orchard.Data;
using OrchardHUN.ExternalPages.Models;

namespace OrchardHUN.ExternalPages.Services
{
    public interface IBitbucketService : IDependency
    {
        IRepository<BitbucketRepositoryDataRecord> RepositoryDataRepository { get; }

        void Repopulate(int repositoryId);
        void CheckChangesets(int repositoryId);
        void ProcessNextPendingChangeset();
    }
}
