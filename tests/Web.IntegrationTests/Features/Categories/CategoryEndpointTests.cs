using System.Net;
using FluentAssertions;
using Web.Features.Categories;
using Web.IntegrationTests.Helpers;

namespace Web.IntegrationTests.Features.Categories;

public sealed class CategoryEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CategoryEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_ShouldReturnSeededCategories()
    {
        var response = await _client.GetAsync("/api/Categories", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await ApiTestClient.ReadDataAsync<IReadOnlyList<CategoryResponse>>(
            response,
            TestContext.Current.CancellationToken);

        categories.Select(category => category.Name)
            .Should()
            .Contain(["Roads", "Drainage", "Street Lights", "Bridges"]);
    }
}
