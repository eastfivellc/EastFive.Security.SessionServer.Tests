using BlackBarLabs.Api.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Security.AuthorizationServer.API.Tests
{
    public static class SessionBuilder
    {
        internal static async Task SessionAsync(Func<ITestSession, Task> callback)
        {
            await TestSession.StartAsync(
                async (session) =>
                {
                    await callback(session);
                });
        }
    }
}
