using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Common.Mappings;
using Web.Common.Models;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.ListReport;

public sealed record ListReportRequest
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;
    public const int MaximumPageSize = 100;

    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
}



public sealed class ListReportHandler(ApplicationDbContext dbContext)
{
    public async Task<PaginatedList<ReportListItemResponse>> HandleAsync(
        ListReportRequest request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber ?? ListReportRequest.DefaultPageNumber;
        var pageSize = request.PageSize ?? ListReportRequest.DefaultPageSize;

        return await dbContext.Reports
            .AsNoTracking()
            .OrderByDescending(report => report.Created)
            .ThenByDescending(report => report.Id)
            .Select(report => new ReportListItemResponse(
                report.Id,
                report.CategoryId,
                report.Category.Name,
                report.County,
                report.RoadName,
                report.Latitude,
                report.Longitude,
                report.LocationLabel,
                report.LocationSource,
                report.Status,
                report.Confirmations.Count,
                report.Created))
            .PaginateAsync(pageNumber, pageSize, cancellationToken);
    }
}
