using FluentAssertions;
using Microsoft.Extensions.Options;
using Web.Features.Auth;

namespace Web.UnitTests.Features.Auth;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_ShouldReturnTokenAndExpiry()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "PublicPulse.Tests",
            Audience = "PublicPulse.Tests",
            SigningKey = "public-pulse-unit-test-signing-key-with-enough-length",
            ExpiryMinutes = 30
        }));
        var user = new User
        {
            Email = "citizen@example.com",
            PasswordHash = "hash"
        };

        var response = service.CreateToken(user);

        response.UserId.Should().Be(user.Id);
        response.Email.Should().Be(user.Email);
        response.Token.Should().NotBeNullOrWhiteSpace();
        response.ExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }
}
