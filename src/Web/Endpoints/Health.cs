using Web.Contracts;
using Web.Infrastructure;

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

    private static IResult GetHealth(IConfiguration configuration)
    {
        var databaseConnectionString = configuration.GetConnectionString("DefaultConnection");

        var data = new HealthStatus(
            Status: "Healthy",
            DatabaseConfigured: !string.IsNullOrWhiteSpace(databaseConnectionString),
            CheckedAtUtc: DateTimeOffset.UtcNow);

        return Results.Ok(ApiResponse<HealthStatus>.Ok(data, "PublicPulse API is running."));
    }
}

public sealed record HealthStatus(string Status, bool DatabaseConfigured, DateTimeOffset CheckedAtUtc);
