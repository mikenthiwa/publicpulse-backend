var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () =>
{
    var data = new HealthStatus(
        Status: "Healthy",
        DatabaseConfigured: !string.IsNullOrWhiteSpace(databaseConnectionString),
        CheckedAtUtc: DateTimeOffset.UtcNow);

    return Results.Ok(ApiResponse<HealthStatus>.Ok(data, "PublicPulse API is running."));
})
.WithName("HealthCheck");

app.Run();

public sealed record ApiResponse<T>(bool Success, string Message, T? Data)
{
    public static ApiResponse<T> Ok(T data, string message = "Request completed successfully.")
    {
        return new ApiResponse<T>(true, message, data);
    }
}

public sealed record HealthStatus(string Status, bool DatabaseConfigured, DateTimeOffset CheckedAtUtc);
