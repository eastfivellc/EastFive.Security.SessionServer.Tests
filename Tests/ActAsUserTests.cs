using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using BlackBarLabs.Api.Tests;

using EastFive.Api.Tests;
using EastFive.Security.SessionServer.Tests;
using System.Linq;

namespace EastFive.Security.SessionServer.Api.Tests
{
    [TestClass]
    public class ActAsUserTests
    {
        [TestMethod]
        public async Task ActAsUserActsAppropriately()
        {
            Assert.IsTrue(await await SessionUtilities.StartAsync(
                async (testSession) =>
                {
                    AssertApi.Unauthorized(await testSession.ActAsUserGetAsync("http://example.com/authorize",
                        (response, fetchActAsUserResources) => response));

                    var superAdminSession = testSession.GetSuperAdmin();
                    return await await superAdminSession.ActAsUserGetAsync("http://example.com/authorize",
                        async (responseGetActAsuser, fetchActAsUsers) =>
                        {
                            AssertApi.Success(responseGetActAsuser);
                            var actAsUsers = fetchActAsUsers();
                            Assert.IsTrue(actAsUsers.Any());
                            var actAsUser = actAsUsers.First();

                            var actAsUserLinkParams = System.Web.HttpUtility.ParseQueryString(actAsUser.Link.ToUri().Query);
                            return await await superAdminSession.ActAsUserGetAsync(Guid.Parse(actAsUserLinkParams["token"]), actAsUserLinkParams["RedirectUri"],
                                async (responseGetActAsUserRedirect) =>
                                {
                                    var redirect = AssertApi.Redirect(responseGetActAsUserRedirect).Headers.Location;
                                    var sessionId = Guid.Parse(redirect.GetQueryParam(EastFive.Api.Azure.AzureApplication.QueryRequestIdentfier));
                                    // Fetching without a tokened session succeeds... (per UI dev's whining ;-))
                                    return await testSession.AuthenticationRequestGetAsync(sessionId,
                                        (responseAuthRequestPopulatedGet, fetchPopulated) =>
                                        {
                                            var authRequestPopulated = fetchPopulated();
                                            Assert.IsTrue(authRequestPopulated.authorization.HasValue);
                                            var userSes = new TestSession(authRequestPopulated.authorization.id.Value);
                                            Assert.IsFalse(authRequestPopulated.token.IsNullOrWhiteSpace());
                                            userSes.LoadToken(authRequestPopulated.token);

                                            return true;
                                        });
                                });
                        });
                }));
        }
    }
}
