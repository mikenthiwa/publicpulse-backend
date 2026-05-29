using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Web.Features.Locations;
using Web.IntegrationTests.Helpers;

namespace Web.IntegrationTests.Features.Locations;

public sealed class LocationEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public LocationEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(-91, 36.817223)]
    [InlineData(91, 36.817223)]
    [InlineData(-1.286389, -181)]
    [InlineData(-1.286389, 181)]
    public async Task Reverse_WithOutOfRangeCoordinates_ShouldReturnBadRequest(
        double latitude,
        double longitude)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/Locations/reverse?latitude={latitude}&longitude={longitude}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reverse_WithValidCoordinates_ShouldReturnLocationLookupResponse()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/Locations/reverse?latitude=-1.286389&longitude=36.817223",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var location = await ApiTestClient.ReadDataAsync<LocationLookupResponse>(
            response,
            TestContext.Current.CancellationToken);

        location.County.Should().Be("Nairobi");
        location.RoadName.Should().Be("Kenyatta Avenue");
        location.LocationLabel.Should().Be("Kenyatta Avenue, Nairobi, Kenya");
        location.Latitude.Should().Be(-1.286389);
        location.Longitude.Should().Be(36.817223);
        location.Source.Should().Be("fake");
        location.Confidence.Should().Be(LocationConfidence.High);
    }

    [Fact]
    public async Task Reverse_WhenProviderFails_ShouldReturnBadGatewayProblemDetails()
    {
        using var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IReverseGeocodingProvider>();
                    services.AddScoped<IReverseGeocodingProvider, FailingReverseGeocodingProvider>();
                });
            })
            .CreateClient();

        var response = await client.GetAsync(
            "/api/Locations/reverse?latitude=-1.286389&longitude=36.817223",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private sealed class FailingReverseGeocodingProvider : IReverseGeocodingProvider
    {
        public Task<LocationLookupResponse> ReverseGeocodeAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            throw new ReverseGeocodingProviderException("Reverse geocoding failed.");
        }
    }
}
