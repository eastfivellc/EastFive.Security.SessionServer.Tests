using EastFive.Api;
using EastFive.Api.Tests;
using EastFive.Azure.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Azure.Tests.Extensions
{
    public static class Extensions
    {
        public static async Task<TResult> ExecuteLoginRedirect<TRedirect, TResult>(this ITestApplicationFactory sessionFactory,
                string integrationName,
                Func<ITestApplication, TRedirect> getRedirect,
            Func<ITestApplication, Auth.Session, Auth.Authorization, Uri, TResult> onComplete)
        {
            var comms = sessionFactory.GetUnauthorizedSession();
            var responseResource = getRedirect(comms);
            //var authentication = await comms.GetAsync<Method, Method>(
            //    onContents:
            //        authentications =>
            //        {
            //            var matchingAuthentications = authentications
            //                .Where(auth => auth.name == integrationName);
            //            Assert.IsTrue(matchingAuthentications.Any());
            //            return matchingAuthentications.First();
            //        });

            return await await comms.GetAsync(responseResource,
                onRedirect:
                    async (urlRedirect, reason) =>
                    {
                        var authIdStr = urlRedirect.GetQueryParam(EastFive.Api.Azure.AzureApplication.QueryRequestIdentfier);
                        var authId = Guid.Parse(authIdStr);
                        var authIdRef = authId.AsRef<Auth.Authorization>();

                        // TODO: New comms here?
                        return await await comms.GetAsync(
                            (Auth.Authorization authorizationGet) => authorizationGet.authorizationId.AssignQueryValue(authIdRef),
                            onContent:
                                (authenticatedAuthorization) =>
                                {
                                    // Use comms here to avoid losing overides var commsRedirect = sessionFactory.GetUnauthorizedSession();
                                    var session = new Session
                                    {
                                        sessionId = Guid.NewGuid().AsRef<Session>(),
                                        authorization = new RefOptional<Auth.Authorization>(authenticatedAuthorization.authorizationId),
                                    };
                                    return comms.PostAsync(session, // commsRedirect.PostAsync(session,
                                        onCreatedBody:
                                            (updated, contentType) =>
                                            {
                                                var commsAuth = sessionFactory.GetAuthorizedSession(updated.token);
                                                return onComplete(commsAuth, updated, authenticatedAuthorization, urlRedirect);
                                            });
                                });
                    });
        }
    }
}
