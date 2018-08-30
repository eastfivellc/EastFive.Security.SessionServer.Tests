using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using System.Net.Http;
using EastFive.Api.Azure.Credentials.Resources;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class CredentialActions
    {
        public static async Task<TResult> CredentialPostAsync<TResult>(this ITestSession session,
            EastFive.Api.Azure.Credentials.CredentialValidationMethodTypes method, string subject, Guid authentication,
            Func<HttpResponseMessage, Credential, TResult> callback)
        {
            //Create the order via post
            var resource = new Credential()
            {
                Id = Guid.NewGuid(),
                Authentication = authentication,
                Method = method,
                Subject = subject,
            };

            var response = await session.PostAsync<EastFive.Api.Azure.Credentials.Controllers.CredentialController>(resource);
            return callback(response, resource);
        }
    }
}
