using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.CreateReport;

public class CreateReportHandler(
    ApplicationDbContext dbContext,
    IReportImageCloudinaryService imageCloudinaryService,
    IOptions<CloudinaryOptions> options)
{
    private readonly CloudinaryOptions _options = options.Value;

    public async Task<ReportResponse> HandleAsync(
        CreateReportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = ReportUserClaims.GetUserId(user);
        ValidateCreateReportRequest(request, userId);
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
    
    private void ValidateCreateReportRequest(CreateReportRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Description is required.");
        }

        if (request.CategoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.");
        }

        if (request.Images is null || request.Images.Count == 0)
        {
            throw new ArgumentException("At least one image is required.");
        }

        if (request.Images.Count > _options.MaxImagesPerReport)
        {
            throw new ArgumentException($"A report can include at most {_options.MaxImagesPerReport} images.");
        }

        if (string.IsNullOrWhiteSpace(request.County))
        {
            throw new ArgumentException("County is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RoadName))
        {
            throw new ArgumentException("Road name is required.");
        }

        foreach (var image in request.Images)
        {
            if (image is null)
            {
                throw new ArgumentException("Image metadata is required.");
            }

            if (string.IsNullOrWhiteSpace(image.PublicId))
            {
                throw new ArgumentException("Cloudinary public ID is required.");
            }

            if (string.IsNullOrWhiteSpace(image.Version))
            {
                throw new ArgumentException("Cloudinary version is required.");
            }

            if (string.IsNullOrWhiteSpace(image.Signature))
            {
                throw new ArgumentException("Cloudinary signature is required.");
            }

            if (!image.PublicId.Trim().StartsWith(
                    $"{imageCloudinaryService.GetUserFolder(userId)}/",
                    StringComparison.Ordinal))
            {
                throw new ArgumentException("Image was not uploaded for the current user.");
            }

            if (!imageCloudinaryService.IsUploadResultValid(image))
            {
                throw new ArgumentException("Cloudinary image signature is invalid.");
            }
        }
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
            report.Status,
            confirmationCount,
            report.Created,
            report.LastModified);
    }
}
