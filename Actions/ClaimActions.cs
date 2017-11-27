using System;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using System.Net.Http;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class ClaimActions
    {
        public static async Task<HttpResponseMessage> ClaimPostAsync(this ITestSession testSession, 
            Guid authId, string type, string value, string issuer = default(string))
        {
            Uri issuerUri;
            Uri.TryCreate(issuer, UriKind.RelativeOrAbsolute, out issuerUri);
            var claim = new Resources.Claim()
            {
                Id = Guid.NewGuid(),
                AuthorizationId = authId,
                Issuer = issuerUri,
                Type = new Uri(type),
                Value = value,
                Signature = "",
            };
            return await testSession.PostAsync<Api.Controllers.AccountLinksController>(claim);
        }
    }
}
