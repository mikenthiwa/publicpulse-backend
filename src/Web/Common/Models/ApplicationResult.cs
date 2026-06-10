namespace Web.Common.Models;

public enum ApplicationErrorKind
{
    BadRequest,
    NotFound,
    Forbidden
}

public sealed record ApplicationError(ApplicationErrorKind Kind, string Message);

public sealed class ApplicationResult<T>
{
    private readonly T? _value;
    private readonly ApplicationError? _error;

    private ApplicationResult(T value)
    {
        IsSuccess = true;
        _value = value;
    }

    private ApplicationResult(ApplicationError error)
    {
        IsSuccess = false;
        _error = error;
    }

    public bool IsSuccess { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result does not contain a value.");

    public ApplicationError Error => !IsSuccess
        ? _error!
        : throw new InvalidOperationException("A successful result does not contain an error.");

    public static ApplicationResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ApplicationResult<T>(value);
    }

    public static ApplicationResult<T> BadRequest(string message)
    {
        return Failure(new ApplicationError(ApplicationErrorKind.BadRequest, message));
    }

    public static ApplicationResult<T> NotFound(string message)
    {
        return Failure(new ApplicationError(ApplicationErrorKind.NotFound, message));
    }

    public static ApplicationResult<T> Forbidden(string message)
    {
        return Failure(new ApplicationError(ApplicationErrorKind.Forbidden, message));
    }

    public static ApplicationResult<T> Failure(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(error.Message);

        return new ApplicationResult<T>(error);
    }
}
