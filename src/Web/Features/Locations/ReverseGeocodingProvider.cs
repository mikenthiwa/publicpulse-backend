using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Web.Infrastructure;

namespace Web.Features.Locations;

public interface IReverseGeocodingProvider
{
    Task<LocationLookupResponse> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken);
}

public sealed class ReverseGeocodingProviderException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class MapboxReverseGeocodingProvider(
    HttpClient httpClient,
    IOptions<MapboxOptions> options,
    ILogger<MapboxReverseGeocodingProvider> logger) : IReverseGeocodingProvider
{
    public const string SourceName = "mapbox";

    public async Task<LocationLookupResponse> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var accessToken = options.Value.AccessToken;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ProviderConfigurationException("Mapbox access token is missing.");
        }

        var uri = new UriBuilder(new Uri(new Uri(options.Value.BaseUrl), "search/geocode/v6/reverse"))
        {
            Query = string.Join(
                "&",
                $"longitude={Uri.EscapeDataString(longitude.ToString(System.Globalization.CultureInfo.InvariantCulture))}",
                $"latitude={Uri.EscapeDataString(latitude.ToString(System.Globalization.CultureInfo.InvariantCulture))}",
                "country=ke",
                $"access_token={Uri.EscapeDataString(accessToken)}")
        }.Uri;

        HttpResponseMessage response;

        try
        {
            response = await httpClient.GetAsync(uri, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ReverseGeocodingProviderException("Mapbox reverse geocoding request timed out.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new ReverseGeocodingProviderException("Mapbox reverse geocoding request failed.", exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Mapbox reverse geocoding failed with status code {StatusCode}.",
                response.StatusCode);

            throw new ReverseGeocodingProviderException(
                response.StatusCode == HttpStatusCode.Unauthorized
                    ? "Mapbox reverse geocoding is not authorized."
                    : "Mapbox reverse geocoding request failed.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        try
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return MapResponse(document.RootElement, latitude, longitude);
        }
        catch (JsonException exception)
        {
            throw new ReverseGeocodingProviderException("Mapbox reverse geocoding returned an invalid response.", exception);
        }
    }

    internal static LocationLookupResponse MapResponse(JsonElement root, double latitude, double longitude)
    {
        var features = root.TryGetProperty("features", out var featuresElement)
            && featuresElement.ValueKind == JsonValueKind.Array
            ? featuresElement.EnumerateArray().ToArray()
            : [];

        if (features.Length == 0)
        {
            return new LocationLookupResponse(
                County: null,
                RoadName: null,
                LocationLabel: null,
                Latitude: latitude,
                Longitude: longitude,
                Source: SourceName,
                Confidence: LocationConfidence.Low);
        }

        var roadFeature = features.FirstOrDefault(IsRoadLevelFeature);
        var selectedFeature = roadFeature.ValueKind == JsonValueKind.Undefined
            ? features[0]
            : roadFeature;

        var county = FindContextName(features, "district")
            ?? FindContextName(features, "region")
            ?? FindContextName(features, "place");
        var roadName = FindRoadName(selectedFeature) ?? FindRoadName(features[0]);
        var locationLabel = GetFeatureString(selectedFeature, "full_address")
            ?? GetFeatureString(selectedFeature, "place_formatted")
            ?? GetFeatureString(selectedFeature, "name");
        var confidence = GetConfidence(roadName, county, selectedFeature);

        return new LocationLookupResponse(
            County: county,
            RoadName: roadName,
            LocationLabel: locationLabel,
            Latitude: latitude,
            Longitude: longitude,
            Source: SourceName,
            Confidence: confidence);
    }

    private static bool IsRoadLevelFeature(JsonElement feature)
    {
        var featureType = GetFeatureType(feature);

        return string.Equals(featureType, "address", StringComparison.OrdinalIgnoreCase)
            || string.Equals(featureType, "street", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindRoadName(JsonElement feature)
    {
        var featureType = GetFeatureType(feature);

        if (string.Equals(featureType, "address", StringComparison.OrdinalIgnoreCase)
            || string.Equals(featureType, "street", StringComparison.OrdinalIgnoreCase))
        {
            return GetFeatureString(feature, "name")
                ?? GetFeatureString(feature, "full_address");
        }

        return null;
    }

    private static string? FindContextName(JsonElement[] features, string contextType)
    {
        foreach (var feature in features)
        {
            var contextName = FindContextName(feature, contextType);

            if (!string.IsNullOrWhiteSpace(contextName))
            {
                return contextName;
            }
        }

        return features
            .Select(feature => string.Equals(GetFeatureType(feature), contextType, StringComparison.OrdinalIgnoreCase)
                ? GetFeatureString(feature, "name")
                : null)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
    }

    private static string? FindContextName(JsonElement feature, string contextType)
    {
        if (!feature.TryGetProperty("properties", out var properties)
            || !properties.TryGetProperty("context", out var context)
            || context.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (context.TryGetProperty(contextType, out var typedContext))
        {
            return GetString(typedContext, "name");
        }

        foreach (var item in context.EnumerateObject())
        {
            if (item.Name.StartsWith($"{contextType}.", StringComparison.OrdinalIgnoreCase))
            {
                return GetString(item.Value, "name");
            }
        }

        return null;
    }

    private static string GetConfidence(string? roadName, string? county, JsonElement selectedFeature)
    {
        if (!string.IsNullOrWhiteSpace(roadName) && !string.IsNullOrWhiteSpace(county))
        {
            return LocationConfidence.High;
        }

        if (!string.IsNullOrWhiteSpace(roadName)
            || !string.IsNullOrWhiteSpace(county)
            || IsRoadLevelFeature(selectedFeature))
        {
            return LocationConfidence.Medium;
        }

        return LocationConfidence.Low;
    }

    private static string? GetFeatureType(JsonElement feature)
    {
        if (feature.TryGetProperty("properties", out var properties))
        {
            return GetString(properties, "feature_type");
        }

        return null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(property.GetString())
            ? property.GetString()
            : null;
    }

    private static string? GetFeatureString(JsonElement feature, string propertyName)
    {
        return GetString(feature, propertyName)
            ?? (feature.TryGetProperty("properties", out var properties)
                ? GetString(properties, propertyName)
                : null);
    }
}
