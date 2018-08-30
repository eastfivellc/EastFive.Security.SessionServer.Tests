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
    public static class RolesActions
    {
        public static async Task<TResult> RolesPostAsync<TResult>(this ITestSession session,
            WebId actorId, string role,
            Func<HttpResponseMessage, EastFive.Api.Azure.Resources.Role, TResult> callback)
        {
            //Create the order via post
            var resource = new EastFive.Api.Azure.Resources.Role()
            {
                Id = Guid.NewGuid(),
                Actor = actorId,
                Name = role,
            };

            var response = await session.PostAsync<EastFive.Api.Azure.Controllers.RoleController>(resource);
            return callback(response, resource);
        }
        
        public static async Task<TResult> RolesGetByIdAsync<TResult>(this ITestSession session,
            WebId roleId,
            Func<HttpResponseMessage, Func<EastFive.Api.Azure.Resources.Role>, TResult> callback)
        {
            var query = new EastFive.Api.Azure.Resources.Queries.RoleQuery
            {
                Id = roleId,
            };
            var response = await session.GetAsync<EastFive.Api.Azure.Controllers.RoleController>(query);
            return callback(response,
                () => response.GetContent<EastFive.Api.Azure.Resources.Role>());
        }

        public static async Task<TResult> RolesGetByActorAsync<TResult>(this ITestSession session,
            WebId actorId,
            Func<HttpResponseMessage, Func<EastFive.Api.Azure.Resources.Role[]>, TResult> callback)
        {
            var query = new EastFive.Api.Azure.Resources.Queries.RoleQuery
            {
                Actor = actorId,
            };
            var response = await session.GetAsync<EastFive.Api.Azure.Controllers.RoleController>(query);
            return callback(response,
                () => response.GetContentMultipart<EastFive.Api.Azure.Resources.Role>().ToArray());
        }

        public static async Task<HttpResponseMessage> RolesDeleteByIdAsync(this ITestSession session,
            WebId roleId)
        {
            var query = new EastFive.Api.Azure.Resources.Queries.RoleQuery
            {
                Id = roleId,
            };
            var response = await session.DeleteAsync<EastFive.Api.Azure.Controllers.RoleController>(query);
            return response;
        }
    }
}
