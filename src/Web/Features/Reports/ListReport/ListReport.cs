using Microsoft.EntityFrameworkCore;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.ListReport;

public sealed class ListReportHandler(ApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<ReportListItemResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Reports
            .AsNoTracking()
            .OrderByDescending(report => report.Created)
            .Select(report => new ReportListItemResponse(
                report.Id,
                report.CategoryId,
                report.Category.Name,
                report.County,
                report.RoadName,
                report.Status,
                report.Confirmations.Count,
                report.Created))
            .ToListAsync(cancellationToken);
    }
}
