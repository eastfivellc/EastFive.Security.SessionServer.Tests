using EastFive.Api.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Azure.Tests
{
    public static class AdminWorkflows
    {
        public static Task<bool> PurgeSystemAsync()
        {
            return BlackBarLabs.Persistence.Azure.StorageTables.AzureStorageRepository.Connection(
                async azureStorageRepository =>
                {
                    // new Persistence.DataContext(OrderOwl.AppSettingKeys.ASTConnectionStringKey);
                    var purgeSuccess = await azureStorageRepository.PurgeAsync();
                    Assert.IsTrue(purgeSuccess);
                    return purgeSuccess;
                });
        }

        private static ITestApplication genesisSession = default(TestApplication);

        public static async Task<ITestApplication> SessionSuperAdminAsync(this ITestApplicationFactory testAppFactory)
        {
            return await EastFive.Web.Configuration.Settings.GetGuid(EastFive.Api.AppSettings.AuthorizationIdSuperAdmin,
                async (superAdminAuthorizationId) =>
                {
                    var sessionToRun = testAppFactory.GetUnauthorizedSession();
                    var token = await sessionToRun.PostAsync(
                        new EastFive.Azure.Auth.Session
                        {
                            sessionId = Guid.NewGuid().AsRef<EastFive.Azure.Auth.Session>(),
                            authorization = superAdminAuthorizationId.AsRefOptional<EastFive.Azure.Auth.Authorization>(),
                        },
                        onCreatedBody:
                        (sessionCreated, contentType) =>
                        {
                            return ((EastFive.Azure.Auth.Session)sessionCreated).token;
                        });
                    var testApplication = testAppFactory.GetAuthorizedSession(token);
                    return testApplication;
                },
                (why) =>
                {
                    Assert.Fail(why);
                    throw new Exception(why);
                });
        }

        public static async Task<ITestApplication> SessionUnauthenticatedAsync(this ITestApplicationFactory testAppFactory)
        {
            var sessionToRun = testAppFactory.GetUnauthorizedSession();
            var token = await sessionToRun.PostAsync(
                new EastFive.Azure.Auth.Session
                {
                    sessionId = Guid.NewGuid().AsRef<EastFive.Azure.Auth.Session>(),
                },
                onCreatedBody:
                (sessionCreated, contentType) =>
                {
                    return ((EastFive.Azure.Auth.Session)sessionCreated).token;
                });
            var testApplication = testAppFactory.GetAuthorizedSession(token);
            return testApplication;
        }

    }
}
