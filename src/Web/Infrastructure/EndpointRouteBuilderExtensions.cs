namespace Web.Infrastructure;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGet(
        this IEndpointRouteBuilder builder,
        Delegate handler,
        string pattern = "")
    {
        builder
            .MapGet(pattern, handler)
            .WithName(handler.Method.Name);

        return builder;
    }

    public static IEndpointRouteBuilder MapPost(
        this IEndpointRouteBuilder builder,
        Delegate handler,
        string pattern = "")
    {
        builder
            .MapPost(pattern, handler)
            .WithName(handler.Method.Name);

        return builder;
    }
}
