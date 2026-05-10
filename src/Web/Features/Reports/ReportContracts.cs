namespace Web.Features.Reports;

public sealed record CreateReportRequest(
    string Title,
    string Description,
    Guid CategoryId,
    string PhotoUrl,
    string County,
    string RoadName);

public sealed record CreateReportImageUploadUrlRequest(
    string FileName,
    string ContentType,
    long ContentLength);

public sealed record ReportImageUploadUrlResponse(
    string UploadUrl,
    string ImageUrl,
    string ImageKey,
    IReadOnlyDictionary<string, string> Headers);

public sealed record UpdateReportStatusRequest(ReportStatus Status);

public sealed record ReportListItemResponse(
    Guid Id,
    string Title,
    Guid CategoryId,
    string CategoryName,
    string County,
    string RoadName,
    ReportStatus Status,
    int ConfirmationCount,
    DateTimeOffset CreatedAtUtc);

public sealed record ReportResponse(
    Guid Id,
    string Title,
    string Description,
    Guid CategoryId,
    string CategoryName,
    string PhotoUrl,
    string County,
    string RoadName,
    ReportStatus Status,
    int ConfirmationCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record ConfirmReportResponse(Guid ReportId, int ConfirmationCount);
