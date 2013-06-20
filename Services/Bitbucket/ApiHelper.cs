using System;
using Orchard.Environment.Extensions;
using OrchardHUN.ExternalPages.Models;
using Piedone.HelpfulLibraries.Libraries.Utilities;
using RestSharp;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket")]
    public static class ApiHelper
    {
        public static TResponse GetResponse<TResponse>(IBitbucketRepositorySettings settings, string path) where TResponse : new()
        {
            var restObjects = PrepareRest(settings, path);
            var response = restObjects.Client.Execute<TResponse>(restObjects.Request);
            ThrowIfBadResponse(restObjects.Request, response);
            return response.Data;
        }

        public static byte[] GetResponse(IBitbucketRepositorySettings settings, string path)
        {
            var restObjects = PrepareRest(settings, path);
            var response = restObjects.Client.Execute(restObjects.Request);
            ThrowIfBadResponse(restObjects.Request, response);
            return response.RawBytes;
        }

        public static RestObjects PrepareRest(IBitbucketRepositorySettings settings, string path)
        {
            var client = new RestClient("https://api.bitbucket.org/1.0/");
            if (!String.IsNullOrEmpty(settings.Username)) client.Authenticator = new HttpBasicAuthenticator(settings.Username, settings.Password);
            var request = new RestRequest(UriHelper.Combine("repositories", settings.AccountName, settings.Slug, path));
            return new RestObjects(client, request);
        }

        public static void ThrowIfBadResponse(IRestRequest request, IRestResponse response)
        {
            if (response.ResponseStatus == ResponseStatus.TimedOut)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " timed out.", response.ErrorException);

            if (response.ResponseStatus == ResponseStatus.Error)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " failed with the following status: " + response.StatusDescription, response.ErrorException);

            if (response.ResponseStatus == ResponseStatus.Aborted)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " was aborted.", response.ErrorException);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new ApplicationException("The Bitbucket API request to " + request.Resource + " is unauthorized.", response.ErrorException);
        }


        public class RestObjects
        {
            public RestClient Client { get; private set; }
            public RestRequest Request { get; private set; }

            public RestObjects(RestClient client, RestRequest request)
            {
                Client = client;
                Request = request;
            }
        }
    }

    public interface IBitbucketRepositorySettings
    {
        string AccountName { get; }
        string Slug { get; }
        string Username { get; }
        string Password { get; }
    }
}