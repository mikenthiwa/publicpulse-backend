using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Web.IntegrationTests;

public sealed class ExceptionTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? _previousDefaultConnection;

    public ExceptionTestWebApplicationFactory()
    {
        _previousDefaultConnection = Environment.GetEnvironmentVariable(TestConnectionStrings.DefaultConnectionKey);
        Environment.SetEnvironmentVariable(
            TestConnectionStrings.DefaultConnectionKey,
            TestConnectionStrings.DefaultConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddTransient<IStartupFilter, ExceptionTestStartupFilter>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable(
            TestConnectionStrings.DefaultConnectionKey,
            _previousDefaultConnection);

        base.Dispose(disposing);
    }
}
