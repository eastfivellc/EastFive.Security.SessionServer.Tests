using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BlackBarLabs.Security.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlackBarLabs.Api.Tests;
using BlackBarLabs.Security.AuthorizationServer.API.Controllers;
using BlackBarLabs.Security.Authorization;

namespace BlackBarLabs.Security.AuthorizationServer.API.Tests
{
    [TestClass]
    public class AuthorizeTests
    {
        [TestMethod]
        public async Task CreateAuthorize()
        {
            await RSA.Generate(
                async (publicKey, privateKey) =>
                {
                    await TestSession.StartAsync(async (testSession) =>
                    {
                        // Create Auth resource
                        await testSession.CreateAuthorizationAsync();
                        var scope = new Uri($"urn:example.com/{Guid.NewGuid().ToString("N")}");
                        await testSession.CreateAuthorizeAsync(Guid.NewGuid().ToString("N"), scope, publicKey,
                            (response, resource) => response);
                    });
                });
            
        }

        [TestMethod]
        public async Task AuthorizationDuplicateCredentials()
        {
            await TestSession.StartAsync(async (testSession) =>
            {
                var auth1 = await testSession.CreateAuthorizationAsync();
                var credential = await testSession.CreateCredentialImplicitAsync(auth1.Id);
                var auth2 = await testSession.CreateAuthorizationAsync();
                var duplicateCredentailResponse = await testSession.PostAsync<CredentialController>(new Resources.CredentialPost()
                {
                    Id = Guid.NewGuid(),
                    AuthorizationId = auth2.Id,
                    Method = credential.Method,
                    Provider = credential.Provider,
                    UserId = credential.UserId,
                    Token = credential.Token,
                });
                duplicateCredentailResponse.Assert(System.Net.HttpStatusCode.Conflict);
            });
        }

        [TestMethod]
        public async Task VoucherAuthentication()
        {
            await TestSession.StartAsync(async (testSession) =>
            {
                
                var auth = await testSession.CreateAuthorizationAsync();
                var credential = await testSession.CreateCredentialVoucherAsync(auth.Id);

                var session = new Resources.SessionPost()
                {
                    Id = Guid.NewGuid(),
                    AuthorizationId = auth.Id,
                    Credentials = credential,
                };
                await testSession.PostAsync<SessionController>(session)
                    .AssertAsync(System.Net.HttpStatusCode.Created);
            });
        }
    }
}
