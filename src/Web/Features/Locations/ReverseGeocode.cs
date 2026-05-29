namespace Web.Features.Locations;

public sealed class ReverseGeocodeHandler(IReverseGeocodingProvider provider)
{
    public Task<LocationLookupResponse> HandleAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentException("Longitude must be between -180 and 180.");
        }

        return provider.ReverseGeocodeAsync(latitude, longitude, cancellationToken);
    }
}
