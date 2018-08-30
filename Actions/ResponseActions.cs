using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using System.Net.Http;
using BlackBarLabs.Api.Resources;
using BlackBarLabs;
using EastFive.Api.Azure.Credentials;
using EastFive.Api.Azure.Credentials.Controllers;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class ResponseActions
    {
        public static async Task<TResult> ResponseGetAsync<TResult>(this ITestSession session,
            CredentialValidationMethodTypes method, IDictionary<string, string> extraParams,
            Func<HttpResponseMessage, TResult> callback)
        {
            var query = new ResponseResult
            {
                method = method,
            };
            var response = await session.GetAsync<ResponseController>(query,
                (request) =>
                {
                    request.RequestUri = extraParams.Aggregate(
                        request.RequestUri,
                        (requestUri, queryParam) => requestUri.SetQueryParam(queryParam.Key, queryParam.Value));
                });
            return callback(response);
        }
    }
}
