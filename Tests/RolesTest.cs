using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BlackBarLabs.Security.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlackBarLabs.Api.Tests;
using EastFive.Security.SessionServer.Tests;
using EastFive.Api.Tests;
using EastFive.Security.SessionServer.Api.Tests;
using System.Linq;

namespace BlackBarLabs.Security.AuthorizationServer.API.Tests
{
    [TestClass]
    public class RolesTest
    {
        [TestMethod]
        public async Task RolesActsAppropriately()
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
                            var userIdProvider = Guid.NewGuid().ToString("N");
                            var token = ProvideLoginMock.GetToken(userIdProvider);

                            // Create Auth resource
                            var authentication = Guid.NewGuid();
                            var userSession = new TestSession(authentication);
                            AssertApi.Created(await userSession.CredentialPostAsync(authRequestLink.CredentialValidationMethodType, userIdProvider, authentication,
                                (response, resource) => response));

                            Assert.AreEqual(0, await userSession.RolesGetByActorAsync(authentication,
                                (response, fetch) => fetch().Length));

                            var role1 = Guid.NewGuid().ToString("N");
                            AssertApi.Created(await userSession.RolesPostAsync(authentication, role1,
                                (response, resource) => response));

                            Assert.AreEqual(1, await userSession.RolesGetByActorAsync(authentication,
                                (response, fetch) => fetch().Length));

                            var stressTestCount = 30;
                            var deleteRate = 2;
                            var newRoles = await Enumerable.Range(0, stressTestCount).Select(
                                async i =>
                                {
                                    var role2 = Guid.NewGuid().ToString("N") + $"{i}";
                                    AssertApi.Created(await await userSession.RolesPostAsync(authentication, role2,
                                       async (response, resource) =>
                                        {
                                            if(i % deleteRate == 0)
                                                AssertApi.Success(await userSession.RolesDeleteByIdAsync(resource.Id));
                                            return response;
                                        }));
                                    return role2;
                                })
                                .WhenAllAsync();

                            Assert.AreEqual(1 + (stressTestCount / deleteRate), await userSession.RolesGetByActorAsync(authentication,
                                (response, fetch) => fetch().Length));

                            return true;
                    }));

                    return true;
                }));
        }
    }
}
