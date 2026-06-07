using Microsoft.AspNetCore.Http.HttpResults;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Web.Common.Mappings;
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

    private static async Task<Results<Ok<ApiResponse<AuthResponse>>, ProblemHttpResult>> Register(
        RegisterRequest request,
        RegisterHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(ApiResponse<AuthResponse>.Ok(
                result.Value,
                "Registration completed successfully."))
            : result.ToProblemHttpResult(httpContext);
    }

    private static async Task<Results<Ok<ApiResponse<AuthResponse>>, ProblemHttpResult>> Login(
        LoginRequest request,
        LoginHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(ApiResponse<AuthResponse>.Ok(
                result.Value,
                "Login completed successfully."))
            : result.ToProblemHttpResult(httpContext);
    }
}
