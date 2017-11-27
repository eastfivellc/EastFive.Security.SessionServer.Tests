using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;

using BlackBarLabs.Api.Tests;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class AccountLinksActions
    {
        public static async Task<TResult> AuthenticationRequestLinksGetAsync<TResult>(this ITestSession session,
            Func<HttpResponseMessage, Func<Resources.AuthenticationRequestLink[]>, TResult> callback)
        {
            var authenticationRequestLinksQuery = new Resources.Queries.AuthenticationRequestLinkQuery()
            {
            };

            var response = await session.GetAsync<Controllers.AuthenticationRequestLinkController>(authenticationRequestLinksQuery);
            return callback(response, () => response
                .GetContentMultipart<Resources.AuthenticationRequestLink>()
                .ToArray());
        }
    }
}
