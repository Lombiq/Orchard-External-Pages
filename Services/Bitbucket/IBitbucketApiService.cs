using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    public interface IBitbucketApiService : IDependency
    {
        TResponse Fetch<TResponse>(IBitbucketRepositorySettings settings, string path) where TResponse : new();
        byte[] Fetch(IBitbucketRepositorySettings settings, string path);
    }
}
