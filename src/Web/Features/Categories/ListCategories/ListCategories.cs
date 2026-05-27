using Microsoft.EntityFrameworkCore;
using Web.Features.Categories;
using Web.Infrastructure.Persistence;

namespace Web.Features.Categories.ListCategories;

public sealed class ListCategoriesHandler(ApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<CategoryResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Description))
            .ToListAsync(cancellationToken);
    }
}
