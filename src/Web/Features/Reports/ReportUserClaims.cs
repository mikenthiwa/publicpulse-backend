using System.Security.Claims;

namespace Web.Features.Reports;

internal static class ReportUserClaims
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedAccessException("Authenticated user id is invalid.");
        }

        return parsedUserId;
    }
}
