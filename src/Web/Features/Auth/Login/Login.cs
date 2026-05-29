using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Entities;
using Web.Features.Auth;
using Web.Infrastructure.Persistence;

namespace Web.Features.Auth.Login;

public sealed class LoginHandler(
    ApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService)
{
    public async Task<AuthResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        var passwordVerification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordVerification == PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        return jwtTokenService.CreateToken(user);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
