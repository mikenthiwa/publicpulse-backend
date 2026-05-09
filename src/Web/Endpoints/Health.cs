using Web.Contracts;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Health : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGet("/health", () =>
        {
            var databaseConnectionString = app.Configuration.GetConnectionString("DefaultConnection");

            var data = new HealthStatus(
                Status: "Healthy",
                DatabaseConfigured: !string.IsNullOrWhiteSpace(databaseConnectionString),
                CheckedAtUtc: DateTimeOffset.UtcNow);

            return Results.Ok(ApiResponse<HealthStatus>.Ok(data, "PublicPulse API is running."));
        })
        .WithName("HealthCheck")
        .WithTags("Health");
    }
}

public sealed record HealthStatus(string Status, bool DatabaseConfigured, DateTimeOffset CheckedAtUtc);
