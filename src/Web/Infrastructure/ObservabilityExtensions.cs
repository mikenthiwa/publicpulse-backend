using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;

namespace Web.Infrastructure;

public static class ObservabilityExtensions
{
    private const string ApplicationInsightsConnectionStringKey =
        "APPLICATIONINSIGHTS_CONNECTION_STRING";

    public static IHostApplicationBuilder AddObservability(
        this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration[
            ApplicationInsightsConnectionStringKey];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return builder;
        }

        builder.Services
            .AddOpenTelemetry()
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
                options.EnableLiveMetrics = false;
            });

        builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
        {
            options.Filter = context =>
                !context.Request.Path.StartsWithSegments("/health");
        });

        builder.Services.Configure<OpenTelemetryLoggerOptions>(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        });

        return builder;
    }
}
