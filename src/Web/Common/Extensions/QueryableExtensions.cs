using Microsoft.EntityFrameworkCore;
using Web.Common.Models;

namespace Web.Common.Extensions;

public static class QueryableExtensions
{
    public static Task<PaginatedList<T>> PaginateAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        where T : class =>
        PaginatedList<T>.CreateAsync(source.AsNoTracking(), pageNumber, pageSize, cancellationToken);
}
