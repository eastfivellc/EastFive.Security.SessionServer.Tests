using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlackBarLabs.Api.Tests;
using System.Net;
using System.Linq;
using BlackBarLabs.Web;

namespace EastFive.Security.SessionServer.Api.Tests
{
    [TestClass]
    public class ClaimsTest
    {
        [TestMethod]
        public async Task ClaimsWorks()
        {
            await TestSession.Start(Guid.NewGuid(),
                async (testSession) =>
            {
                //var type = "urn:example.com/Claim/type-abc";
                var value = Guid.NewGuid().ToString();

                var auth = await testSession.CreateCredentialAsync();
                //await testSession.ClaimPostAsync(auth.Id, type, value)
                //    .AssertAsync(HttpStatusCode.Created);
                
                //var credential = await testSession.CreateCredentialVoucherAsync(auth.Id);
                //var session = await testSession.CreateSessionWithCredentialsAsync(credential);

                //session.SessionHeader.Value.GetClaimsJwtString(
                //    (claims) =>
                //    {
                //        var thisClaim = claims.First(clm => String.Compare(clm.Type, type) == 0);
                //        Assert.AreEqual(value, thisClaim.Value);
                //        return true;
                //    },
                //    (why) =>
                //    {
                //        Assert.Fail(why);
                //        return false;
                //    },
                //    "AuthServer.issuer", "AuthServer.key");

                //var type2 = "urn:example.com/Claim/type-zyx";
                //await testSession.ClaimPostAsync(auth.Id, type2, value, "http://example.com/issuers/test")
                //    .AssertAsync(HttpStatusCode.Created);
                
                //session = await testSession.CreateSessionWithCredentialsAsync(credential);

                //session.SessionHeader.Value.GetClaimsJwtString(
                //    (claims) =>
                //    {
                //        var thisClaim = claims.First(clm => String.Compare(clm.Type, type2) == 0);
                //        Assert.AreEqual(value, thisClaim.Value);
                //        return true;
                //    },
                //    (why) =>
                //    {
                //        Assert.Fail(why);
                //        return false;
                //    },
                //    "AuthServer.issuer", "AuthServer.key");
            });
        }
    }
}
