using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Web.Features.Locations;
using Web.Features.Reports;
using ValidationException = Web.Common.Exceptions.ValidationException;

namespace Web.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers = new()
    {
        { typeof(KeyNotFoundException), HandleNotFoundException },
        { typeof(ArgumentException), HandleBadRequestException },
        { typeof(InvalidOperationException), HandleBadRequestException },
        { typeof(UnauthorizedAccessException), HandleForbiddenException },
        { typeof(ReportImageUploadException), HandleBadGatewayException },
        { typeof(ReverseGeocodingProviderException), HandleBadGatewayException },
        { typeof(ValidationException), HandleValidationException }
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

    private static Task HandleValidationException(HttpContext httpContext, Exception exception)
    {
        var ex = (ValidationException)exception;
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        var problemDetails = new ValidationProblemDetails(ex.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            (System.Text.Json.JsonSerializerOptions?)null,
            "application/problem+json",
            CancellationToken.None);
    }

    private static Task HandleNotFoundException(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource not found.",
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            (System.Text.Json.JsonSerializerOptions?)null,
            "application/problem+json",
            CancellationToken.None);
    }

    private static Task HandleBadRequestException(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad request.",
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            (System.Text.Json.JsonSerializerOptions?)null,
            "application/problem+json",
            CancellationToken.None);
    }

    private static Task HandleForbiddenException(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden.",
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            (System.Text.Json.JsonSerializerOptions?)null,
            "application/problem+json",
            CancellationToken.None);
    }

    private static Task HandleBadGatewayException(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status502BadGateway,
            Title = "Upstream provider failed.",
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.3"
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            (System.Text.Json.JsonSerializerOptions?)null,
            "application/problem+json",
            CancellationToken.None);
    }

    private Task HandleUnhandledException(HttpContext httpContext, Exception exception)
    {
        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The server encountered an unexpected error.",
            Instance = httpContext.Request.Path,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            (System.Text.Json.JsonSerializerOptions?)null,
            "application/problem+json",
            CancellationToken.None);
    }
}
