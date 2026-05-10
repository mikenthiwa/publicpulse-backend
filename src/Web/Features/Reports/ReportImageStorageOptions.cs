namespace Web.Features.Reports;

public sealed class ReportImageStorageOptions
{
    public const string SectionName = "ReportImages";

    public string Provider { get; init; } = "Local";
    public string? PublicBaseUrl { get; init; }
    public string? UploadBaseUrl { get; init; }
    public string? LocalStoragePath { get; init; }
    public string? SigningKey { get; init; }
    public int SignedUploadExpiryMinutes { get; init; } = 10;
    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
    public string? S3Endpoint { get; init; }
    public string? S3Region { get; init; }
    public string? S3Bucket { get; init; }
    public string? S3AccessKeyId { get; init; }
    public string? S3SecretAccessKey { get; init; }
    public bool S3UsePathStyle { get; init; } = true;
}
