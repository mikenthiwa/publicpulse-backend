using SharpGrip.FluentValidation.AutoValidation.Endpoints.Results;
using ValidationException = Web.Common.Exceptions.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Web.Common.Factory;

public class CustomResultFactory : IFluentValidationAutoValidationResultFactory
{
    public IResult CreateResult(EndpointFilterInvocationContext context, ValidationResult validationResult)
    {
        throw new ValidationException(validationResult.Errors);
    }
}
