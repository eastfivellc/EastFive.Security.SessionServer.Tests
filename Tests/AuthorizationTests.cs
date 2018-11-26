using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using BlackBarLabs.Security.Crypto;
using BlackBarLabs.Api.Tests;
using BlackBarLabs.Extensions;

using EastFive.Api.Tests;
using EastFive.Security.SessionServer.Tests;
using System.Linq;
using EastFive.Api.Azure.Credentials;
using EastFive.Api.Azure.Credentials.Controllers;

namespace EastFive.Security.SessionServer.Api.Tests
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
        public async Task AuthenticationRequestActsAppropriately()
        {
            Assert.IsTrue(await await SessionUtilities.StartAsync(
                async (testSession) =>
            {
                Assert.IsTrue(await await testSession.AuthenticationRequestLinksGetAsync(
                    async (responseGetAccountLinks, fetchAuthRequestLinks) =>
                    {
                        AssertApi.Success(responseGetAccountLinks);
                        var authRequestLinks = fetchAuthRequestLinks();
                        Assert.IsTrue(authRequestLinks.Any());
                        var authRequestLink = authRequestLinks.First();

                        var superAdminSession = testSession.GetSuperAdmin();

                        var redirectAddressDesired = new Uri($"http://testing{Guid.NewGuid().ToString("N")}.example.com/App");
                        var redirectAddressDesiredPostLogout = new Uri($"http://testing{Guid.NewGuid().ToString("N")}.example.com/Login");
                        var userSession = await await testSession.SessionPostAsync(authRequestLink.Id,
                                Enum.GetName(typeof(CredentialValidationMethodTypes), authRequestLink.CredentialValidationMethodType), AuthenticationActions.signin, 
                                redirectAddressDesired, redirectAddressDesiredPostLogout,
                            async (responsePosted, postedResource, fetchBody) =>
                            {
                                AssertApi.Created(responsePosted);
                                var authenticationRequestPosted = fetchBody();
                                Assert.IsFalse(authenticationRequestPosted.AuthorizationId.HasValue);
                                Assert.IsFalse(authenticationRequestPosted.Token.IsNullOrWhiteSpace());
                                Assert.AreEqual(Enum.GetName(typeof(CredentialValidationMethodTypes), authRequestLink.CredentialValidationMethodType), authenticationRequestPosted.Method);
                                Assert.AreEqual(redirectAddressDesired, authenticationRequestPosted.LocationAuthenticationReturn);
                                Assert.AreEqual(redirectAddressDesiredPostLogout, authenticationRequestPosted.LocationLogoutReturn);

                                // Fetching without a tokened session succeeds... (per UI dev's whining ;-))
                                AssertApi.Success(await testSession.AuthenticationRequestGetAsync(postedResource,
                                    (response, fetch) => response));

                                ((TestSession)testSession).LoadToken(authenticationRequestPosted.Token);
                                return await await testSession.AuthenticationRequestGetAsync(postedResource,
                                    async (responseAuthRequestGet, fetch) =>
                                    {
                                        AssertApi.Success(responseAuthRequestGet);
                                        var value = fetch();
                                        Assert.IsFalse(value.AuthorizationId.HasValue);
                                        Assert.IsTrue(value.Token.IsNullOrWhiteSpace());
                                        Assert.AreEqual(Enum.GetName(typeof(CredentialValidationMethodTypes), authRequestLink.CredentialValidationMethodType), value.Method);
                                        Assert.IsTrue(value.RefreshToken.IsNullOrWhiteSpace());
                                        Assert.AreEqual(redirectAddressDesired, value.LocationAuthenticationReturn);
                                        Assert.AreEqual(redirectAddressDesiredPostLogout, value.LocationLogoutReturn);

                                        var userIdProvider = Guid.NewGuid().ToString("N");
                                        var token = ProvideLoginMock.GetToken(userIdProvider);
                                        Enum.TryParse(authRequestLink.Method, out CredentialValidationMethodTypes val);
                                        var responseWithoutAccountLink = await testSession.GetAsync<ResponseController>(
                                            new ResponseResult
                                            {
                                                method = val,
                                            },
                                            (request) =>
                                            {
                                                request.RequestUri = value.LocationAuthentication.ParseQuery().Aggregate(
                                                    request.RequestUri.AddQuery(ProvideLoginMock.extraParamToken, token),
                                                    (url, queryValue) => url.AddQuery(queryValue.Key, queryValue.Value));
                                            });
                                        AssertApi.Conflict(responseWithoutAccountLink);

                                        var authentication = Guid.NewGuid();
                                        AssertApi.Created(await superAdminSession.CredentialPostAsync(val, userIdProvider, authentication,
                                            (response, resource) => response));

                                        var responsePostAccountLink = await testSession.GetAsync<ResponseController>(
                                            new ResponseResult
                                            {
                                                method = val,
                                            },
                                            (request) =>
                                            {
                                                request.RequestUri = value.LocationAuthentication.ParseQuery().Aggregate(
                                                    request.RequestUri.AddQuery(ProvideLoginMock.extraParamToken, token),
                                                    (url, queryValue) => url.AddQuery(queryValue.Key, queryValue.Value));
                                            });
                                        AssertApi.Redirect(responsePostAccountLink);
                                        Assert.AreEqual(
                                            postedResource.Id.UUID.ToString("N"),
                                            responsePostAccountLink.Headers.Location.GetQueryParam(EastFive.Api.Azure.AzureApplication.QueryRequestIdentfier));

                                        return await testSession.AuthenticationRequestGetAsync(postedResource,
                                            (responseAuthRequestPopulatedGet, fetchPopulated) =>
                                            {
                                                var authRequestPopulated = fetchPopulated();
                                                Assert.IsTrue(authRequestPopulated.AuthorizationId.HasValue);
                                                var userSes = new TestSession(authRequestPopulated.AuthorizationId.Value);
                                                Assert.IsFalse(authRequestPopulated.Token.IsNullOrWhiteSpace());
                                                userSes.LoadToken(authRequestPopulated.Token);
                                                
                                                return userSes;
                                            });
                                    });
                            });


                        var redirectAddressDesiredLink = new Uri($"http://testing{Guid.NewGuid().ToString("N")}.example.com");
                        Assert.IsTrue(await await userSession.IntegrationPostAsync(Guid.NewGuid(),
                            Enum.GetName(typeof(CredentialValidationMethodTypes), authRequestLink.CredentialValidationMethodType), userSession.Id, redirectAddressDesiredLink,
                            async (responsePosted, postedResource, fetchBody) =>
                            {
                                AssertApi.Created(responsePosted);
                                var authenticationRequestPosted = fetchBody();
                                Assert.AreEqual(userSession.Id, authenticationRequestPosted.AuthorizationId);
                                Assert.AreEqual(Enum.GetName(typeof(CredentialValidationMethodTypes), authRequestLink.CredentialValidationMethodType), authenticationRequestPosted.Method);
                                Assert.AreEqual(redirectAddressDesiredLink, authenticationRequestPosted.LocationAuthenticationReturn);

                                var userIdProvider = Guid.NewGuid().ToString("N");
                                var token = ProvideLoginMock.GetToken(userIdProvider);
                                Enum.TryParse(authRequestLink.Method, out CredentialValidationMethodTypes val);
                                var responseWithoutAccountLink = await testSession.GetAsync<ResponseController>(
                                    new ResponseResult
                                    {
                                        method = val
                                    },
                                    (request) =>
                                    {
                                        request.RequestUri = authenticationRequestPosted.LocationAuthentication.ParseQuery().Aggregate(
                                            request.RequestUri.AddQuery(ProvideLoginMock.extraParamToken, token),
                                            (url, queryValue) => url.AddQuery(queryValue.Key, queryValue.Value));
                                    });
                                AssertApi.Redirect(responseWithoutAccountLink);

                                return true;
                            }));
                        
                        return true;
                    }));
                return true;
            }));
        }

        [TestMethod]
        public async Task AuthorizationDuplicateCredentials()
        {
            await TestSession.StartAsync(async (testSession) =>
            {
                //var auth1 = await testSession.CreateAuthorizationAsync();
                //var credential = await testSession.CreateCredentialImplicitAsync(auth1.Id);
                //var auth2 = await testSession.CreateAuthorizationAsync();
                //var duplicateCredentailResponse = await testSession.PostAsync<CredentialController>(new Resources.CredentialPost()
                //{
                //    Id = Guid.NewGuid(),
                //    AuthorizationId = auth2.Id,
                //    Method = credential.Method,
                //    Provider = credential.Provider,
                //    UserId = credential.UserId,
                //    Token = credential.Token,
                //});
                //duplicateCredentailResponse.Assert(System.Net.HttpStatusCode.Conflict);
                await true.ToTask();
            });
        }

        [TestMethod]
        public async Task VoucherAuthentication()
        {
            await TestSession.StartAsync(async (testSession) =>
            {

                //var auth = await testSession.CreateAuthorizationAsync();
                //var credential = await testSession.CreateCredentialVoucherAsync(auth.Id);

                //var session = new Resources.SessionPost()
                //{
                //    Id = Guid.NewGuid(),
                //    AuthorizationId = auth.Id,
                //    Credentials = credential,
                //};
                //await testSession.PostAsync<SessionController>(session)
                //    .AssertAsync(System.Net.HttpStatusCode.Created);
                await true.ToTask();
            });
        }
    }
}
