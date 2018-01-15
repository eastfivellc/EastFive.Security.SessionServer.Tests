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
    public static class ActAsUserActions
    {
        public static async Task<TResult> ActAsUserGetAsync<TResult>(this ITestSession session,
            string redirectUri,
            Func<HttpResponseMessage, Func<Resources.UserInfo[]>, TResult> callback)
        {
            var query = new Resources.Queries.ActAsUserQuery
            {
                RedirectUri = redirectUri,
            };
            var response = await session.GetAsync<Controllers.ActAsUserController>(query);
            return callback(response,
                () => response.GetContentMultipart<Resources.UserInfo>().ToArray());
        }

        public static async Task<TResult> ActAsUserGetAsync<TResult>(this ITestSession session,
            Guid actorId, string redirectUri,
            Func<HttpResponseMessage, TResult> callback)
        {
            var query = new Resources.Queries.ActAsUserQuery
            {
                ActorId = actorId,
                RedirectUri = redirectUri,
            };
            var response = await session.GetAsync<Controllers.ActAsUserController>(query);
            return callback(response);
        }
    }
}
