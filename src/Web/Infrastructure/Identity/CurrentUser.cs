using System.Security.Claims;

namespace Web.Infrastructure.Identity;

public interface ICurrentUser
{
    ClaimsPrincipal User { get; }

    Guid UserId { get; }
}

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("Authenticated user is required.");

    public Guid UserId
    {
        get
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                throw new UnauthorizedAccessException("Authenticated user id is invalid.");
            }

            return parsedUserId;
        }
    }
}
