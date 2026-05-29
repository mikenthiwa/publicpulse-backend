using Microsoft.EntityFrameworkCore;
using Web.Infrastructure.Identity;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.UpdateReportStatus;

public sealed class UpdateReportStatusHandler(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser)
{
    public async Task<ReportResponse> HandleAsync(
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
            throw new KeyNotFoundException("Report was not found.");
        }

        if (report.CreatedBy != userId)
        {
            throw new UnauthorizedAccessException("Only the report creator can update status.");
        }

        report.Status = request.Status;
        report.LastModified = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ReportResponseMapper.ToReportResponse(report, report.Confirmations.Count);
    }
}
