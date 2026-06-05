using Microsoft.AspNetCore.Http.HttpResults;
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
            .WithName(nameof(Reverse))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    private static async Task<Ok<ApiResponse<LocationLookupResponse>>> Reverse(
        double latitude,
        double longitude,
        ReverseGeocodeHandler handler,
        CancellationToken cancellationToken)
    {
        var location = await handler.HandleAsync(latitude, longitude, cancellationToken);

        return TypedResults.Ok(ApiResponse<LocationLookupResponse>.Ok(
            location,
            "Location lookup completed successfully."));
    }
}
