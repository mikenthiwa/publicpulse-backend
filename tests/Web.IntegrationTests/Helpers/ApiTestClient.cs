using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Web.Features.Auth;

namespace Web.IntegrationTests.Helpers;

public static class ApiTestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<AuthResponse> RegisterAndLoginAsync(
        HttpClient client,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new RegisterRequest(email, password),
            cancellationToken);

        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new LoginRequest(email, password),
            cancellationToken);

        loginResponse.EnsureSuccessStatusCode();

        return await ReadDataAsync<AuthResponse>(loginResponse, cancellationToken);
    }

    public static void SetBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<T> ReadDataAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var apiResponse = await JsonSerializer.DeserializeAsync<ApiEnvelope<T>>(
            responseStream,
            JsonOptions,
            cancellationToken);

        return apiResponse is null
            ? throw new InvalidOperationException("API response could not be deserialized.")
            : apiResponse.Data;
    }

    private sealed record ApiEnvelope<T>(bool Success, string Message, T Data);
}
