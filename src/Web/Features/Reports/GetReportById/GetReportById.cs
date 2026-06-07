using Microsoft.EntityFrameworkCore;
using Web.Common.Models;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.GetReportById;

public sealed class GetReportByIdHandler(ApplicationDbContext dbContext)
{
    public async Task<ApplicationResult<ReportResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports
            .AsNoTracking()
            .Include(report => report.Category)
            .Include(report => report.Confirmations)
            .Include(report => report.Images)
            .SingleOrDefaultAsync(report => report.Id == id, cancellationToken);

        if (report is null)
        {
            return ApplicationResult<ReportResponse>.NotFound("Report was not found.");
        }

        return ApplicationResult<ReportResponse>.Success(
            ReportResponseMapper.ToReportResponse(report, report.Confirmations.Count));
    }
}
