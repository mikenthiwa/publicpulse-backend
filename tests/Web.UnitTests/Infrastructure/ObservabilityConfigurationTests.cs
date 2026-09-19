using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Web.Infrastructure;

namespace Web.UnitTests.Infrastructure;

public sealed class ObservabilityConfigurationTests
{
    private const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    [Fact]
    public void AddObservability_WithoutConnectionString_DoesNotRegisterAzureMonitor()
    {
        var builder = CreateBuilder(connectionString: null);

        builder.AddObservability();

        Assert.DoesNotContain(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(TracerProvider));
    }

    [Fact]
    public void AddObservability_WithConnectionString_UsesLowVolumeOptions()
    {
        var builder = CreateBuilder(TestConnectionString);

        builder.AddObservability();

        Assert.Contains(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(TracerProvider));

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var azureMonitorOptions = serviceProvider
            .GetRequiredService<IOptions<AzureMonitorOptions>>()
            .Value;
        var loggerOptions = serviceProvider
            .GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>()
            .Value;

        Assert.False(azureMonitorOptions.EnableLiveMetrics);
        Assert.True(loggerOptions.IncludeScopes);
        Assert.True(loggerOptions.IncludeFormattedMessage);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/readiness")]
    public void AddObservability_WithConnectionString_FiltersHealthTraces(string path)
    {
        var builder = CreateBuilder(TestConnectionString);
        builder.AddObservability();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider
            .GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>()
            .Value;
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        Assert.NotNull(options.Filter);
        Assert.False(options.Filter(context));
    }

    [Fact]
    public void AddObservability_WithConnectionString_AllowsApiTraces()
    {
        var builder = CreateBuilder(TestConnectionString);
        builder.AddObservability();

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider
            .GetRequiredService<IOptions<AspNetCoreTraceInstrumentationOptions>>()
            .Value;
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/auth/login";

        Assert.NotNull(options.Filter);
        Assert.True(options.Filter(context));
    }

    private const string TestConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000";

    private static HostApplicationBuilder CreateBuilder(string? connectionString)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration[ConnectionStringKey] = connectionString ?? string.Empty;
        return builder;
    }
}
