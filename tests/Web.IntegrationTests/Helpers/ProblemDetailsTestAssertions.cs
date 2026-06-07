using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Web.IntegrationTests.Helpers;

public static class ProblemDetailsTestAssertions
{
    public static async Task<TestProblemDetails> ShouldBeProblemDetailsAsync(
        this HttpResponseMessage response,
        HttpStatusCode status,
        string title,
        string detail,
        string instance,
        string type)
    {
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<TestProblemDetails>(
            TestContext.Current.CancellationToken);

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)status);
        problemDetails.Title.Should().Be(title);
        problemDetails.Detail.Should().Be(detail);
        problemDetails.Instance.Should().Be(instance);
        problemDetails.Type.Should().Be(type);
        problemDetails.TraceId.Should().NotBeNullOrWhiteSpace();

        return problemDetails;
    }

    public sealed record TestProblemDetails(
        string? Type,
        string? Title,
        int? Status,
        string? Detail,
        string? Instance,
        string? TraceId,
        Dictionary<string, string[]>? Errors);
}
