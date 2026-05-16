using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Web.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers = new()
    {
        { typeof(KeyNotFoundException), HandleNotFoundException },
        { typeof(ArgumentException), HandleBadRequestException },
        { typeof(InvalidOperationException), HandleBadRequestException },
        { typeof(UnauthorizedAccessException), HandleForbiddenException },
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (!_exceptionHandlers.TryGetValue(exceptionType, out var handler))
        {
            await HandleUnhandledException(httpContext, exception);
            return true;
        }

        logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        await handler.Invoke(httpContext, exception);

        return true;
    }

    private static Task HandleNotFoundException(HttpContext httpContext, Exception exception)
    {
        return WriteProblemDetails(
            httpContext,
            StatusCodes.Status404NotFound,
            "Resource not found.",
            exception.Message);
    }

    private static Task HandleBadRequestException(HttpContext httpContext, Exception exception)
    {
        return WriteProblemDetails(
            httpContext,
            StatusCodes.Status400BadRequest,
            "Bad request.",
            exception.Message);
    }

    private static Task HandleForbiddenException(HttpContext httpContext, Exception exception)
    {
        return WriteProblemDetails(
            httpContext,
            StatusCodes.Status403Forbidden,
            "Forbidden.",
            exception.Message);
    }

    private Task HandleUnhandledException(HttpContext httpContext, Exception exception)
    {
        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        return WriteProblemDetails(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            "The server encountered an unexpected error.");
    }

    private static Task WriteProblemDetails(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail)
    {
        httpContext.Response.StatusCode = statusCode;
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: default);
    }
}
