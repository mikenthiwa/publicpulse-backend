using Web.Contracts;
using Web.Features.Auth;
using Web.Infrastructure;

namespace Web.Endpoints;

public sealed class Auth : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(Register, "/register")
            .MapPost(Login, "/login");
        
        // var group = app.MapGroup(this);
        //
        // group.MapPost("/register", Register)
        //     .WithName(nameof(Register));
        //
        // group.MapPost("/login", Login)
        //     .WithName(nameof(Login));
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, cancellationToken);

        return Results.Ok(ApiResponse<AuthResponse>.Ok(response, "Registration completed successfully."));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);

        return Results.Ok(ApiResponse<AuthResponse>.Ok(response, "Login completed successfully."));
    }
}
