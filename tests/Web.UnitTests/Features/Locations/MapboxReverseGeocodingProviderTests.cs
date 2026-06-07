using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Web.Features.Locations;
using Web.Infrastructure;

namespace Web.UnitTests.Features.Locations;

public sealed class MapboxReverseGeocodingProviderTests
{
    [Fact]
    public async Task ReverseGeocodeAsync_WithRoadAndDistrictContext_ShouldMapLocation()
    {
        var provider = CreateProvider(
            """
            {
              "features": [
                {
                  "properties": {
                    "feature_type": "address",
                    "name": "Kenyatta Avenue",
                    "full_address": "Kenyatta Avenue, Nairobi, Kenya",
                    "context": {
                      "district": { "name": "Nairobi" },
                      "country": { "name": "Kenya" }
                    }
                  }
                }
              ]
            }
            """);

        var response = await provider.ReverseGeocodeAsync(
            -1.286389,
            36.817223,
            TestContext.Current.CancellationToken);

        response.County.Should().Be("Nairobi");
        response.RoadName.Should().Be("Kenyatta Avenue");
        response.LocationLabel.Should().Be("Kenyatta Avenue, Nairobi, Kenya");
        response.Source.Should().Be(MapboxReverseGeocodingProvider.SourceName);
        response.Confidence.Should().Be(LocationConfidence.High);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_WithEmptyResults_ShouldReturnLowConfidenceNullLocationFields()
    {
        var provider = CreateProvider("""{ "features": [] }""");

        var response = await provider.ReverseGeocodeAsync(
            -1.286389,
            36.817223,
            TestContext.Current.CancellationToken);

        response.County.Should().BeNull();
        response.RoadName.Should().BeNull();
        response.LocationLabel.Should().BeNull();
        response.Confidence.Should().Be(LocationConfidence.Low);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_WithProviderFailure_ShouldThrowProviderException()
    {
        var provider = CreateProvider("""{ "message": "failed" }""", HttpStatusCode.BadGateway);

        var action = async () => await provider.ReverseGeocodeAsync(
            -1.286389,
            36.817223,
            TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<ReverseGeocodingProviderException>();
    }

    [Fact]
    public async Task ReverseGeocodeAsync_WithoutAccessToken_ShouldThrowConfigurationException()
    {
        var provider = CreateProvider(
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            accessToken: string.Empty);

        var action = async () => await provider.ReverseGeocodeAsync(
            -1.286389,
            36.817223,
            TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<ProviderConfigurationException>()
            .WithMessage("Mapbox access token is missing.");
    }

    [Fact]
    public async Task ReverseGeocodeAsync_WithTimeout_ShouldThrowProviderException()
    {
        var provider = CreateProvider(_ => throw new TaskCanceledException("timeout"));

        var action = async () => await provider.ReverseGeocodeAsync(
            -1.286389,
            36.817223,
            TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<ReverseGeocodingProviderException>()
            .WithMessage("Mapbox reverse geocoding request timed out.");
    }

    private static MapboxReverseGeocodingProvider CreateProvider(
        string responseJson,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return CreateProvider(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseJson)
        });
    }

    private static MapboxReverseGeocodingProvider CreateProvider(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        string accessToken = "test-token")
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory));

        return new MapboxReverseGeocodingProvider(
            httpClient,
            Options.Create(new MapboxOptions { AccessToken = accessToken }),
            NullLogger<MapboxReverseGeocodingProvider>.Instance);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
