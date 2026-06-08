using System.Security.Claims;

namespace Web.Infrastructure.Identity;

public interface ICurrentUser
{
    ClaimsPrincipal User { get; }

    Guid UserId { get; }
}

public sealed class CurrentUserException(string message) : Exception(message);

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User
        ?? throw new CurrentUserException("Authenticated user is required.");

    public Guid UserId
    {
        get
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                throw new CurrentUserException("Authenticated user id is invalid.");
            }

            return parsedUserId;
        }
    }
}
