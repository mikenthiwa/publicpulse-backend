using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Common.Models;
using Web.Domain.Entities;
using Web.Features.Auth;
using Web.Infrastructure.Persistence;

namespace Web.Features.Auth.Login;

public sealed class LoginHandler(
    ApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService)
{
    public async Task<ApplicationResult<AuthResponse>> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

        if (user is null)
        {
            return ApplicationResult<AuthResponse>.BadRequest("Invalid email or password.");
        }

        var passwordVerification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordVerification == PasswordVerificationResult.Failed)
        {
            return ApplicationResult<AuthResponse>.BadRequest("Invalid email or password.");
        }

        return ApplicationResult<AuthResponse>.Success(jwtTokenService.CreateToken(user));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
