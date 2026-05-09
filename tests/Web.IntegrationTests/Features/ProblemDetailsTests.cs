using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Web.IntegrationTests.Features;

public sealed class ProblemDetailsTests : IClassFixture<ExceptionTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProblemDetailsTests(ExceptionTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnhandledException_ShouldReturnProblemDetails()
    {
        var response = await _client.GetAsync(
            "/test/unhandled-exception",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<TestProblemDetails>(
            TestContext.Current.CancellationToken);

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)HttpStatusCode.InternalServerError);
        problemDetails.Title.Should().Be("An unexpected error occurred.");
        problemDetails.Detail.Should().Be("The server encountered an unexpected error.");
        problemDetails.Instance.Should().Be("/test/unhandled-exception");
        problemDetails.TraceId.Should().NotBeNullOrWhiteSpace();
    }

    private sealed record TestProblemDetails(
        string? Type,
        string? Title,
        int? Status,
        string? Detail,
        string? Instance,
        string? TraceId);
}
