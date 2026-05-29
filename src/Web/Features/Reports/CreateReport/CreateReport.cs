using Microsoft.EntityFrameworkCore;
using Web.Domain.Entities;
using Web.Infrastructure.Identity;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.CreateReport;

public class CreateReportHandler(
    ApplicationDbContext dbContext,
    IReportImageCloudinaryService imageCloudinaryService,
    ICurrentUser currentUser)
{
    public async Task<ReportResponse> HandleAsync(
        CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var categoryExists = await dbContext.Categories
            .AnyAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new KeyNotFoundException("Category was not found.");
        }

        var publicIds = request.Images
            .Select(image => image.PublicId.Trim())
            .ToArray();

        var hasDuplicatePublicIds = publicIds
            .Distinct(StringComparer.Ordinal)
            .Count() != publicIds.Length;

        if (hasDuplicatePublicIds)
        {
            throw new ArgumentException("Report images must be unique.");
        }

        var hasUsedPublicIds = await dbContext.ReportImages
            .AnyAsync(image => publicIds.Contains(image.PublicId), cancellationToken);

        if (hasUsedPublicIds)
        {
            throw new ArgumentException("One or more images have already been used.");
        }

        var report = new Report
        {
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            County = request.County.Trim(),
            RoadName = request.RoadName.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationLabel = string.IsNullOrWhiteSpace(request.LocationLabel)
                ? null
                : request.LocationLabel.Trim(),
            LocationSource = string.IsNullOrWhiteSpace(request.LocationSource)
                ? null
                : request.LocationSource.Trim(),
            CreatedBy = userId
        };

        foreach (var image in request.Images)
        {
            report.Images.Add(new ReportImage
            {
                ReportId = report.Id,
                ImageUrl = imageCloudinaryService.CreateImageUrl(image.PublicId, image.Version),
                PublicId = image.PublicId.Trim(),
                CreatedBy = userId
            });
        }

        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(report)
            .Reference(currentReport => currentReport.Category)
            .LoadAsync(cancellationToken);

        await dbContext.Entry(report)
            .Collection(currentReport => currentReport.Images)
            .LoadAsync(cancellationToken);

        return ToReportResponse(report, confirmationCount: 0);
    }

    private static ReportResponse ToReportResponse(Report report, int confirmationCount)
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
