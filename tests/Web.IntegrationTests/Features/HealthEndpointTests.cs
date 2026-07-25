using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Web.Infrastructure.Persistence;

namespace Web.IntegrationTests.Features;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetLiveness_WhenDatabaseIsUnavailable_ShouldReturnOk()
    {
        await using var factory = new TestWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Replace(ServiceDescriptor.Scoped<IDatabaseHealthCheck, ThrowingDatabaseHealthCheck>());
                });
            });

        using var client = factory.CreateClient();
        var beforeRequest = DateTimeOffset.UtcNow;

        var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var afterRequest = DateTimeOffset.UtcNow;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LivenessResponse>(
            TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Message.Should().Be("PublicPulse API liveness check completed.");
        body.Data.Status.Should().Be("Healthy");
        body.Data.CheckedAtUtc.Should().BeOnOrAfter(beforeRequest);
        body.Data.CheckedAtUtc.Should().BeOnOrBefore(afterRequest);
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseIsUnavailable_ShouldReturnServiceUnavailable()
    {
        await using var factory = new TestWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Replace(ServiceDescriptor.Scoped<IDatabaseHealthCheck, UnavailableDatabaseHealthCheck>());
                });
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(
            TestContext.Current.CancellationToken);

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Data.DatabaseConfigured.Should().BeTrue();
        body.Data.DatabaseConnected.Should().BeFalse();
        body.Data.Status.Should().Be("Unhealthy");
    }

    private sealed record HealthResponse(bool Success, string Message, HealthData Data);

    private sealed record LivenessResponse(bool Success, string Message, LivenessData Data);

    private sealed record HealthData(
        string Status,
        bool DatabaseConfigured,
        bool DatabaseConnected,
        DateTimeOffset CheckedAtUtc);

    private sealed record LivenessData(string Status, DateTimeOffset CheckedAtUtc);

    private sealed class UnavailableDatabaseHealthCheck : IDatabaseHealthCheck
    {
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class ThrowingDatabaseHealthCheck : IDatabaseHealthCheck
    {
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Liveness must not query the database.");
        }
    }
}
