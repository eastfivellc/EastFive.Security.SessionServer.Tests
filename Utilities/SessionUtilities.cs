using BlackBarLabs.Api.Tests;
using BlackBarLabs.Extensions;
using EastFive.Api.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace EastFive.Security.SessionServer.Tests
{
    public static class SessionUtilities
    {
        private static async Task InitAsync()
        {
            var mailService = new MockMailService();
            mailService.SendEmailMessageCallback =
                (templateName, toAddress, toName, fromAddress, fromName,
                 subject, substitutionsSingle) => true.ToTask();
            var timeService = new BlackBarLabs.Api.Tests.Mocks.MockTimeService();
            var httpConfig = new HttpConfiguration();
            EastFive.Api.Services.ServiceConfiguration.Initialize(httpConfig,
                () => mailService, () => timeService);
            var authProviderMock = new EastFive.Api.Tests.ProvideLoginMock();

            Assert.IsTrue(
                await ServiceConfiguration.InitializeAsync(authProviderMock, httpConfig,
                    new Func<
                        Func<IProvideAuthorization, IProvideAuthorization[]>,
                        Func<EastFive.Security.SessionServer.IProvideAuthorization[]>,
                        Func<string, EastFive.Security.SessionServer.IProvideAuthorization[]>,
                            Task<EastFive.Security.SessionServer.IProvideAuthorization[]>>[]
                    {
                        ProvideLoginMock.InitializeAsync,
                    },
                () => true,
                (why) =>
                {
                    Assert.Fail(why);
                    return false;
                }));
        }

        public static async Task<TResult> StartAsync<TResult>(Func<TestSession, TResult> callback)
        {
            await InitAsync();
            return TestSession.Start(
                (session) =>
                {
                    return callback(session);
                });

        }

        public static async Task<TResult> StartAsync<TResult>(Guid sessionActorId, Func<TestSession, TResult> callback)
        {
            await InitAsync();
            return TestSession.Start(sessionActorId,
                (session) =>
                {
                    return callback(session);
                });

        }

        public static ITestSession GetSuperAdmin(this ITestSession session)
        {
            return Web.Configuration.Settings.GetString(EastFive.Api.Configuration.SecurityDefinitions.SiteAdminAuthorization,
                (siteAdminAuthToken) =>
                {
                    var superAdminSession = new TestSession(siteAdminAuthToken);
                    return superAdminSession;
                },
                (why) =>
                {
                    Assert.Fail(why);
                    throw new Exception(why);
                });

        }
    }
}
