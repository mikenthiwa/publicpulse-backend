using FluentValidation.Results;

namespace Web.Common.Exceptions;

public class ValidationException(IEnumerable<ValidationFailure> failures) : Exception("One or more validation failures have occurred.")
{
    public Dictionary<string, string[]> Errors { get; } = failures.GroupBy(e => e.PropertyName, e => e.ErrorMessage)
        .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
}
