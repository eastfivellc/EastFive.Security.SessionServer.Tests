using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using BlackBarLabs.Api.Tests;
using System.Net.Http;
using BlackBarLabs.Api.Resources;
using EastFive.Api.Azure.Credentials;
using EastFive.Api.Azure.Credentials.Controllers;

namespace EastFive.Security.SessionServer.Api.Tests
{
    public static class AuthorizationHelpers
    {
        public static async Task<TResult> SessionPostAsync<TResult>(this ITestSession session,
            WebId requestId,
            string method, AuthenticationActions action, 
            Uri redirectAddressDesired, Uri redirectAddressDesiredPostLogout,
            Func<HttpResponseMessage, EastFive.Api.Azure.Credentials.Resources.Session, Func<EastFive.Api.Azure.Credentials.Resources.Session>, TResult> callback)
        {
            //Create the order via post
            var resource = new EastFive.Api.Azure.Credentials.Resources.Session()
            {
                Id = requestId,
                Method = Enum.GetName(typeof(CredentialValidationMethodTypes), method),
                LocationAuthenticationReturn = redirectAddressDesired,
                LocationLogoutReturn = redirectAddressDesiredPostLogout,
            };

            var response = await session.PostAsync<SessionController>(resource);
            return callback(response, resource,
                () => response.GetContent<EastFive.Api.Azure.Credentials.Resources.Session>());
        }

        public static async Task<TResult> IntegrationPostAsync<TResult>(this ITestSession session,
            WebId requestId,
            string method, Guid authorizationId, Uri redirect,
            Func<HttpResponseMessage, EastFive.Api.Azure.Credentials.Resources.Integration, Func<EastFive.Api.Azure.Credentials.Resources.Integration>, TResult> callback)
        {
            //Create the order via post
            var resource = new EastFive.Api.Azure.Credentials.Resources.Integration()
            {
                Id = requestId,
                Method = Enum.GetName(typeof(CredentialValidationMethodTypes), method),
                AuthorizationId = authorizationId,
                LocationAuthenticationReturn = redirect,
            };

            var response = await session.PostAsync<EastFive.Api.Azure.Credentials.Controllers.IntegrationController>(resource);
            return callback(response, resource,
                () => response.GetContent<EastFive.Api.Azure.Credentials.Resources.Integration>());
        }

        public static async Task<TResult> IntegrationPutAsync<TResult>(this ITestSession session,
           WebId requestId,
           string method, Guid authorizationId, Uri redirect,
           IDictionary<string, EastFive.Api.Azure.Credentials.Resources.AuthorizationRequest.CustomParameter> userParams,
           Func<HttpResponseMessage, EastFive.Api.Azure.Credentials.Resources.Integration, Func<EastFive.Api.Azure.Credentials.Resources.Integration>, TResult> callback)
        {
            var resource = new EastFive.Api.Azure.Credentials.Resources.Integration()
            {
                Id = requestId,
                Method = method,
                AuthorizationId = authorizationId,
                LocationAuthenticationReturn = redirect,
                UserParameters = userParams
            };

            var response = await session.PutAsync<EastFive.Api.Azure.Credentials.Controllers.IntegrationController>(resource);
            return callback(response, resource,
                () => response.GetContent<EastFive.Api.Azure.Credentials.Resources.Integration>());
        }

        public static async Task<TResult> AuthenticationRequestGetAsync<TResult>(this ITestSession session,
            WebId authenticationRequestId,
            Func<HttpResponseMessage, Func<EastFive.Api.Azure.Credentials.Resources.Session>, TResult> callback)
        {
            var query = new EastFive.Api.Azure.Credentials.Resources.Queries.SessionQuery
            {
                Id = authenticationRequestId,
            };
            var response = await session.GetAsync<EastFive.Api.Azure.Credentials.Controllers.SessionController>(query);
            return callback(response,
                () => response.GetContent<EastFive.Api.Azure.Credentials.Resources.Session>());
        }
        
        public static async Task<TResult> IntegrationGetAsync<TResult>(this ITestSession session,
            WebId integrationId,
            Func<HttpResponseMessage, Func<EastFive.Api.Azure.Credentials.Resources.Integration>, TResult> callback)
        {
            var query = new EastFive.Api.Azure.Credentials.Resources.Queries.IntegrationQuery
            {
                Id = integrationId,
            };
            var response = await session.GetAsync<EastFive.Api.Azure.Credentials.Controllers.IntegrationController>(query);
            return callback(response,
                () => response.GetContent<EastFive.Api.Azure.Credentials.Resources.Integration>());
        }

        public static async Task<TResult> IntegrationGetByAuthorizationAsync<TResult>(this ITestSession session,
            WebId actorId,
            Func<HttpResponseMessage, Func<EastFive.Api.Azure.Credentials.Resources.Integration[]>, TResult> callback)
        {
            var query = new EastFive.Api.Azure.Credentials.Resources.Queries.IntegrationQuery
            {
                ActorId = actorId,
            };
            var response = await session.GetAsync<EastFive.Api.Azure.Credentials.Controllers.IntegrationController>(query);
            return callback(response,
                () => response.GetContentMultipart<EastFive.Api.Azure.Credentials.Resources.Integration>().ToArray());
        }

        public static async Task<HttpResponseMessage> IntegrationDeleteAsync(this ITestSession session,
            WebId integrationId)
        {
            var query = new EastFive.Api.Azure.Credentials.Resources.Queries.IntegrationQuery
            {
                Id = integrationId,
            };
            var response = await session.GetAsync<EastFive.Api.Azure.Credentials.Controllers.IntegrationController>(query);
            return response;
        }
    }
}
