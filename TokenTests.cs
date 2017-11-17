using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using BlackBarLabs.Persistence.Azure.StorageTables;
using BlackBarLabs.Security.AuthorizationServer.Exceptions;
using BlackBarLabs.Security.CredentialProvider.Facebook.Tests;
using BlackBarLabs.Web.Utilities.Routing;
using BlackBarLabs.Web.Utilities.Tests.Doubles.Stubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlackBarLabs.Security.AuthorizationServer.API.Tests
{
    [TestClass]
    public class TokenTest
    {
        protected IResolveUrls urlResolver;
        protected ResponseModifier responseModifier;
        protected Context context;

        protected CredentialProvider.Doubles.Stubs.StubCredentialProvider.ModifierDelegate credentialResponse = 
            (provider, username, token) => Task.FromResult(Guid.NewGuid().ToString());

        [TestInitialize]
        public void Init()
        {
            urlResolver = new global::BlackBarLabs.Web.Utilities.Tests.Doubles.Fakes.UrlResolver();
            responseModifier = new ResponseModifier();
            context = new Context(() =>
            {
                const string connectionStringKeyName = "Azure.Authorization.Storage";
                return new Persistence.Azure.DataContext(connectionStringKeyName);
            },
            (providerMethod) => new CredentialProvider.Doubles.Stubs.StubCredentialProvider((provider, username, token) =>
                credentialResponse(provider, username, token)));
        }


    


    }
}
