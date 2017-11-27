using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class SessionHelpers
    {
        public static async Task<Resources.Session> CreateSessionAsync(this ITestSession testSession)
        {
            var id = Guid.NewGuid();
            var session = new Resources.Session()
            {
                Id = id,
            };
            var response = await testSession.PostAsync<Controllers.SessionController>(session);
            response.Assert(HttpStatusCode.Created);
            return session;
        }

        //public static async Task<Resources.Session> CreateSessionWithCredentialsAsync(this ITestSession testSession)
        //{
        //    var auth = await testSession.CreateAuthorizationAsync();
        //    if(default(ICredential) == credentials)
        //        credentials = await testSession.CreateCredentialVoucherAsync(auth.Id);
        //    var sessionId = Guid.NewGuid();
        //    var session = new Resources.SessionPost()
        //    {
        //        Id = sessionId,
        //        AuthorizationId = auth.Id,
        //        Credentials = (Resources.Credential)credentials,
        //    };
        //    var createSessionResponse = await testSession.PostAsync<SessionController>(session)
        //        .AssertAsync(HttpStatusCode.Created);
        //    var responseSession = createSessionResponse.GetContent<Resources.Session>();
        //    return responseSession;
        //}

        public static async Task<HttpResponseMessage> AuthenticateSession(this ITestSession testSession,
            Guid sessionId, Guid authorizationId)
        {
            var session = new Resources.Session()
            {
                Id = sessionId,
                AuthorizationId = authorizationId,
            };
            var authenticateSessionResponse = await testSession.PutAsync<Controllers.SessionController>(session);
            return authenticateSessionResponse;
        }
    }
}
