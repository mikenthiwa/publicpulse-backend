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
    [InlineData(-91, 36.817223, "Latitude must be between -90 and 90.")]
    [InlineData(91, 36.817223, "Latitude must be between -90 and 90.")]
    [InlineData(-1.286389, -181, "Longitude must be between -180 and 180.")]
    [InlineData(-1.286389, 181, "Longitude must be between -180 and 180.")]
    public async Task Reverse_WithOutOfRangeCoordinates_ShouldReturnBadRequest(
        double latitude,
        double longitude,
        string detail)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/Locations/reverse?latitude={latitude}&longitude={longitude}",
            TestContext.Current.CancellationToken);

        await response.ShouldBeProblemDetailsAsync(
            HttpStatusCode.BadRequest,
            "Bad request.",
            detail,
            "/api/Locations/reverse",
            "https://tools.ietf.org/html/rfc7231#section-6.5.1");
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

        await response.ShouldBeProblemDetailsAsync(
            HttpStatusCode.BadGateway,
            "Upstream provider failed.",
            "Reverse geocoding failed.",
            "/api/Locations/reverse",
            "https://tools.ietf.org/html/rfc7231#section-6.6.3");
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
