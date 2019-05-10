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

            var user = sessionFactory.GetUnauthorizedSession();

            (user as Api.Azure.AzureApplication)
                .AddOrUpdateInstantiation(typeof(ProvideLoginMock),
                    async (app) =>
                    {
                        await 1.AsTask();
                        return new ProvideLoginMock();
                    });

            var mockAuthenticationMock = await user.GetAsync<Method, Method>(
                onContents:
                    authentications =>
                    {
                        var matchingAuthentications = authentications
                            .Where(auth => auth.name == ProvideLoginMock.IntegrationName);
                        Assert.IsTrue(matchingAuthentications.Any());
                        return matchingAuthentications.First();
                    });



            // TODO: Only get Authorization with property header
            // TODO: Can't get twice
            // TODO: IF authorized, LocationAuthentication should not be set

            // Create empty authorization

            var authReturnUrl = new Uri("http://example.com/authtest");
            var authorizationIdSecure = Security.SecureGuid.Generate();
            var authorizationUnmapped = await user.PostAsync(
                    new Auth.Authorization
                    {
                        authorizationRef = authorizationIdSecure.AsRef<Auth.Authorization>(),
                        Method = mockAuthenticationMock.authenticationId,
                        LocationAuthenticationReturn = authReturnUrl,
                    },
                onCreatedBody:
                    (authorizationResponse, contentType) =>
                    {
                        Assert.AreEqual(mockAuthenticationMock.id, authorizationResponse.Method.id);
                        Assert.AreEqual(authReturnUrl, authorizationResponse.LocationAuthenticationReturn);
                        return authorizationResponse;
                    });

            #region User performs login

            var externalSystemUserId = Guid.NewGuid().ToString();// User performs login
            var internalSystemUserId = Guid.NewGuid();
            var responseResource = ProvideLoginMock.GetResponse(externalSystemUserId, authorizationUnmapped.authorizationRef.id);
            var userPostLogin = sessionFactory.GetUnauthorizedSession();

            (userPostLogin as Api.Azure.AzureApplication)
                .AddOrUpdateInstantiation(typeof(ProvideLoginMock),
                    async (app) =>
                    {
                        await 1.AsTask();
                        var loginMock = new ProvideLoginAccountMock();
                        loginMock.MapAccount =
                            (externalKey, extraParameters, authenticationInner, authorization,
                                baseUri, webApiApplication,
                             onCreatedMapping,
                             onAllowSelfServeAccounts,
                             onInterceptProcess,
                             onNoChange) =>
                            {
                                //Assert.AreEqual(externalSystemUserId, externalKey);
                                return onAllowSelfServeAccounts().AsTask();
                            };
                        return loginMock;
                    });

            #endregion

            var authorizationToAthenticateSession = await await userPostLogin.GetAsync(responseResource,
                onRedirect:
                    async (urlRedirect, reason) =>
                    {
                        var authIdStr = urlRedirect.GetQueryParam(EastFive.Api.Azure.AzureApplication.QueryRequestIdentfier);
                        var authId = Guid.Parse(authIdStr);
                        var authIdRef = authId.AsRef<Auth.Authorization>();

                        var comms = sessionFactory.GetUnauthorizedSession();
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

                                    sessionVirgin.authorization = new RefOptional<Auth.Authorization>(authenticatedAuthorization.authorizationRef);
                                    return await comms.PatchAsync(sessionVirgin,
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

            // TODO: Only get Authorization with property header
            // TODO: Can't get twice
            // TODO: IF authorized, LocationAuthentication should not be set

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

            // TODO: Only get Authorization with property header
            // TODO: Can't get twice
            // TODO: IF authorized, LocationAuthentication should not be set


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
                            (subject,
                                extraParameters, disard, authorization,
                                baseUri,
                                webApiApplication,
                             onMapped,
                             onSelfServe,
                             onInterrupt,
                             onNoChange) =>
                            {
                                Assert.AreEqual(externalSystemUserId, subject);
                                return onMapped(internalSystemUserId).AsTask();
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

    }
}
