using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Web.Common.Models;

namespace Web.Common.Mappings;

public static class ApplicationResultHttpExtensions
{
    public static ProblemHttpResult ToProblemHttpResult<T>(
        this ApplicationResult<T> result,
        HttpContext httpContext)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be mapped to a problem response.");
        }

        var (status, title, type) = result.Error.Kind switch
        {
            ApplicationErrorKind.BadRequest => (
                StatusCodes.Status400BadRequest,
                "Bad request.",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"),
            ApplicationErrorKind.NotFound => (
                StatusCodes.Status404NotFound,
                "Resource not found.",
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"),
            ApplicationErrorKind.Forbidden => (
                StatusCodes.Status403Forbidden,
                "Forbidden.",
                "https://tools.ietf.org/html/rfc7231#section-6.5.3"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(result),
                result.Error.Kind,
                "Unsupported application error kind.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = result.Error.Message,
            Instance = httpContext.Request.Path,
            Type = type
        };

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return TypedResults.Problem(problemDetails);
    }
}
