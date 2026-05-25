using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.CreateReport;


public class CreateReportHandler(ApplicationDbContext dbContext, IReportImageUploadService imageUploadService)
{
    public async Task<ReportResponse> HandleAsync(
        CreateReportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        ValidateCreateReportRequest(request);
        var userId = ReportUserClaims.GetUserId(user);
        var categoryExists = await dbContext.Categories
            .AnyAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new KeyNotFoundException("Category was not found.");
        }

        await imageUploadService.MarkIssuedImageAsUsedAsync(
            request.PhotoUrl,
            userId,
            cancellationToken);

        var report = new Report
        {
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            PhotoUrl = request.PhotoUrl.Trim(),
            County = request.County.Trim(),
            RoadName = request.RoadName.Trim(),
            CreatedBy = userId
        };

        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(report)
            .Reference(currentReport => currentReport.Category)
            .LoadAsync(cancellationToken);

        return ToReportResponse(report, confirmationCount: 0);
    }
    
    private static void ValidateCreateReportRequest(CreateReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Description is required.");
        }

        if (request.CategoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PhotoUrl))
        {
            throw new ArgumentException("Photo URL is required.");
        }

        if (string.IsNullOrWhiteSpace(request.County))
        {
            throw new ArgumentException("County is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RoadName))
        {
            throw new ArgumentException("Road name is required.");
        }
    }
    private static ReportResponse ToReportResponse(Report report, int confirmationCount)
    {
        return new ReportResponse(
            report.Id,
            report.Description,
            report.CategoryId,
            report.Category.Name,
            report.PhotoUrl,
            report.County,
            report.RoadName,
            report.Status,
            confirmationCount,
            report.Created,
            report.LastModified);
    }

}
