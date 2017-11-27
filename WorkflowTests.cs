using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlackBarLabs.Api.Tests;

namespace EastFive.Security.SessionServer.Api.Tests
{
    [TestClass]
    public class WorkflowTests
    {
        //[TestMethod]
        //public async Task WorkflowSASPut()
        //{
        //    await TestSession.StartAsync(async (testSession) =>
        //    {
        //        //TODO: SessionBuilder testSession.AddRequestPropertyFetch(AuthorizationClient.ServicePropertyDefinitions.AuthorizationClient, authClient);

        //        // Create session, auth, and credential resources
        //        var session = await testSession.CreateSessionAsync();
        //        var auth = await testSession.CreateAuthorizationAsync();
        //        var credential = await testSession.CreateCredentialVoucherAsync(auth.Id);

        //        // Authenticate session
        //        var authenticateSessionResponseMessage = await testSession.AuthenticateSession(
        //            session.Id, credential);
        //        authenticateSessionResponseMessage.AssertSuccessPut();
        //        var authenticateSession = authenticateSessionResponseMessage.GetContent<Resources.SessionPut>();
        //        Assert.AreEqual(auth.Id, authenticateSession.AuthorizationId);
        //    });
        //}

        //[TestMethod]
        //public async Task WorkflowSASPost()
        //{
        //    await SessionBuilder.SessionAsync(async (testSession) =>
        //    {
        //        // Create session, auth, and credential resources
        //        var session = await testSession.CreateSessionAsync();
        //        var auth = await testSession.CreateAuthorizationAsync();
        //        var credential = await testSession.CreateCredentialVoucherAsync(auth.Id);

        //        // Authenticate session
        //        var authenticateSessionResponse = await testSession.AuthenticateSession(
        //            session.Id, credential);
        //        authenticateSessionResponse.Assert(System.Net.HttpStatusCode.Accepted);

        //        // Authenticate session
        //        var newSession = new Resources.SessionPost()
        //        {
        //            Id = Guid.NewGuid(),
        //            AuthorizationId = auth.Id,
        //            Credentials = credential,
        //        };
        //        await testSession.PostAsync<SessionController>(newSession)
        //            .AssertAsync(System.Net.HttpStatusCode.Created);
        //    });
        //}

        //[TestMethod]
        //public async Task WorkflowAS()
        //{
        //    await TestSession.StartAsync(async (testSession) =>
        //    {
        //        //TODO: SessionBuilder testSession.AddRequestPropertyFetch(AuthorizationClient.ServicePropertyDefinitions.AuthorizationClient, authClient);

        //        // Create Auth resource
        //        var auth = await testSession.CreateAuthorizationAsync();
        //        var credential = await testSession.CreateCredentialVoucherAsync(auth.Id);

        //        var session = await testSession.CreateSessionWithCredentialsAsync(credential);
        //        Assert.AreEqual(auth.Id, session.AuthorizationId);
        //    });
        //}

        //[TestMethod]
        //public async Task WorkflowImplicitAuth()
        //{
        //    await TestSession.StartAsync(async (testSession) =>
        //    {
        //        //TODO: SessionBuilder testSession.AddRequestPropertyFetch(AuthorizationClient.ServicePropertyDefinitions.AuthorizationClient, authClient);

        //        var auth = await testSession.CreateAuthorizationAsync();
        //        var credential = await testSession.CreateCredentialImplicitAsync(auth.Id);

        //        var session = await testSession.CreateSessionWithCredentialsAsync(credential);
        //        Assert.AreEqual(auth.Id, session.AuthorizationId);
        //    });
        //}
        
    }
}
