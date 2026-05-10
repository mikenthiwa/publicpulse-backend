using System.Net;
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
}
