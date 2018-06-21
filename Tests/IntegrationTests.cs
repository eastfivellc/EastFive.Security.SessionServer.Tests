using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using EastFive.Extensions;
using EastFive.Api.Tests;
using EastFive.Security.SessionServer.Tests;
using BlackBarLabs.Api.Tests;


namespace EastFive.Security.SessionServer.Api.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public async Task IntegrationsActsAppropriately()
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
                        var authorizationId = Guid.NewGuid();
                        var userSession = new TestSession(authorizationId);
                        Assert.IsTrue(await await userSession.IntegrationPostAsync(authRequestLink.Id,
                                Enum.GetName(typeof(CredentialValidationMethodTypes), authRequestLink.CredentialValidationMethodType), authorizationId,
                                redirectAddressDesired, 
                            async (responsePosted, postedResource, fetchBody) =>
                            {
                                AssertApi.Created(responsePosted);
                                var authenticationRequestPosted = fetchBody();
                                Assert.IsFalse(authenticationRequestPosted.AuthorizationId.IsDefault());
                                Assert.AreEqual(Enum.GetName(typeof(CredentialValidationMethodTypes), authRequestLink.CredentialValidationMethodType), authenticationRequestPosted.Method);
                                Assert.AreEqual(redirectAddressDesired, authenticationRequestPosted.LocationAuthenticationReturn);
                                
                                Assert.IsTrue(await await userSession.IntegrationGetAsync(postedResource,
                                    async (responseAuthRequestGet, fetch) =>
                                    {
                                        AssertApi.Success(responseAuthRequestGet);
                                        var value = fetch();
                                        Assert.IsFalse(value.AuthorizationId.IsDefault());
                                        Assert.AreEqual(Enum.GetName(typeof(CredentialValidationMethodTypes), authRequestLink.CredentialValidationMethodType), value.Method);
                                        Assert.AreEqual(redirectAddressDesired, value.LocationAuthenticationReturn);

                                        var userIdProvider = Guid.NewGuid().ToString("N");
                                        var token = ProvideLoginMock.GetToken(userIdProvider);
                                        Enum.TryParse(authRequestLink.Method, out CredentialValidationMethodTypes val);
                                        var responseAuthenicateIntegration = await userSession.GetAsync<Controllers.ResponseController>(
                                            new Controllers.ResponseResult
                                            {
                                                method = val,
                                            },
                                            (request) =>
                                            {
                                                request.RequestUri = value.LocationAuthentication.ParseQuery().Aggregate(
                                                    request.RequestUri.AddQuery(ProvideLoginMock.extraParamToken, token),
                                                    (url, queryValue) => url.AddQuery(queryValue.Key, queryValue.Value));
                                            });
                                        AssertApi.Redirect(responseAuthenicateIntegration);
                                        
                                        Assert.AreEqual(
                                            postedResource.Id.UUID.ToString("N"),
                                            responseAuthenicateIntegration.Headers.Location.GetQueryParam("request_id"));

                                        AssertApi.Success(await userSession.IntegrationGetAsync(postedResource,
                                            (responseAuthRequestPopulatedGet, fetchPopulated) =>
                                            {
                                                var authRequestPopulated = fetchPopulated();
                                                Assert.IsFalse(authRequestPopulated.AuthorizationId.IsDefault());
                                                return responseAuthRequestPopulatedGet;
                                            }));

                                        Assert.AreEqual(1, await userSession.IntegrationGetByAuthorizationAsync(authorizationId,
                                            (responseAuthRequestPopulatedGet, fetchPopulated) => fetchPopulated().Length));

                                        AssertApi.Success(await userSession.IntegrationDeleteAsync(postedResource));

                                        Assert.AreEqual(0, await userSession.IntegrationGetByAuthorizationAsync(postedResource.AuthorizationId,
                                            (responseAuthRequestPopulatedGet, fetchPopulated) => fetchPopulated().Length));

                                        AssertApi.NotFound(await userSession.IntegrationGetAsync(postedResource,
                                            (responseAuthRequestPopulatedGet, fetchPopulated) => responseAuthRequestPopulatedGet));

                                        return true;
                                    }));

                                return true;
                            }));
                        
                        return true;
                    }));
                return true;
            }));
        }
    }
}
