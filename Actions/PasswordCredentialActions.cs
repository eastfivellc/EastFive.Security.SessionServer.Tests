using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using System.Net.Http;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class CredentialActions
    {
        public static async Task<TResult> CredentialPostAsync<TResult>(this ITestSession session,
            CredentialValidationMethodTypes method, string subject, Guid authentication,
            Func<HttpResponseMessage, Resources.Credential, TResult> callback)
        {
            //Create the order via post
            var resource = new Resources.Credential()
            {
                Id = Guid.NewGuid(),
                Authentication = authentication,
                Method = method,
                Subject = subject,
            };

            var response = await session.PostAsync<Controllers.CredentialController>(resource);
            return callback(response, resource);
        }
    }
}
