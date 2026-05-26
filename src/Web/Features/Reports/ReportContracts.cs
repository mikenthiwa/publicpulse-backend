using Web.Domain.Enums;

namespace Web.Features.Reports;

public sealed record CreateReportRequest(
    string Description,
    Guid CategoryId,
    IReadOnlyList<CreateReportImageRequest> Images,
    string County,
    string RoadName);

public sealed record CreateReportImageRequest(
    string PublicId,
    string Version,
    string Signature);

public sealed record ReportImageUploadSignatureResponse(
    string CloudName,
    string ApiKey,
    long Timestamp,
    string Folder,
    string UploadPreset,
    string Signature);

public sealed record UpdateReportStatusRequest(ReportStatus Status);

public sealed record ReportListItemResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string County,
    string RoadName,
    ReportStatus Status,
    int ConfirmationCount,
    DateTimeOffset Created);

public sealed record ReportResponse(
    Guid Id,
    string Description,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<ReportImageResponse> Images,
    string County,
    string RoadName,
    ReportStatus Status,
    int ConfirmationCount,
    DateTimeOffset Created,
    DateTimeOffset? LastModified);

public sealed record ReportImageResponse(
    Guid Id,
    string ImageUrl,
    string PublicId);

public sealed record ConfirmReportResponse(Guid ReportId, int ConfirmationCount);
