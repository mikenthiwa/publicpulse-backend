using Microsoft.EntityFrameworkCore;
using Web.Common.Models;
using Web.Features.Categories;
using Web.Infrastructure;
using Web.Infrastructure.Persistence;

namespace Web.Endpoints;

public class Categories : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet(GetCategories);
    }

    private static async Task<IResult> GetCategories(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description))
            .ToListAsync(cancellationToken);

        return Results.Ok(ApiResponse<IReadOnlyList<CategoryResponse>>.Ok(
            categories,
            "Categories retrieved successfully."));
    }
}
