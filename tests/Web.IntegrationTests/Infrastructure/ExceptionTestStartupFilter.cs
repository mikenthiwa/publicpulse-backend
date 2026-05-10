using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Web.IntegrationTests;

public sealed class ExceptionTestStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            next(app);

            app.Map("/test/unhandled-exception", testApp =>
            {
                testApp.Run(_ => throw new Exception("Test exception."));
            });
        };
    }
}
