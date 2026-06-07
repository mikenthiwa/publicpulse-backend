using Microsoft.AspNetCore.Http.HttpResults;
using Web.Common.Mappings;
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

    private static async Task<Results<Ok<ApiResponse<LocationLookupResponse>>, ProblemHttpResult>> Reverse(
        double latitude,
        double longitude,
        ReverseGeocodeHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(latitude, longitude, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(ApiResponse<LocationLookupResponse>.Ok(
                result.Value,
                "Location lookup completed successfully."))
            : result.ToProblemHttpResult(httpContext);
    }
}
