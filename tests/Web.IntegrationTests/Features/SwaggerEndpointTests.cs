using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Web.IntegrationTests.Features;

public sealed class SwaggerEndpointTests : IClassFixture<DevelopmentWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SwaggerEndpointTests(DevelopmentWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSwaggerUi_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwaggerDocument_ShouldDescribeEndpointErrorResponses()
    {
        var response = await _client.GetAsync(
            "/swagger/v1/swagger.json",
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        var paths = document.RootElement.GetProperty("paths");

        GetResponseCodes(paths, "/api/Locations/reverse", "get")
            .Should().BeEquivalentTo(["200", "400", "502"]);
        GetResponseCodes(paths, "/api/Reports/{id}", "get")
            .Should().BeEquivalentTo(["200", "404"]);
        GetResponseCodes(paths, "/api/Reports/{id}/status", "put")
            .Should().BeEquivalentTo(["200", "401", "403", "404"]);
    }

    private static string[] GetResponseCodes(
        JsonElement paths,
        string path,
        string method)
    {
        return paths
            .GetProperty(path)
            .GetProperty(method)
            .GetProperty("responses")
            .EnumerateObject()
            .Select(response => response.Name)
            .ToArray();
    }
}
