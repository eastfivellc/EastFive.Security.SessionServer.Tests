using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using System.Net.Http;
using BlackBarLabs.Api.Resources;
using EastFive.Api.Azure.Credentials.Resources;
using EastFive.Api.Azure.Credentials.Resources.Queries;
using EastFive.Api.Azure.Credentials.Controllers;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class ActAsUserActions
    {
        public static async Task<TResult> ActAsUserGetAsync<TResult>(this ITestSession session,
            string redirectUri,
            Func<HttpResponseMessage, Func<UserInfo[]>, TResult> callback)
        {
            var query = new ActAsUserQuery
            {
                RedirectUri = redirectUri,
            };
            var response = await session.GetAsync<ActAsUserController>(query);
            return callback(response,
                () => response.GetContentMultipart<UserInfo>().ToArray());
        }

        public static async Task<TResult> ActAsUserGetAsync<TResult>(this ITestSession session,
            Guid actorId, string redirectUri,
            Func<HttpResponseMessage, TResult> callback)
        {
            var query = new ActAsUserQuery
            {
                ActorId = actorId,
                RedirectUri = redirectUri,
            };
            var response = await session.GetAsync<ActAsUserController>(query);
            return callback(response);
        }
    }
}
