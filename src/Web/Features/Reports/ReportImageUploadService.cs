using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports;

public interface IReportImageUploadService
{
    Task<ReportImageUploadUrlResponse> CreateUploadUrlAsync(
        CreateReportImageUploadUrlRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken);

    Task MarkIssuedImageAsUsedAsync(
        string imageUrl,
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed class ReportImageUploadService(
    ApplicationDbContext dbContext,
    IReportImageStorageService storageService,
    IOptions<ReportImageStorageOptions> options) : IReportImageUploadService
{
    private static readonly IReadOnlyDictionary<string, string> FileExtensionsByContentType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif"
        };

    private readonly ReportImageStorageOptions _options = options.Value;

    public async Task<ReportImageUploadUrlResponse> CreateUploadUrlAsync(
        CreateReportImageUploadUrlRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        ValidateUploadMetadata(request);

        var userId = ReportUserClaims.GetUserId(user);
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_options.SignedUploadExpiryMinutes);
        var imageKey = CreateImageKey(userId, request.ContentType);
        var uploadTarget = storageService.CreateUploadTarget(
            imageKey,
            request.ContentType.Trim(),
            request.ContentLength,
            expiresAtUtc);

        dbContext.ReportImageUploads.Add(new ReportImageUpload
        {
            ImageKey = imageKey,
            ImageUrl = uploadTarget.ImageUrl,
            ContentType = request.ContentType.Trim(),
            ContentLength = request.ContentLength,
            OriginalFileName = Path.GetFileName(request.FileName.Trim()),
            CreatedByUserId = userId,
            ExpiresAtUtc = expiresAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ReportImageUploadUrlResponse(
            uploadTarget.UploadUrl,
            uploadTarget.ImageUrl,
            imageKey,
            uploadTarget.Headers);
    }

    public async Task MarkIssuedImageAsUsedAsync(
        string imageUrl,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var trimmedImageUrl = imageUrl.Trim();
        var issuedUpload = await dbContext.ReportImageUploads
            .SingleOrDefaultAsync(
                upload => upload.ImageUrl == trimmedImageUrl && upload.CreatedByUserId == userId,
                cancellationToken);

        if (issuedUpload is null)
        {
            throw new ArgumentException("Photo URL must reference an image upload issued by this API.");
        }

        if (issuedUpload.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("Issued image upload has expired.");
        }

        if (issuedUpload.UsedAtUtc is not null)
        {
            throw new ArgumentException("Issued image upload has already been used.");
        }

        issuedUpload.UsedAtUtc = DateTimeOffset.UtcNow;
    }

    private void ValidateUploadMetadata(CreateReportImageUploadUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("File name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new ArgumentException("Content type is required.");
        }

        if (request.ContentLength <= 0)
        {
            throw new ArgumentException("Content length is required.");
        }

        if (!FileExtensionsByContentType.ContainsKey(request.ContentType.Trim()))
        {
            throw new ArgumentException("Content type is not supported.");
        }

        if (request.ContentLength > _options.MaxFileSizeBytes)
        {
            throw new ArgumentException("File size exceeds the 5 MB limit.");
        }
    }

    private static string CreateImageKey(Guid userId, string contentType)
    {
        var extension = FileExtensionsByContentType[contentType.Trim()];
        return $"reports/{userId:N}/{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}";
    }
}
