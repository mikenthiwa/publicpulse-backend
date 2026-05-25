using System.Security.Claims;
using Microsoft.Extensions.Options;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports.CreateUploadUrl;

public sealed class CreateUploadUrlHandler(
    ApplicationDbContext dbContext,
    IReportImageStorageService storageService,
    IOptions<ReportImageStorageOptions> options)
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

    public async Task<ReportImageUploadUrlResponse> HandleAsync(
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
            CreatedBy = userId,
            ExpiresAtUtc = expiresAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ReportImageUploadUrlResponse(
            uploadTarget.UploadUrl,
            uploadTarget.ImageUrl,
            imageKey,
            uploadTarget.Headers);
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
