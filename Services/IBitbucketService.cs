using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard;
using OrchardHUN.Bitbucket.Models;

namespace OrchardHUN.Bitbucket.Services
{
    public interface IBitbucketService : IDependency
    {
        void CheckChangesets(IRepositorySettings settings);
    }
}
