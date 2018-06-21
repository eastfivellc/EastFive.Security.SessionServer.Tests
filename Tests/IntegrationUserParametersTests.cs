using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using EastFive.Extensions;
using EastFive.Api.Tests;
using EastFive.Security.SessionServer.Tests;
using BlackBarLabs.Api.Tests;
using EastFive.Security.SessionServer.Api.Tests;
using System.Collections.Generic;

namespace EastFive.Security.SessionServer.Api.Tests
{
    /// <summary>
    /// Summary description for IntegrationUserParametersTests
    /// </summary>
    [TestClass]
    public class IntegrationUserParametersTests
    {
        [TestMethod]
        public async Task IntegrationUserParametersWorkProperly()
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
                                    authRequestLink.Method, authorizationId,
                                    redirectAddressDesired,
                                async (responsePosted, postedResource, fetchBody) =>
                                {
                                    AssertApi.Created(responsePosted);
                                    var authenticationRequestPosted = fetchBody();
                                    Assert.IsFalse(authenticationRequestPosted.AuthorizationId.IsDefault());
                                    Assert.AreEqual(authRequestLink.Method, authenticationRequestPosted.Method);
                                    Assert.AreEqual(redirectAddressDesired, authenticationRequestPosted.LocationAuthenticationReturn);

                                    Assert.IsTrue(await await userSession.IntegrationGetAsync(postedResource,
                                        async (responseAuthRequestGet, fetch) =>
                                        {
                                            AssertApi.Success(responseAuthRequestGet);
                                            var value = fetch();
                                            Assert.IsFalse(value.AuthorizationId.IsDefault());
                                            Assert.AreEqual(authRequestLink.Method, value.Method);
                                            Assert.AreEqual(redirectAddressDesired, value.LocationAuthenticationReturn);

                                            var userParams = new Dictionary<string, Resources.AuthorizationRequest.CustomParameter>();
                                            userParams.Add("push_pmp_file_to_ehr", new Resources.AuthorizationRequest.CustomParameter { Value = "true" });
                                            Assert.IsTrue(await userSession.IntegrationPutAsync(authRequestLink.Id,
                                               authRequestLink.Method, authorizationId,
                                               redirectAddressDesired,
                                               userParams,
                                               (responsePut, putResource, putBody) =>
                                               {
                                                   AssertApi.Success(responsePut);
                                                   return true;
                                               }));

                                            Assert.IsTrue(await userSession.IntegrationGetAsync(authRequestLink.Id,
                                                (intGet, intFetch) =>
                                                {
                                                    AssertApi.Success(intGet);
                                                    var fetchValue = intFetch();
                                                    Assert.IsTrue(fetchValue.UserParameters.ContainsKey("push_pmp_file_to_ehr"));
                                                    Assert.AreEqual("true", fetchValue.UserParameters["push_pmp_file_to_ehr"].Value);
                                                    return true;
                                                }));

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
