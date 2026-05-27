using Web.Common.Models;
using Web.Features.Categories;
using Web.Features.Categories.ListCategories;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Categories : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetCategories);
    }

    private static async Task<IResult> GetCategories(
        ListCategoriesHandler handler,
        CancellationToken cancellationToken)
    {
        var categories = await handler.HandleAsync(cancellationToken);

        return Results.Ok(ApiResponse<IReadOnlyList<CategoryResponse>>.Ok(
            categories,
            "Categories retrieved successfully."));
    }
}
