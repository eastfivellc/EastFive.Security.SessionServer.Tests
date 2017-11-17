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
    public class AuthorizationTests
    {
        internal void GenerateKeys()
        {
            var rsa = new RSACryptoServiceProvider(2048);

            rsa.PersistKeyInCsp = false; //This is important because we don't want to store these keys in the windows files system

            string publicPrivateKeyXML = rsa.ToXmlString(true);
            string publicOnlyKeyXML = rsa.ToXmlString(false);

            string publicPrivateKeystring = CryptoTools.UrlBase64Encode(publicPrivateKeyXML);
            string publicOnlyKeystring = CryptoTools.UrlBase64Encode(publicOnlyKeyXML);
            
            // do stuff with keys...
        }

        [TestMethod]
        public void GenerateKeysTest()
        {
            this.GenerateKeys();
        }

        [TestMethod]
        public async Task CreateAuthorizationWithCredentials()
        {
            await TestSession.StartAsync(async (testSession) =>
            {
                // Create Auth resource
                await testSession.CreateAuthorizationAsync();
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
