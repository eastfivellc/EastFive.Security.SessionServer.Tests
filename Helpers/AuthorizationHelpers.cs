using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using BlackBarLabs.Security.AuthorizationServer.API.Controllers;
using BlackBarLabs.Security.CredentialProvider.Facebook.Tests;

namespace BlackBarLabs.Security.AuthorizationServer.API.Tests
{
    public static class AuthorizationHelpers
    {
        public static async Task<Resources.Authorization> CreateAuthorizationAsync(this ITestSession testSession)
        {
            var auth = new Resources.AuthorizationPost()
            {
                Id = Guid.NewGuid(),
            };
            var createAuthResponse = await testSession.PostAsync<AuthorizationController>(auth);
            createAuthResponse.Assert(HttpStatusCode.Created);
            return auth;
        }
    }
}
