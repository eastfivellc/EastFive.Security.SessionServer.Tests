﻿using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using BlackBarLabs.Api.Tests;

using EastFive.Api.Tests;
using EastFive.Security.SessionServer.Tests;
using System.Linq;
using EastFive.Api.Azure.Credentials;
using EastFive.Api.Azure.Credentials.Controllers;
using EastFive.Security.Crypto;
using EastFive.Extensions;
using EastFive.Security.SessionServer.Api.Tests;
using EastFive.Azure.Auth;
using EastFive.Api;
using BlackBarLabs.Extensions;

namespace EastFive.Azure.Tests.Authorization
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
                                Enum.GetName(typeof(CredentialValidationMethodTypes), authRequestLink.CredentialValidationMethodType), Security.SessionServer.AuthenticationActions.signin, 
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
        public async Task AuthenticationRequestActsAppropriately2()
        {

            // var sessionFactory = new RestApplicationFactory();
            var sessionFactory = await TestApplicationFactory.InitAsync();
            var superAdmin = await sessionFactory.SessionSuperAdminAsync();

            (superAdmin as Api.Azure.AzureApplication)
                .AddOrUpdateInstantiation(typeof(Auth.CredentialProviders.AdminLogin),
                    async (app) =>
                    {
                        await 1.AsTask();
                        return new Auth.CredentialProviders.AdminLogin();
                    });
            (superAdmin as Api.Azure.AzureApplication)
                .AddOrUpdateInstantiation(typeof(ProvideLoginMock),
                    async (app) =>
                    {
                        await 1.AsTask();
                        return new ProvideLoginMock();
                    });

            var authenticationAdmin = await superAdmin.GetAsync<Method, Method>(
                onContents:
                    authentications =>
                    {
                        var matchingAuthentications = authentications
                            .Where(auth => auth.name == Auth.CredentialProviders.AdminLogin.IntegrationName);
                        Assert.IsTrue(matchingAuthentications.Any());
                        return matchingAuthentications.First();
                    });

            var mockAuthenticationMock = await superAdmin.GetAsync<Method, Method>(
                onContents:
                    authentications =>
                    {
                        var matchingAuthentications = authentications
                            .Where(auth => auth.name == ProvideLoginMock.IntegrationName);
                        Assert.IsTrue(matchingAuthentications.Any());
                        return matchingAuthentications.First();
                    });

            var authReturnUrl = new Uri("http://example.com/authtest");
            var authorizationIdSecure = Security.SecureGuid.Generate();
            var authorizationInvite = new Auth.Authorization
            {
                authorizationId = authorizationIdSecure.AsRef<Auth.Authorization>(),
                Method = mockAuthenticationMock.authenticationId,
                LocationAuthenticationReturn = authReturnUrl,
            };
            var authroizationWithUrls = await superAdmin.PostAsync(authorizationInvite,
                onCreatedBody:
                    (authorizationResponse, contentType) =>
                    {
                        Assert.AreEqual(authorizationInvite.Method.id, authorizationResponse.Method.id);
                        Assert.AreEqual(authReturnUrl, authorizationResponse.LocationAuthenticationReturn);
                        return authorizationResponse;
                    });


            var externalSystemUserId = Guid.NewGuid().ToString();
            var internalSystemUserId = Guid.NewGuid();
            // var mockParameters = ProvideLoginMock.GetParameters(externalSystemUserId);
            Assert.IsTrue(await superAdmin.PostAsync(
                new Auth.AccountMapping
                {
                    accountMappingId = Guid.NewGuid(),
                    accountId = internalSystemUserId,
                    authorization = authorizationInvite.authorizationId,
                },
                onCreated: () => true));

            var comms = sessionFactory.GetUnauthorizedSession();
            var session = new Session
            {
                sessionId = Guid.NewGuid().AsRef<Session>(),
            };
            var token = await comms.PostAsync(session,
                onCreatedBody: (sessionWithToken, contentType) => sessionWithToken.token);

            (comms as Api.Azure.AzureApplication)
                .AddOrUpdateInstantiation(typeof(ProvideLoginMock),
                    async (app) =>
                    {
                        await 1.AsTask();
                        return new ProvideLoginMock();
                    });

            // TODO: comms.LoadToken(session.token);
            var authentication = await comms.GetAsync<Method, Method>(
                onContents:
                    authentications =>
                    {
                        var matchingAuthentications = authentications
                            .Where(auth => auth.name == ProvideLoginMock.IntegrationName);
                        Assert.IsTrue(matchingAuthentications.Any());
                        return matchingAuthentications.First();
                    });


            var responseResource = ProvideLoginMock.GetResponse(externalSystemUserId, authorizationInvite.authorizationId.id);
            var authorizationToAthenticateSession = await await comms.GetAsync(responseResource,
                onRedirect:
                    async (urlRedirect, reason) =>
                    {
                        var authIdStr = urlRedirect.GetQueryParam(EastFive.Api.Azure.AzureApplication.QueryRequestIdentfier);
                        var authId = Guid.Parse(authIdStr);
                        var authIdRef = authId.AsRef<Auth.Authorization>();

                        // TODO: New comms here?
                        return await await comms.GetAsync(
                            (Auth.Authorization authorizationGet) => authorizationGet.authorizationId.AssignQueryValue(authIdRef),
                            onContent:
                                async (authenticatedAuthorization) =>
                                {
                                    var sessionVirgin = new Session
                                    {
                                        sessionId = Guid.NewGuid().AsRef<Session>(),
                                        authorization = new RefOptional<Auth.Authorization>(authIdRef),
                                    };
                                    var tokenNew = await comms.PostAsync(sessionVirgin,
                                        onCreatedBody: (sessionWithToken, contentType) =>
                                        {
                                            Assert.AreEqual(internalSystemUserId, sessionWithToken.account.Value);
                                            return sessionWithToken.HeaderName.PairWithValue(sessionWithToken.token);
                                        });
                                    comms.Headers.Add(tokenNew.Key, tokenNew.Value);

                                    var integration = new Auth.Integration
                                    {
                                        integrationId = Guid.NewGuid(),
                                        accountId = internalSystemUserId,
                                        Method = mockAuthenticationMock.authenticationId,
                                        authorization = new RefOptional<Auth.Authorization>(authIdRef),
                                    };
                                    Assert.IsTrue(await comms.PostAsync(integration,
                                        onCreated: () => true));

                                    session.authorization = new RefOptional<Auth.Authorization>(authenticatedAuthorization.authorizationId);
                                    return await comms.PatchAsync(session,
                                        onUpdatedBody:
                                            (updated) =>
                                            {
                                                return updated;
                                            });
                                });
                    });

            Assert.AreEqual(internalSystemUserId, authorizationToAthenticateSession.account.Value);

        }


        [TestMethod]
        public async Task AuthenticationHandlesDirectLink()
        {
            // var sessionFactory = new RestApplicationFactory();
            var sessionFactory = await TestApplicationFactory.InitAsync();

            var externalSystemUserId = Guid.NewGuid().ToString();
            var internalSystemUserId = Guid.NewGuid();
            
            var comms = sessionFactory.GetUnauthorizedSession();
            
            (comms as Api.Azure.AzureApplication)
                .AddOrUpdateInstantiation(typeof(ProvideLoginMock),
                    async (app) =>
                    {
                        await 1.AsTask();
                        var accountMock = new ProvideLoginAccountMock();
                        accountMock.MapAccount =
                            (externalKey) =>
                            {
                                Assert.AreEqual(externalSystemUserId, externalKey);
                                return internalSystemUserId;
                            };
                        return accountMock;
                    });

            // TODO: comms.LoadToken(session.token);
            var authentication = await comms.GetAsync<Method, Method>(
                onContents:
                    authentications =>
                    {
                        var matchingAuthentications = authentications
                            .Where(auth => auth.name == ProvideLoginMock.IntegrationName);
                        Assert.IsTrue(matchingAuthentications.Any());
                        return matchingAuthentications.First();
                    });

            var responseResource = ProvideLoginMock.GetResponse(externalSystemUserId);
            var authorizationToAthenticateSession = await await comms.GetAsync(responseResource,
                onRedirect:
                    async (urlRedirect, reason) =>
                    {
                        var authIdStr = urlRedirect.GetQueryParam(EastFive.Api.Azure.AzureApplication.QueryRequestIdentfier);
                        var authId = Guid.Parse(authIdStr);
                        var authIdRef = authId.AsRef<Auth.Authorization>();

                        // TODO: New comms here?
                        return await await comms.GetAsync(
                            (Auth.Authorization authorizationGet) => authorizationGet.authorizationId.AssignQueryValue(authIdRef),
                            onContent:
                                (authenticatedAuthorization) =>
                                {
                                    var session = new Session
                                    {
                                        sessionId = Guid.NewGuid().AsRef<Session>(),
                                        authorization = new RefOptional<Auth.Authorization>(authenticatedAuthorization.authorizationId),
                                    };
                                    return comms.PatchAsync(session,
                                        onUpdatedBody:
                                            (updated) =>
                                            {
                                                return updated;
                                            });
                                });
                    });

            Assert.AreEqual(internalSystemUserId, authorizationToAthenticateSession.account.Value);

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
                await true.AsTask();
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
                await true.AsTask();
            });
        }
    }
}
