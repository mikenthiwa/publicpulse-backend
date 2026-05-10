using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Web.IntegrationTests;

public sealed class DevelopmentWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? _previousDefaultConnection;

    public DevelopmentWebApplicationFactory()
    {
        _previousDefaultConnection = Environment.GetEnvironmentVariable(TestConnectionStrings.DefaultConnectionKey);
        Environment.SetEnvironmentVariable(
            TestConnectionStrings.DefaultConnectionKey,
            TestConnectionStrings.DefaultConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable(
            TestConnectionStrings.DefaultConnectionKey,
            _previousDefaultConnection);

        base.Dispose(disposing);
    }
}
