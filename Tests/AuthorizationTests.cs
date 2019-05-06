using System;
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
        public async Task CanLoginInviteAuthenticationRequest()
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
                authorizationRef = authorizationIdSecure.AsRef<Auth.Authorization>(),
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
                    authorization = authorizationInvite.authorizationRef,
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

            var responseResource = ProvideLoginMock.GetResponse(externalSystemUserId, authorizationInvite.authorizationRef.id);
            var authorizationToAthenticateSession = await await comms.GetAsync(responseResource,
                onRedirect:
                    async (urlRedirect, reason) =>
                    {
                        var authIdStr = urlRedirect.GetQueryParam(EastFive.Api.Azure.AzureApplication.QueryRequestIdentfier);
                        var authId = Guid.Parse(authIdStr);
                        var authIdRef = authId.AsRef<Auth.Authorization>();

                        // TODO: New comms here?
                        return await await comms.GetAsync(
                            (Auth.Authorization authorizationGet) => authorizationGet.authorizationRef.AssignQueryValue(authIdRef),
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
                                        integrationRef = Guid.NewGuid().AsRef<Auth.Integration>(),
                                        accountId = internalSystemUserId,
                                        Method = mockAuthenticationMock.authenticationId,
                                        authorization = new RefOptional<Auth.Authorization>(authIdRef),
                                    };
                                    Assert.IsTrue(await comms.PostAsync(integration,
                                        onCreated: () => true));

                                    session.authorization = new RefOptional<Auth.Authorization>(authenticatedAuthorization.authorizationRef);
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
        public async Task CanLoginWithAuthenticationRequest()
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
                authorizationRef = authorizationIdSecure.AsRef<Auth.Authorization>(),
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
                    authorization = authorizationInvite.authorizationRef,
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
            
            var responseResource = ProvideLoginMock.GetResponse(externalSystemUserId, authorizationInvite.authorizationRef.id);
            var authorizationToAthenticateSession = await await comms.GetAsync(responseResource,
                onRedirect:
                    async (urlRedirect, reason) =>
                    {
                        var authIdStr = urlRedirect.GetQueryParam(EastFive.Api.Azure.AzureApplication.QueryRequestIdentfier);
                        var authId = Guid.Parse(authIdStr);
                        var authIdRef = authId.AsRef<Auth.Authorization>();

                        // TODO: New comms here?
                        return await await comms.GetAsync(
                            (Auth.Authorization authorizationGet) => authorizationGet.authorizationRef.AssignQueryValue(authIdRef),
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
                                        integrationRef = Guid.NewGuid().AsRef<Auth.Integration>(),
                                        accountId = internalSystemUserId,
                                        Method = mockAuthenticationMock.authenticationId,
                                        authorization = new RefOptional<Auth.Authorization>(authIdRef),
                                    };
                                    Assert.IsTrue(await comms.PostAsync(integration,
                                        onCreated: () => true));

                                    session.authorization = new RefOptional<Auth.Authorization>(authenticatedAuthorization.authorizationRef);
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
                            (Auth.Authorization authorizationGet) => authorizationGet.authorizationRef.AssignQueryValue(authIdRef),
                            onContent:
                                (authenticatedAuthorization) =>
                                {
                                    var session = new Session
                                    {
                                        sessionId = Guid.NewGuid().AsRef<Session>(),
                                        authorization = new RefOptional<Auth.Authorization>(authenticatedAuthorization.authorizationRef),
                                    };
                                    return comms.PostAsync(session,
                                        onCreatedBody:
                                            (updated, contentType) =>
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
