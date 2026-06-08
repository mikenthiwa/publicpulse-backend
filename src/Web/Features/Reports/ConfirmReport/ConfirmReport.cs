using Microsoft.EntityFrameworkCore;
using Web.Common.Models;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.ConfirmReport;

public sealed class ConfirmReportHandler(ApplicationDbContext dbContext)
{
    public async Task<ApplicationResult<ConfirmReportResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reportExists = await dbContext.Reports
            .AnyAsync(report => report.Id == id, cancellationToken);

        if (!reportExists)
        {
            return ApplicationResult<ConfirmReportResponse>.NotFound("Report was not found.");
        }

        dbContext.ReportConfirmations.Add(new ReportConfirmation
        {
            ReportId = id
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var confirmationCount = await dbContext.ReportConfirmations
            .CountAsync(confirmation => confirmation.ReportId == id, cancellationToken);

        return ApplicationResult<ConfirmReportResponse>.Success(
            new ConfirmReportResponse(id, confirmationCount));
    }
}
