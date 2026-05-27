using Web.Infrastructure.Identity;

namespace Web.Features.Reports.CreateImageUploadSignature;

public sealed class CreateImageUploadSignatureHandler(
    IReportImageCloudinaryService imageCloudinaryService,
    ICurrentUser currentUser)
{
    public ReportImageUploadSignatureResponse Handle()
    {
        return imageCloudinaryService.CreateUploadSignature(currentUser.UserId);
    }
}
