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
            Func<HttpResponseMessage, Resources.AuthenticationRequest, Func<Resources.AuthenticationRequest>, TResult> callback)
        {
            //Create the order via post
            var resource = new Resources.AuthenticationRequest()
            {
                Id = requestId,
                Method = method,
                Action = action,
                Redirect = redirectAddressDesired,
            };

            var response = await session.PostAsync<Controllers.AuthenticationRequestController>(resource);
            return callback(response, resource,
                () => response.GetContent<Resources.AuthenticationRequest>());
        }

        public static async Task<TResult> AuthenticationRequestLinkPostAsync<TResult>(this ITestSession session,
            WebId requestId,
            CredentialValidationMethodTypes method, Guid authorizationId, Uri redirect,
            Func<HttpResponseMessage, Resources.AuthenticationRequest, Func<Resources.AuthenticationRequest>, TResult> callback)
        {
            //Create the order via post
            var resource = new Resources.AuthenticationRequest()
            {
                Id = requestId,
                Method = method,
                Action = AuthenticationActions.link,
                AuthorizationId = authorizationId,
                Redirect = redirect,
            };

            var response = await session.PostAsync<Controllers.AuthenticationRequestController>(resource);
            return callback(response, resource,
                () => response.GetContent<Resources.AuthenticationRequest>());
        }

        public static async Task<TResult> AuthenticationRequestGetAsync<TResult>(this ITestSession session,
            WebId authenticationRequestId,
            Func<HttpResponseMessage, Func<Resources.AuthenticationRequest>, TResult> callback)
        {
            var query = new Resources.Queries.AuthenticationRequestQuery
            {
                Id = authenticationRequestId,
            };
            var response = await session.GetAsync<Controllers.AuthenticationRequestController>(query);
            return callback(response,
                () => response.GetContent<Resources.AuthenticationRequest>());
        }
    }
}
