using CloudinaryDotNet;
using Microsoft.Extensions.Options;

namespace Web.Features.Reports;

public interface IReportImageCloudinaryService
{
    ReportImageUploadSignatureResponse CreateUploadSignature(Guid userId);

    bool IsUploadResultValid(CreateReportImageRequest image);

    string GetUserFolder(Guid userId);

    string CreateImageUrl(string publicId, string version);
}

public sealed class ReportImageUploadException(string message) : Exception(message);

public sealed class CloudinaryReportImageService(
    Cloudinary cloudinary,
    IOptions<CloudinaryOptions> options) : IReportImageCloudinaryService
{
    private readonly CloudinaryOptions _options = options.Value;

    public ReportImageUploadSignatureResponse CreateUploadSignature(Guid userId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var folder = GetUserFolder(userId);
        var parameters = new Dictionary<string, object>
        {
            ["folder"] = folder,
            ["timestamp"] = timestamp,
            ["upload_preset"] = _options.UploadPreset!
        };
        var signature = cloudinary.Api.SignParameters(parameters);

        return new ReportImageUploadSignatureResponse(
            _options.CloudName!,
            _options.ApiKey!,
            timestamp,
            folder,
            _options.UploadPreset!,
            signature);
    }

    public bool IsUploadResultValid(CreateReportImageRequest image)
    {
        return cloudinary.Api.VerifyApiResponseSignature(
            image.PublicId,
            image.Version,
            image.Signature);
    }

    public string GetUserFolder(Guid userId)
    {
        var rootFolder = string.IsNullOrWhiteSpace(_options.Folder)
            ? "public-pulse/reports"
            : _options.Folder.Trim().Trim('/');

        return $"{rootFolder}/{userId:N}";
    }

    public string CreateImageUrl(string publicId, string version)
    {
        var escapedPublicId = string.Join(
            "/",
            publicId
                .Trim()
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));

        return $"https://res.cloudinary.com/{_options.CloudName}/image/upload/v{version.Trim()}/{escapedPublicId}";
    }
}
