namespace Web.Features.Auth;

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(Guid UserId, string Email, string Token, DateTimeOffset ExpiresAtUtc);
