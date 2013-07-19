using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard;
using Orchard.Environment.Extensions;
using Piedone.HelpfulLibraries.Utilities;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    public interface IBitbucketApiService : IDependency
    {
        TResponse Fetch<TResponse>(IBitbucketAuthConfig authConfig, string path) where TResponse : new();
        byte[] Fetch(IBitbucketAuthConfig authConfig, string path);
    }


    public interface IBitbucketAuthConfig
    {
        string Username { get; }
        string Password { get; }
    }

    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket.Services")]
    public static class BitbucketApiServiceExtensions
    {
        public static TResponse FetchFromRepo<TResponse>(this IBitbucketApiService apiService, IBitbucketRepositorySettings settings, string path) where TResponse : new()
        {
            return apiService.Fetch<TResponse>(settings, UriHelper.Combine("repositories", settings.AccountName, settings.Slug, path));
        }

        public static byte[] FetchFromRepo(this IBitbucketApiService apiService, IBitbucketRepositorySettings settings, string path)
        {
            return apiService.Fetch(settings, UriHelper.Combine("repositories", settings.AccountName, settings.Slug, path));
        }

        public static TResponse FetchFromUser<TResponse>(this IBitbucketApiService apiService, IBitbucketAuthConfig settings, string path) where TResponse : new()
        {
            return apiService.Fetch<TResponse>(settings, UriHelper.Combine("user", path));
        }
    }

    public interface IBitbucketRepositorySettings : IBitbucketAuthConfig
    {
        string AccountName { get; }
        string Slug { get; }
    }
}
