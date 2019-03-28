using EastFive.Api.Azure.Credentials;
using EastFive.Api.Tests;
using EastFive.Extensions;
using EastFive.Security.SessionServer;
using EastFive.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace EastFive.Azure.Tests
{
    public class TestApplicationFactory : ITestApplicationFactory
    {
        private TestApplicationFactory()
        {
        }

        public static async Task<TestApplicationFactory> InitAsync()
        {
            var timeService = new EastFive.Web.Services.TimeService();
            Func<ISendMessageService> messageService = () => default(ISendMessageService);

            var httpConfig = new HttpConfiguration();
            ProvideLoginMock.method = CredentialValidationMethodTypes.Password;
            var identityServices = new Func<
                        Func<IProvideAuthorization, IProvideAuthorization[]>,
                        Func<IProvideAuthorization[]>,
                        Func<string, IProvideAuthorization[]>,
                            Task<IProvideAuthorization[]>>[]
                    {
                        ProvideLoginMock.InitializeAsync,
                    };
            EastFive.Api.Services.ServiceConfiguration.Initialize(httpConfig,
                messageService,
                () => timeService);
            return await (new TestApplicationFactory()).AsTask();
        }

        public ITestApplication GetAuthorizedSession(string token)
        {
            var testApplication = new TestApplication();
            testApplication.ApplicationStart();
            return testApplication;
        }

        public ITestApplication GetUnauthorizedSession()
        {
            var testApplication = new TestApplication();
            testApplication.ApplicationStart();
            return testApplication;
        }
    }

    public class TestApplication : Api.Azure.AzureApplication, ITestApplication
    {
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            var response = await EastFive.Api.Modules.ControllerHandler.DirectSendAsync(this, request, default(CancellationToken),
                (requestBack, token) =>
                {
                    Assert.Fail($"Failed to invoke `{request.RequestUri}`");
                    throw new Exception();
                });
            return response;
        }

        public TestApplication()
        {
            var baseMailService = base.SendMessageService;
            this.messageCallback = (templateName, toAddress, toName, fromAddress, fromName,
                subject, substitutionsSingle) =>
                    baseMailService.SendEmailMessageAsync(templateName, toAddress, toName,
                            fromAddress, fromName, subject,
                            substitutionsSingle, null,
                        (a) => true,
                        () => false,
                        (a) => false);
            this.Headers = new Dictionary<string, string>();
        }

        public TestApplication(string token)
        {
            var baseMailService = base.SendMessageService;
            this.messageCallback = (templateName, toAddress, toName, fromAddress, fromName,
                subject, substitutionsSingle) =>
                    baseMailService.SendEmailMessageAsync(templateName, toAddress, toName,
                            fromAddress, fromName, subject,
                            substitutionsSingle, null,
                        (a) => true,
                        () => false,
                        (a) => false);

            this.Headers = new Dictionary<string, string>()
            {
                { "Authorization", token }
            };
        }

        [Obsolete]
        public Guid ActorId => throw new NotImplementedException();

        public IDictionary<string, string> Headers { get; set;  }

        #region Mockable Services

        #region Login

        private static EastFive.Api.Tests.ProvideLoginMock loginService =
            default(EastFive.Api.Tests.ProvideLoginMock);
        public EastFive.Api.Tests.ProvideLoginMock LoginService
        {
            get
            {
                if (default(EastFive.Api.Tests.ProvideLoginMock) == loginService)
                    loginService = new EastFive.Api.Tests.ProvideLoginMock();
                return loginService;
            }
            set
            {
                loginService = value;
            }
        }

        #endregion

        #region Messaging

        public BlackBarLabs.Api.Tests.MockMailService.SendEmailMessageDelegate messageCallback;

        public override ISendMessageService SendMessageService =>
            new BlackBarLabs.Api.Tests.MockMailService(this.messageCallback);

        public TResult MockMailService<TResult>(BlackBarLabs.Api.Tests.MockMailService.SendEmailMessageDelegate callback,
            Func<TResult> onMocked)
        {
            var currentMailFetch = messageCallback;
            messageCallback = callback;
            var result = onMocked();
            messageCallback = currentMailFetch;
            return result;
        }

        #endregion

        #endregion


        #region Depricated overrides


        #endregion

    }
}
