using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Entities;
using Web.Features.Auth;
using Web.Infrastructure.Persistence;

namespace Web.Features.Auth.Register;

public sealed class RegisterHandler(
    ApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService)
{
    public async Task<AuthResponse> HandleAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var emailExists = await dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = string.Empty
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return jwtTokenService.CreateToken(user);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
