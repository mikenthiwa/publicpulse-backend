using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Web.Common.Models;
using Web.Features.Auth;
using Web.Features.Auth.Login;
using Web.Features.Auth.Register;
using Web.Infrastructure;

namespace Web.Endpoints;

public sealed class Auth : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .AddFluentValidationAutoValidation()
            .MapPost(Register, "/register")
            .MapPost(Login, "/login");
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        RegisterHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        return Results.Ok(ApiResponse<AuthResponse>.Ok(response, "Registration completed successfully."));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        LoginHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        return Results.Ok(ApiResponse<AuthResponse>.Ok(response, "Login completed successfully."));
    }
}
