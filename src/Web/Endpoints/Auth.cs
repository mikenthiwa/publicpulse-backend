using Microsoft.AspNetCore.Http.HttpResults;
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
        var group = app.MapGroup(this)
            .AddFluentValidationAutoValidation();

        group.MapPost("/register", Register)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapPost("/login", Login)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<Ok<ApiResponse<AuthResponse>>> Register(
        RegisterRequest request,
        RegisterHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        return TypedResults.Ok(ApiResponse<AuthResponse>.Ok(response, "Registration completed successfully."));
    }

    private static async Task<Ok<ApiResponse<AuthResponse>>> Login(
        LoginRequest request,
        LoginHandler handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(request, cancellationToken);

        return TypedResults.Ok(ApiResponse<AuthResponse>.Ok(response, "Login completed successfully."));
    }
}
