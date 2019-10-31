using Orchard.Environment.Extensions;
using RestSharp;
using System;
using System.Net;

namespace OrchardHUN.ExternalPages.Services.Bitbucket
{
    [OrchardFeature("OrchardHUN.ExternalPages.Bitbucket.Services")]
    public class BitbucketApiService : IBitbucketApiService
    {
        static BitbucketApiService()
        {
            // This needs to be set explicitly for Bitbucket download URLs. 
            // See: https://support.microsoft.com/en-us/help/3069494/cannot-connect-to-a-server-by-using-the-servicepointmanager-or-sslstre
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }


        public TResponse Fetch<TResponse>(IBitbucketAuthConfig authConfig, string path) where TResponse : new()
        {
            var restObjects = PrepareRest(authConfig, path);
            var response = restObjects.Client.Execute<TResponse>(restObjects.Request);
            ThrowIfBadResponse(restObjects.Request, response);
            return response.Data;
        }

        public byte[] Fetch(IBitbucketAuthConfig authConfig, string path)
        {
            var restObjects = PrepareRest(authConfig, path);
            var response = restObjects.Client.Execute(restObjects.Request);
            ThrowIfBadResponse(restObjects.Request, response);
            return response.RawBytes;
        }


        private RestObjects PrepareRest(IBitbucketAuthConfig authConfig, string path)
        {
            var client = new RestClient("https://api.bitbucket.org/2.0/");
            if (!string.IsNullOrEmpty(authConfig.Username))
            {
                client.Authenticator = new HttpBasicAuthenticator(authConfig.Username, authConfig.Password);
            }
            return new RestObjects(client, new RestRequest(path));
        }


        private static void ThrowIfBadResponse(IRestRequest request, IRestResponse response)
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
}