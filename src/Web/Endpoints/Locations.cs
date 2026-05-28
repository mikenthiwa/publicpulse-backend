using Web.Common.Models;
using Web.Features.Locations;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Locations : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet("/reverse", Reverse)
            .WithName(nameof(Reverse));
    }

    private static async Task<IResult> Reverse(
        double latitude,
        double longitude,
        ReverseGeocodeHandler handler,
        CancellationToken cancellationToken)
    {
        var location = await handler.HandleAsync(latitude, longitude, cancellationToken);

        return Results.Ok(ApiResponse<LocationLookupResponse>.Ok(
            location,
            "Location lookup completed successfully."));
    }
}
