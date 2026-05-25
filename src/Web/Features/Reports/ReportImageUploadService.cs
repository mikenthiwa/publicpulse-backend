using Microsoft.EntityFrameworkCore;
using Web.Infrastructure.Persistence;

namespace Web.Features.Reports;

public interface IReportImageUploadService
{
    Task MarkIssuedImageAsUsedAsync(
        string imageUrl,
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed class ReportImageUploadService(
    ApplicationDbContext dbContext) : IReportImageUploadService
{
    public async Task MarkIssuedImageAsUsedAsync(
        string imageUrl,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var trimmedImageUrl = imageUrl.Trim();
        var issuedUpload = await dbContext.ReportImageUploads
            .SingleOrDefaultAsync(
                upload => upload.ImageUrl == trimmedImageUrl && upload.CreatedBy == userId,
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
}
