using System;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using BlackBarLabs.Security.Authorization;
using BlackBarLabs.Security.AuthorizationServer.API.Controllers;
using BlackBarLabs.Security.CredentialProvider.Facebook.Tests;
using System.Net.Http;

namespace BlackBarLabs.Security.AuthorizationServer.API.Tests
{
    public static class ClaimHelpers
    {
        public static async Task<HttpResponseMessage> ClaimPostAsync(this ITestSession testSession, 
            Guid authId, string type, string value, string issuer = default(string))
        {
            Uri issuerUri;
            Uri.TryCreate(issuer, UriKind.RelativeOrAbsolute, out issuerUri);
            var claim = new Resources.ClaimPost()
            {
                Id = Guid.NewGuid(),
                AuthorizationId = authId,
                Issuer = issuerUri,
                Type = new Uri(type),
                Value = value,
                Signature = "",
            };
            return await testSession.PostAsync<ClaimController>(claim);
        }
    }
}
