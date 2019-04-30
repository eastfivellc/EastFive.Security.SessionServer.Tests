using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlackBarLabs.Api.Tests;
using BlackBarLabs.Extensions;
using System.Net;

namespace EastFive.Security.SessionServer.Api.Tests
{
    [TestClass]
    public class SessionTests
    {
        //[TestMethod]
        //public async Task InvalidCredentials()
        //{
        //    await TestSession.StartAsync(async (testSession) =>
        //    {
        //        //TODO: SessionBuilder testSession.AddRequestPropertyFetch(AuthorizationClient.ServicePropertyDefinitions.AuthorizationClient, authClient);

        //        var auth = await testSession.CreateAuthorizationAsync();
        //        var wrongCredential = await testSession.CreateCredentialImplicitAsync(auth.Id);
        //        wrongCredential.Token = Guid.NewGuid().ToString("N");
        //        var sessionWithWrongCredential = new Resources.SessionPost
        //        {
        //            Id = Guid.NewGuid(),
        //            Credentials = wrongCredential,
        //        };

        //        var authenticateSessionResponse = await testSession.PostAsync<SessionController>(sessionWithWrongCredential);
        //        authenticateSessionResponse.Assert(HttpStatusCode.Conflict);
        //    });
        //}

        //[TestMethod]
        //public async Task Example()
        //{
        //    await TestSession.StartAsync(async (testSession) =>
        //    {
        //        //TODO: SessionBuilder testSession.AddRequestPropertyFetch(AuthorizationClient.ServicePropertyDefinitions.AuthorizationClient, authClient);

        //        var session = new Resources.SessionPost()
        //        {
        //            Id = Guid.NewGuid(),
        //            Credentials = new Resources.Credential()
        //            {
        //                    Method = Authorization.CredentialValidationMethodTypes.Implicit,
        //                    Provider = new Uri("http://orderowl.com/api/Auth"),
        //                    UserId = "butthead",
        //                    Token = "Password#1",
        //            },
        //        };
        //        var authenticateSessionResponse = await testSession.PostAsync<SessionController>(session);
        //        authenticateSessionResponse.Assert(HttpStatusCode.Conflict);
        //    });
        //}

    }
}
