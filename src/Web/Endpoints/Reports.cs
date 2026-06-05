using Microsoft.AspNetCore.Http.HttpResults;
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
            .WithName(nameof(GetReportById))
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/images/upload-signature", CreateImageUploadSignature)
            .WithName(nameof(CreateImageUploadSignature))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .RequireAuthorization();
        group.MapPost("", CreateReport)
            .WithName(nameof(CreateReport))
            .AddFluentValidationAutoValidation()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .RequireAuthorization();
        group.MapPost("/{id:guid}/confirmations", ConfirmReport)
            .WithName(nameof(ConfirmReport))
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/{id:guid}/status", UpdateReportStatus)
            .WithName(nameof(UpdateReportStatus))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    private static async Task<Created<ApiResponse<ReportResponse>>> CreateReport(
        CreateReportRequest request,
        CreateReportHandler handler,
        CancellationToken cancellationToken)
    {
        var report = await handler.HandleAsync(request, cancellationToken);

        return TypedResults.Created(
            $"/api/Reports/{report.Id}",
            ApiResponse<ReportResponse>.Ok(report, "Report created successfully."));
    }

    private static Ok<ApiResponse<ReportImageUploadSignatureResponse>> CreateImageUploadSignature(
        CreateImageUploadSignatureHandler handler)
    {
        var signature = handler.Handle();

        return TypedResults.Ok(ApiResponse<ReportImageUploadSignatureResponse>.Ok(
            signature,
            "Upload signature created."));
    }

    private static async Task<Ok<ApiResponse<IReadOnlyList<ReportListItemResponse>>>> ListReports(
        ListReportHandler handler,
        CancellationToken cancellationToken)
    {
        var reports = await handler.HandleAsync(cancellationToken);

        return TypedResults.Ok(ApiResponse<IReadOnlyList<ReportListItemResponse>>.Ok(
            reports,
            "Reports retrieved successfully."));
    }

    private static async Task<Ok<ApiResponse<ReportResponse>>> GetReportById(
        Guid id,
        GetReportByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var report = await handler.HandleAsync(id, cancellationToken);

        return TypedResults.Ok(ApiResponse<ReportResponse>.Ok(report, "Report retrieved successfully."));
    }

    private static async Task<Ok<ApiResponse<ConfirmReportResponse>>> ConfirmReport(
        Guid id,
        ConfirmReportHandler handler,
        CancellationToken cancellationToken)
    {
        var confirmation = await handler.HandleAsync(id, cancellationToken);

        return TypedResults.Ok(ApiResponse<ConfirmReportResponse>.Ok(
            confirmation,
            "Report confirmed successfully."));
    }

    private static async Task<Ok<ApiResponse<ReportResponse>>> UpdateReportStatus(
        Guid id,
        UpdateReportStatusRequest request,
        UpdateReportStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var report = await handler.HandleAsync(id, request, cancellationToken);

        return TypedResults.Ok(ApiResponse<ReportResponse>.Ok(
            report,
            "Report status updated successfully."));
    }
}
