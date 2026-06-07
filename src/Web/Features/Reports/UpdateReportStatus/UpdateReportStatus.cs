using Microsoft.EntityFrameworkCore;
using Web.Common.Models;
using Web.Infrastructure.Identity;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.UpdateReportStatus;

public sealed class UpdateReportStatusHandler(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ApplicationResult<ReportResponse>> HandleAsync(
        Guid id,
        UpdateReportStatusRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var report = await dbContext.Reports
            .Include(report => report.Category)
            .Include(report => report.Confirmations)
            .Include(report => report.Images)
            .SingleOrDefaultAsync(report => report.Id == id, cancellationToken);

        if (report is null)
        {
            return ApplicationResult<ReportResponse>.NotFound("Report was not found.");
        }

        if (report.CreatedBy != userId)
        {
            return ApplicationResult<ReportResponse>.Forbidden(
                "Only the report creator can update status.");
        }

        report.Status = request.Status;
        report.LastModified = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult<ReportResponse>.Success(
            ReportResponseMapper.ToReportResponse(report, report.Confirmations.Count));
    }
}
