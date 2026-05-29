using Web.Domain.Entities;

namespace Web.Features.Reports;

internal static class ReportResponseMapper
{
    public static ReportResponse ToReportResponse(Report report, int confirmationCount)
    {
        return new ReportResponse(
            report.Id,
            report.Description,
            report.CategoryId,
            report.Category.Name,
            report.Images
                .Select(image => new ReportImageResponse(
                    image.Id,
                    image.ImageUrl,
                    image.PublicId))
                .ToArray(),
            report.County,
            report.RoadName,
            report.Latitude,
            report.Longitude,
            report.LocationLabel,
            report.LocationSource,
            report.Status,
            confirmationCount,
            report.Created,
            report.LastModified);
    }
}
