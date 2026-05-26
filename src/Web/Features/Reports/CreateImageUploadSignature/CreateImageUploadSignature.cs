using System.Security.Claims;

namespace Web.Features.Reports.CreateImageUploadSignature;

public sealed class CreateImageUploadSignatureHandler(
    IReportImageCloudinaryService imageCloudinaryService)
{
    public ReportImageUploadSignatureResponse Handle(ClaimsPrincipal user)
    {
        var userId = ReportUserClaims.GetUserId(user);

        return imageCloudinaryService.CreateUploadSignature(userId);
    }
}
