using System;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using BlackBarLabs.Security.Authorization;
using BlackBarLabs.Security.AuthorizationServer.API.Controllers;
using BlackBarLabs.Security.CredentialProvider.Facebook.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlackBarLabs.Security.AuthorizationServer.API.Tests
{
    public static class CredentialHelpers
    {
        public static async Task<Resources.Credential> CreateCredentialFacebookAsync(this ITestSession testSession, 
            Guid authId)
        {
            string userId, token;
            CredentialProviderFacebookTests.CreateFbCredentials(out userId, out token);

            var credential = new Resources.CredentialPost
            {
                AuthorizationId = authId,
                Method = CredentialValidationMethodTypes.Facebook,
                Provider = new Uri("http://api.facebook.com"),
                UserId = userId,
                Token = token,
            };
            await testSession.PostAsync<CredentialController>(credential)
                .AssertAsync(HttpStatusCode.Created);
            return credential;
        }

        public static async Task<Resources.Credential> CreateCredentialVoucherAsync(this ITestSession testSession,
            Guid authId, TimeSpan duration = default(TimeSpan))
        {
            if (duration == default(TimeSpan))
                duration = TimeSpan.FromMinutes(10.0);

            var trustedVoucherProverId = CredentialProvider.Voucher.Utilities.GetTrustedProviderId();
            var token = Tokens.VoucherTools.GenerateToken(authId, DateTime.UtcNow + duration,
                (t) => t,
                (configName) => { Assert.Fail("Invalid config:" + configName); return string.Empty; },
                (configName, issue) => { Assert.Fail("Invalid config:" + configName + "--" + issue); return string.Empty; });
            var credentialVoucher = new Resources.CredentialPost
            {
                AuthorizationId = authId,
                Method = CredentialValidationMethodTypes.Voucher,
                Provider = trustedVoucherProverId,
                Token = token,
                UserId = authId.ToString("N"),
            };
            await testSession.PostAsync<CredentialController>(credentialVoucher)
                .AssertAsync(HttpStatusCode.Created);
            return credentialVoucher;
        }

        public static async Task<Resources.Credential> CreateCredentialImplicitAsync(this ITestSession testSession,
            Guid authId, string username = default(string), string password = default(string),
            Uri [] claimsProviders = default(Uri[]))
        {
            if (default(string) == username)
                username = Guid.NewGuid().ToString("N");
            if (default(string) == password)
                password = Guid.NewGuid().ToString("N");

            var trustedVoucherProverId = CredentialProvider.Voucher.Utilities.GetTrustedProviderId();
            var credentialImplicit = new Resources.CredentialPost
            {
                AuthorizationId = authId,
                Method = CredentialValidationMethodTypes.Implicit,
                Provider = trustedVoucherProverId,
                UserId = username,
                Token = password,
            };
            await testSession.PostAsync<CredentialController>(credentialImplicit)
                .AssertAsync(HttpStatusCode.Created);
            return credentialImplicit;
        }


        public static async Task<Resources.Credential> UpdateCredentialImplicitAsync(this ITestSession testSession,
            Guid authId, string username = default(string), string password = default(string),
            Uri[] claimsProviders = default(Uri[]))
        {
            if (default(string) == username)
                username = Guid.NewGuid().ToString("N");
            if (default(string) == password)
                password = Guid.NewGuid().ToString("N");

            var trustedVoucherProverId = CredentialProvider.Voucher.Utilities.GetTrustedProviderId();
            var credentialImplicit = new Resources.CredentialPut
            {
                AuthorizationId = authId,
                Method = CredentialValidationMethodTypes.Implicit,
                Provider = trustedVoucherProverId,
                UserId = username,
                Token = password,
            };
            await testSession.PutAsync<CredentialController>(credentialImplicit)
                .AssertAsync(HttpStatusCode.Created);
            return credentialImplicit;
        }

        public static async Task<string> GetCredentialImplicitAsync(this ITestSession testSession,
            string username, Action found, Action notFound )
        {
            var trustedVoucherProverId = CredentialProvider.Voucher.Utilities.GetTrustedProviderId();
            var credentialImplicitGet = new Resources.CredentialGet
            {
                Method = CredentialValidationMethodTypes.Implicit,
                Provider = trustedVoucherProverId,
                UserId = username
            };
            var response = await testSession.GetAsync<CredentialController>(credentialImplicitGet);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                found();
                return response.Content.ToString();
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                notFound();
                return string.Empty;
            }
            return string.Empty;
        }

    }
}
