using Web.Common.Models;
using Web.Infrastructure;
using Web.Infrastructure.Persistence;

namespace Web.Endpoints;

public class Health : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetHealth);

        app.MapGet("/health", GetHealth)
        .WithName("HealthCheck")
        .WithTags("Health");
    }

    private static async Task<IResult> GetHealth(
        IConfiguration configuration,
        IDatabaseHealthCheck databaseHealthCheck,
        CancellationToken cancellationToken)
    {
        var databaseConnectionString = configuration.GetConnectionString("DefaultConnection");
        var databaseConfigured = !string.IsNullOrWhiteSpace(databaseConnectionString);
        var databaseConnected = databaseConfigured
            && await databaseHealthCheck.CanConnectAsync(cancellationToken);
        var status = databaseConnected ? "Healthy" : "Unhealthy";

        var data = new HealthStatus(
            Status: status,
            DatabaseConfigured: databaseConfigured,
            DatabaseConnected: databaseConnected,
            CheckedAtUtc: DateTimeOffset.UtcNow);

        var response = new ApiResponse<HealthStatus>(
            Success: databaseConnected,
            Message: "PublicPulse API health check completed.",
            Data: data);

        return databaseConnected
            ? Results.Ok(response)
            : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

public sealed record HealthStatus(
    string Status,
    bool DatabaseConfigured,
    bool DatabaseConnected,
    DateTimeOffset CheckedAtUtc);
