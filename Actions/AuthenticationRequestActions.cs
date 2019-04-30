using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using System.Net.Http;
using BlackBarLabs.Api.Resources;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class AuthorizationHelpers
    {
        

        public static async Task<TResult> AuthenticationRequestGetAsync<TResult>(this ITestSession session,
            WebId authenticationRequestId,
            Func<HttpResponseMessage, Func<EastFive.Azure.Auth.Session>, TResult> callback)
        {
            throw new NotImplementedException();
        }
    }
}
