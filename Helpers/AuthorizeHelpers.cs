using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using BlackBarLabs.Security.AuthorizationServer.API.Controllers;
using BlackBarLabs.Security.CredentialProvider.Facebook.Tests;
using System.Net.Http;

namespace BlackBarLabs.Security.AuthorizationServer.API.Tests
{
    public static class AuthorizeHelpers
    {
        public static async Task<TResult> CreateAuthorizeAsync<TResult>(this ITestSession testSession,
            string name, Uri scope, string key,
            Func<HttpResponseMessage, Resources.Authorize, TResult> callback)
        {
            var auth = new Resources.Authorize()
            {
                Id = Guid.NewGuid(),
                Name = name,
                Scope = scope,
                Key = key,
            };
            var createAuthResponse = await testSession.PostAsync<AuthorizeController>(auth);
            return callback(createAuthResponse, auth);
        }
    }
}
