using System.Diagnostics;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Results;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Web.Common.Factory;

public class CustomResultFactory : IFluentValidationAutoValidationResultFactory
{
    public IResult CreateResult(EndpointFilterInvocationContext context, ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(error => error.PropertyName, error => error.ErrorMessage)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        return TypedResults.ValidationProblem(
            errors,
            detail: "One or more validation failures have occurred.",
            instance: context.HttpContext.Request.Path,
            title: "One or more validation errors occurred.",
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = traceId
            });
    }
}
