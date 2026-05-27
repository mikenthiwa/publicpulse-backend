using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        {typeof(ValidationException), HandleValidationException}
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
        return httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(ex.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        });
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

    private static Task HandleBadGatewayException(HttpContext httpContext, Exception exception)
    {
        return WriteProblemDetails(
            httpContext,
            StatusCodes.Status502BadGateway,
            "Image upload failed.",
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
            cancellationToken: CancellationToken.None);
    }
}
