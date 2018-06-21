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
        public static async Task<TResult> SessionPostAsync<TResult>(this ITestSession session,
            WebId requestId,
            string method, AuthenticationActions action, 
            Uri redirectAddressDesired, Uri redirectAddressDesiredPostLogout,
            Func<HttpResponseMessage, Resources.Session, Func<Resources.Session>, TResult> callback)
        {
            //Create the order via post
            var resource = new Resources.Session()
            {
                Id = requestId,
                Method = Enum.GetName(typeof(CredentialValidationMethodTypes), method),
                LocationAuthenticationReturn = redirectAddressDesired,
                LocationLogoutReturn = redirectAddressDesiredPostLogout,
            };

            var response = await session.PostAsync<Controllers.SessionController>(resource);
            return callback(response, resource,
                () => response.GetContent<Resources.Session>());
        }

        public static async Task<TResult> IntegrationPostAsync<TResult>(this ITestSession session,
            WebId requestId,
            string method, Guid authorizationId, Uri redirect,
            Func<HttpResponseMessage, Resources.Integration, Func<Resources.Integration>, TResult> callback)
        {
            //Create the order via post
            var resource = new Resources.Integration()
            {
                Id = requestId,
                Method = Enum.GetName(typeof(CredentialValidationMethodTypes), method),
                AuthorizationId = authorizationId,
                LocationAuthenticationReturn = redirect,
            };

            var response = await session.PostAsync<Controllers.IntegrationController>(resource);
            return callback(response, resource,
                () => response.GetContent<Resources.Integration>());
        }

        public static async Task<TResult> IntegrationPutAsync<TResult>(this ITestSession session,
           WebId requestId,
           string method, Guid authorizationId, Uri redirect,
           IDictionary<string, Resources.AuthorizationRequest.CustomParameter> userParams,
           Func<HttpResponseMessage, Resources.Integration, Func<Resources.Integration>, TResult> callback)
        {
            var resource = new Resources.Integration()
            {
                Id = requestId,
                Method = method,
                AuthorizationId = authorizationId,
                LocationAuthenticationReturn = redirect,
                UserParameters = userParams
            };

            var response = await session.PutAsync<Controllers.IntegrationController>(resource);
            return callback(response, resource,
                () => response.GetContent<Resources.Integration>());
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
        
        public static async Task<TResult> IntegrationGetAsync<TResult>(this ITestSession session,
            WebId integrationId,
            Func<HttpResponseMessage, Func<Resources.Integration>, TResult> callback)
        {
            var query = new Resources.Queries.IntegrationQuery
            {
                Id = integrationId,
            };
            var response = await session.GetAsync<Controllers.IntegrationController>(query);
            return callback(response,
                () => response.GetContent<Resources.Integration>());
        }

        public static async Task<TResult> IntegrationGetByAuthorizationAsync<TResult>(this ITestSession session,
            WebId actorId,
            Func<HttpResponseMessage, Func<Resources.Integration[]>, TResult> callback)
        {
            var query = new Resources.Queries.IntegrationQuery
            {
                ActorId = actorId,
            };
            var response = await session.GetAsync<Controllers.IntegrationController>(query);
            return callback(response,
                () => response.GetContentMultipart<Resources.Integration>().ToArray());
        }

        public static async Task<HttpResponseMessage> IntegrationDeleteAsync(this ITestSession session,
            WebId integrationId)
        {
            var query = new Resources.Queries.IntegrationQuery
            {
                Id = integrationId,
            };
            var response = await session.GetAsync<Controllers.IntegrationController>(query);
            return response;
        }
    }
}
