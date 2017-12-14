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
            Func<HttpResponseMessage, Resources.Role, TResult> callback)
        {
            //Create the order via post
            var resource = new Resources.Role()
            {
                Id = Guid.NewGuid(),
                Actor = actorId,
                Name = role,
            };

            var response = await session.PostAsync<Controllers.RoleController>(resource);
            return callback(response, resource);
        }
        
        public static async Task<TResult> RolesGetByIdAsync<TResult>(this ITestSession session,
            WebId roleId,
            Func<HttpResponseMessage, Func<Resources.Role>, TResult> callback)
        {
            var query = new Resources.RoleQuery
            {
                Id = roleId,
            };
            var response = await session.GetAsync<Controllers.RoleController>(query);
            return callback(response,
                () => response.GetContent<Resources.Role>());
        }

        public static async Task<TResult> RolesGetByActorAsync<TResult>(this ITestSession session,
            WebId actorId,
            Func<HttpResponseMessage, Func<Resources.Role[]>, TResult> callback)
        {
            var query = new Resources.RoleQuery
            {
                Actor = actorId,
            };
            var response = await session.GetAsync<Controllers.RoleController>(query);
            return callback(response,
                () => response.GetContentMultipart<Resources.Role>().ToArray());
        }

        public static async Task<HttpResponseMessage> RolesDeleteByIdAsync(this ITestSession session,
            WebId roleId)
        {
            var query = new Resources.RoleQuery
            {
                Id = roleId,
            };
            var response = await session.DeleteAsync<Controllers.RoleController>(query);
            return response;
        }
    }
}
