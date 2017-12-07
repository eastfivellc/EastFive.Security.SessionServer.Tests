using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using System.Net.Http;
using BlackBarLabs.Api.Resources;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class AuthorizationHelpers
    {
        public static async Task<TResult> AuthenticationRequestPostAsync<TResult>(this ITestSession session,
            WebId requestId,
            CredentialValidationMethodTypes method, AuthenticationActions action, Uri redirectAddressDesired,
            Func<HttpResponseMessage, Resources.Session, Func<Resources.Session>, TResult> callback)
        {
            //Create the order via post
            var resource = new Resources.Session()
            {
                Id = requestId,
                Method = method,
                Redirect = redirectAddressDesired,
            };

            var response = await session.PostAsync<Controllers.SessionController>(resource);
            return callback(response, resource,
                () => response.GetContent<Resources.Session>());
        }

        public static async Task<TResult> AuthenticationRequestLinkPostAsync<TResult>(this ITestSession session,
            WebId requestId,
            CredentialValidationMethodTypes method, Guid authorizationId, Uri redirect,
            Func<HttpResponseMessage, Resources.Session, Func<Resources.Session>, TResult> callback)
        {
            //Create the order via post
            var resource = new Resources.Integration()
            {
                Id = requestId,
                Method = method,
                AuthorizationId = authorizationId,
                Redirect = redirect,
            };

            var response = await session.PostAsync<Controllers.SessionController>(resource);
            return callback(response, resource,
                () => response.GetContent<Resources.Session>());
        }

        public static async Task<TResult> AuthenticationRequestGetAsync<TResult>(this ITestSession session,
            WebId authenticationRequestId,
            Func<HttpResponseMessage, Func<Resources.Session>, TResult> callback)
        {
            var query = new Resources.Queries.SessionQuery
            {
                Id = authenticationRequestId,
            };
            var response = await session.GetAsync<Controllers.SessionController>(query);
            return callback(response,
                () => response.GetContent<Resources.Session>());
        }
    }
}
