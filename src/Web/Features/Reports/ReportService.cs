using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports;

public interface IReportService
{
    Task<ReportResponse> CreateAsync(
        CreateReportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportListItemResponse>> ListAsync(CancellationToken cancellationToken);

    Task<ReportResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ConfirmReportResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken);

    Task<ReportResponse> UpdateStatusAsync(
        Guid id,
        UpdateReportStatusRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken);
}

public sealed class ReportService(
    ApplicationDbContext dbContext,
    IReportImageUploadService imageUploadService) : IReportService
{
    public async Task<ReportResponse> CreateAsync(
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
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            PhotoUrl = request.PhotoUrl.Trim(),
            County = request.County.Trim(),
            RoadName = request.RoadName.Trim(),
            CreatedByUserId = userId
        };

        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(report)
            .Reference(currentReport => currentReport.Category)
            .LoadAsync(cancellationToken);

        return ToReportResponse(report, confirmationCount: 0);
    }

    public async Task<IReadOnlyList<ReportListItemResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Reports
            .AsNoTracking()
            .OrderByDescending(report => report.CreatedAtUtc)
            .Select(report => new ReportListItemResponse(
                report.Id,
                report.Title,
                report.CategoryId,
                report.Category.Name,
                report.County,
                report.RoadName,
                report.Status,
                report.Confirmations.Count,
                report.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReportResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports
            .AsNoTracking()
            .Include(report => report.Category)
            .Include(report => report.Confirmations)
            .SingleOrDefaultAsync(report => report.Id == id, cancellationToken);

        if (report is null)
        {
            throw new KeyNotFoundException("Report was not found.");
        }

        return ToReportResponse(report, report.Confirmations.Count);
    }

    public async Task<ConfirmReportResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var reportExists = await dbContext.Reports
            .AnyAsync(report => report.Id == id, cancellationToken);

        if (!reportExists)
        {
            throw new KeyNotFoundException("Report was not found.");
        }

        dbContext.ReportConfirmations.Add(new ReportConfirmation
        {
            ReportId = id
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var confirmationCount = await dbContext.ReportConfirmations
            .CountAsync(confirmation => confirmation.ReportId == id, cancellationToken);

        return new ConfirmReportResponse(id, confirmationCount);
    }

    public async Task<ReportResponse> UpdateStatusAsync(
        Guid id,
        UpdateReportStatusRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = ReportUserClaims.GetUserId(user);
        var report = await dbContext.Reports
            .Include(report => report.Category)
            .Include(report => report.Confirmations)
            .SingleOrDefaultAsync(report => report.Id == id, cancellationToken);

        if (report is null)
        {
            throw new KeyNotFoundException("Report was not found.");
        }

        if (report.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the report creator can update status.");
        }

        report.Status = request.Status;
        report.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToReportResponse(report, report.Confirmations.Count);
    }

    private static ReportResponse ToReportResponse(Report report, int confirmationCount)
    {
        return new ReportResponse(
            report.Id,
            report.Title,
            report.Description,
            report.CategoryId,
            report.Category.Name,
            report.PhotoUrl,
            report.County,
            report.RoadName,
            report.Status,
            confirmationCount,
            report.CreatedAtUtc,
            report.UpdatedAtUtc);
    }

    private static void ValidateCreateReportRequest(CreateReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.");
        }

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
}
