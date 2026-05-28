using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Web.Common.Models;
using Web.Features.Reports;
using Web.Features.Reports.ConfirmReport;
using Web.Features.Reports.CreateImageUploadSignature;
using Web.Features.Reports.CreateReport;
using Web.Features.Reports.GetReportById;
using Web.Features.Reports.ListReport;
using Web.Features.Reports.UpdateReportStatus;
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
        group.MapPost("/images/upload-signature", CreateImageUploadSignature)
            .WithName(nameof(CreateImageUploadSignature))
            .RequireAuthorization();
        group.MapPost("", CreateReport)
            .WithName(nameof(CreateReport))
            .AddFluentValidationAutoValidation()
            .RequireAuthorization();
        group.MapPost("/{id:guid}/confirmations", ConfirmReport)
            .WithName(nameof(ConfirmReport));
        group.MapPut("/{id:guid}/status", UpdateReportStatus)
            .WithName(nameof(UpdateReportStatus))
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateReport(
        CreateReportRequest request,
        CreateReportHandler handler,
        CancellationToken cancellationToken)
    {
        var report = await handler.HandleAsync(request, cancellationToken);

        return Results.Created(
            $"/api/Reports/{report.Id}",
            ApiResponse<ReportResponse>.Ok(report, "Report created successfully."));
    }

    private static IResult CreateImageUploadSignature(
        CreateImageUploadSignatureHandler handler)
    {
        var signature = handler.Handle();

        return Results.Ok(ApiResponse<ReportImageUploadSignatureResponse>.Ok(
            signature,
            "Upload signature created."));
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
        GetReportByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var report = await handler.HandleAsync(id, cancellationToken);

        return Results.Ok(ApiResponse<ReportResponse>.Ok(report, "Report retrieved successfully."));
    }

    private static async Task<IResult> ConfirmReport(
        Guid id,
        ConfirmReportHandler handler,
        CancellationToken cancellationToken)
    {
        var confirmation = await handler.HandleAsync(id, cancellationToken);

        return Results.Ok(ApiResponse<ConfirmReportResponse>.Ok(
            confirmation,
            "Report confirmed successfully."));
    }

    private static async Task<IResult> UpdateReportStatus(
        Guid id,
        UpdateReportStatusRequest request,
        UpdateReportStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var report = await handler.HandleAsync(id, request, cancellationToken);

        return Results.Ok(ApiResponse<ReportResponse>.Ok(
            report,
            "Report status updated successfully."));
    }
}
