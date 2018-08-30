using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;

using BlackBarLabs.Api.Tests;
using EastFive.Api.Azure.Credentials.Resources;
using EastFive.Api.Azure.Credentials.Resources.Queries;
using EastFive.Api.Azure.Credentials.Controllers;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class AccountLinksActions
    {
        public static async Task<TResult> AuthenticationRequestLinksGetAsync<TResult>(this ITestSession session,
            Func<HttpResponseMessage, Func<AuthenticationRequestLink[]>, TResult> callback)
        {
            var authenticationRequestLinksQuery = new AuthenticationRequestLinkQuery()
            {
            };

            var response = await session.GetAsync<AuthenticationRequestLinkController>(authenticationRequestLinksQuery);
            throw new NotImplementedException();
            //return callback(response, () => response
            //    .GetContentMultipart<Resources.AuthenticationRequestLink>()
            //    .ToArray());
        }
    }
}
