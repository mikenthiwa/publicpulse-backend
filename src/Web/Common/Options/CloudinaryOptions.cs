namespace Web.Common.Options;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string? CloudName { get; init; }
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string Folder { get; init; } = "public-pulse/reports";
    public string? UploadPreset { get; init; }
    public int MaxImagesPerReport { get; init; } = 5;
}
