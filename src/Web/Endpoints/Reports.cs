using System.Security.Claims;
using Web.Contracts;
using Web.Features.Reports;
using Web.Features.Reports.CreateReport;
using Web.Features.Reports.ListReport;
using Web.Infrastructure;

namespace Web.Endpoints;

public class Reports : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("", ListReports);
        group.MapGet("/{id:guid}", GetReportById)
            .WithName(nameof(GetReportById));
        group.MapPost("/images/upload-url", CreateImageUploadUrl)
            .WithName(nameof(CreateImageUploadUrl))
            .RequireAuthorization();
        group.MapPut("/images/uploads/{token}", UploadLocalImage)
            .WithName(nameof(UploadLocalImage));
        group.MapPost("", CreateReport)
            .WithName(nameof(CreateReport))
            .RequireAuthorization();
        group.MapPost("/{id:guid}/confirmations", ConfirmReport)
            .WithName(nameof(ConfirmReport));
        group.MapPut("/{id:guid}/status", UpdateReportStatus)
            .WithName(nameof(UpdateReportStatus))
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateImageUploadUrl(
        CreateReportImageUploadUrlRequest request,
        ClaimsPrincipal user,
        IReportImageUploadService imageUploadService,
        CancellationToken cancellationToken)
    {
        var upload = await imageUploadService.CreateUploadUrlAsync(request, user, cancellationToken);

        return Results.Ok(ApiResponse<ReportImageUploadUrlResponse>.Ok(upload, "Upload URL created."));
    }

    private static async Task<IResult> UploadLocalImage(
        string token,
        HttpRequest request,
        IReportImageStorageService storageService,
        CancellationToken cancellationToken)
    {
        await storageService.UploadLocalAsync(token, request, cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> CreateReport(
        CreateReportRequest request,
        ClaimsPrincipal user,
        IReportService reportService,
        CreateReportHandler handler,
        CancellationToken cancellationToken)
    {
        var report = await handler.HandleAsync(request, user, cancellationToken);

        return Results.Created(
            $"/api/Reports/{report.Id}",
            ApiResponse<ReportResponse>.Ok(report, "Report created successfully."));
    }

    private static async Task<IResult> ListReports(
        ListReportHandler handler,
        CancellationToken cancellationToken)
    {
        var reports = await handler.HandleAsync(cancellationToken);

        return Results.Ok(ApiResponse<IReadOnlyList<ReportListItemResponse>>.Ok(
            reports,
            "Reports retrieved successfully."));
    }

    private static async Task<IResult> GetReportById(
        Guid id,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        var report = await reportService.GetByIdAsync(id, cancellationToken);

        return Results.Ok(ApiResponse<ReportResponse>.Ok(report, "Report retrieved successfully."));
    }

    private static async Task<IResult> ConfirmReport(
        Guid id,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        var confirmation = await reportService.ConfirmAsync(id, cancellationToken);

        return Results.Ok(ApiResponse<ConfirmReportResponse>.Ok(
            confirmation,
            "Report confirmed successfully."));
    }

    private static async Task<IResult> UpdateReportStatus(
        Guid id,
        UpdateReportStatusRequest request,
        ClaimsPrincipal user,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        var report = await reportService.UpdateStatusAsync(id, request, user, cancellationToken);

        return Results.Ok(ApiResponse<ReportResponse>.Ok(
            report,
            "Report status updated successfully."));
    }
}
