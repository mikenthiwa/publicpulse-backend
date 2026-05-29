using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Web.Features.Auth;
using Web.IntegrationTests.Helpers;

namespace Web.IntegrationTests.Features.Auth;

public sealed class AuthEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldCreateUserAndReturnToken()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            new RegisterRequest("citizen@example.com", "Password123!"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await ApiTestClient.ReadDataAsync<AuthResponse>(
            response,
            TestContext.Current.CancellationToken);

        auth.UserId.Should().NotBeEmpty();
        auth.Email.Should().Be("citizen@example.com");
        auth.Token.Should().NotBeNullOrWhiteSpace();
        auth.ExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        var request = new RegisterRequest("duplicate@example.com", "Password123!");

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            request,
            TestContext.Current.CancellationToken);
        var secondResponse = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            request,
            TestContext.Current.CancellationToken);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public static TheoryData<RegisterRequest> InvalidRegisterRequests => new()
    {
        new RegisterRequest("", "Password123!"),
        new RegisterRequest("register-missing-password@example.com", ""),
        new RegisterRequest("register-short-password@example.com", "short")
    };

    [Theory]
    [MemberData(nameof(InvalidRegisterRequests))]
    public async Task Register_WithInvalidInput_ShouldReturnBadRequest(RegisterRequest request)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        await ApiTestClient.RegisterAndLoginAsync(
            _client,
            "login@example.com",
            "Password123!",
            TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new LoginRequest("login@example.com", "Password123!"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await ApiTestClient.ReadDataAsync<AuthResponse>(
            response,
            TestContext.Current.CancellationToken);

        auth.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnBadRequest()
    {
        await ApiTestClient.RegisterAndLoginAsync(
            _client,
            "invalid-login@example.com",
            "Password123!",
            TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new LoginRequest("invalid-login@example.com", "WrongPassword123!"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public static TheoryData<LoginRequest> InvalidLoginRequests => new()
    {
        new LoginRequest("", "Password123!"),
        new LoginRequest("login-missing-password@example.com", ""),
        new LoginRequest("login-short-password@example.com", "short")
    };

    [Theory]
    [MemberData(nameof(InvalidLoginRequests))]
    public async Task Login_WithInvalidInput_ShouldReturnBadRequest(LoginRequest request)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
