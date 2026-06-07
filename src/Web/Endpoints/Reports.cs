using Microsoft.AspNetCore.Http.HttpResults;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Web.Common.Mappings;
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

        group.MapGet("", ListReports)
            .AddFluentValidationAutoValidation()
            .ProducesProblem(StatusCodes.Status400BadRequest);
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

    private static async Task<Results<Created<ApiResponse<ReportResponse>>, ProblemHttpResult>> CreateReport(
        CreateReportRequest request,
        CreateReportHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/Reports/{result.Value.Id}",
                ApiResponse<ReportResponse>.Ok(
                    result.Value,
                    "Report created successfully."))
            : result.ToProblemHttpResult(httpContext);
    }

    private static Ok<ApiResponse<ReportImageUploadSignatureResponse>> CreateImageUploadSignature(
        CreateImageUploadSignatureHandler handler)
    {
        var signature = handler.Handle();

        return TypedResults.Ok(ApiResponse<ReportImageUploadSignatureResponse>.Ok(
            signature,
            "Upload signature created."));
    }

    private static async Task<Ok<ApiResponse<PaginatedList<ReportListItemResponse>>>> ListReports(
        ListReportHandler handler,
        [AsParameters] ListReportRequest request,
        CancellationToken cancellationToken)
    {
        var reports = await handler.HandleAsync(request, cancellationToken);

        return TypedResults.Ok(ApiResponse<PaginatedList<ReportListItemResponse>>.Ok(
            reports,
            "Reports retrieved successfully."));
    }

    private static async Task<Results<Ok<ApiResponse<ReportResponse>>, ProblemHttpResult>> GetReportById(
        Guid id,
        GetReportByIdHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(ApiResponse<ReportResponse>.Ok(
                result.Value,
                "Report retrieved successfully."))
            : result.ToProblemHttpResult(httpContext);
    }

    private static async Task<Results<Ok<ApiResponse<ConfirmReportResponse>>, ProblemHttpResult>> ConfirmReport(
        Guid id,
        ConfirmReportHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(ApiResponse<ConfirmReportResponse>.Ok(
                result.Value,
                "Report confirmed successfully."))
            : result.ToProblemHttpResult(httpContext);
    }

    private static async Task<Results<Ok<ApiResponse<ReportResponse>>, ProblemHttpResult>> UpdateReportStatus(
        Guid id,
        UpdateReportStatusRequest request,
        UpdateReportStatusHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(ApiResponse<ReportResponse>.Ok(
                result.Value,
                "Report status updated successfully."))
            : result.ToProblemHttpResult(httpContext);
    }
}
