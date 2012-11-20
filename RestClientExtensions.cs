using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using RestSharp;

namespace OrchardHUN.ExternalPages
{
    // For future use
    public static class RestClientExtensions
    {
        public static Task<IRestResponse> GetResponseAsync(this RestClient client, IRestRequest request)
        {
            var tcs = new TaskCompletionSource<IRestResponse>();

            var loginResponse = client.ExecuteAsync(request, r =>
            {
                if (r.ErrorException == null)
                {
                    tcs.SetResult(r);
                }
                else
                {
                    tcs.SetException(r.ErrorException);
                }
            });

            return tcs.Task;
        }

        public static Task<IRestResponse<T>> GetResponseAsync<T>(this RestClient client, IRestRequest request)
             where T : new()
        {
            var tcs = new TaskCompletionSource<IRestResponse<T>>();

            var loginResponse = client.ExecuteAsync<T>(request, r =>
            {
                if (r.ErrorException == null)
                {
                    tcs.SetResult(r);
                }
                else
                {
                    tcs.SetException(r.ErrorException);
                }
            });

            return tcs.Task;
        }
    }
}