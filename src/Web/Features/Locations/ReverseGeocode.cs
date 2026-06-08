using Web.Common.Models;

namespace Web.Features.Locations;

public sealed class ReverseGeocodeHandler(IReverseGeocodingProvider provider)
{
    public async Task<ApplicationResult<LocationLookupResponse>> HandleAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        if (latitude is < -90 or > 90)
        {
            return ApplicationResult<LocationLookupResponse>.BadRequest(
                "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            return ApplicationResult<LocationLookupResponse>.BadRequest(
                "Longitude must be between -180 and 180.");
        }

        var location = await provider.ReverseGeocodeAsync(latitude, longitude, cancellationToken);

        return ApplicationResult<LocationLookupResponse>.Success(location);
    }
}
