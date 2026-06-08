using FluentAssertions;
using Web.Common.Models;

namespace Web.UnitTests.Infrastructure;

public sealed class ApplicationResultTests
{
    [Fact]
    public void Success_ShouldExposeValue()
    {
        var value = new TestData("value");

        var result = ApplicationResult<TestData>.Success(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(value);
        var readError = () => result.Error;
        readError.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(ApplicationErrorKind.BadRequest, "Bad request.")]
    [InlineData(ApplicationErrorKind.NotFound, "Not found.")]
    [InlineData(ApplicationErrorKind.Forbidden, "Forbidden.")]
    public void Failure_ShouldExposeError(ApplicationErrorKind kind, string message)
    {
        var result = kind switch
        {
            ApplicationErrorKind.BadRequest => ApplicationResult<TestData>.BadRequest(message),
            ApplicationErrorKind.NotFound => ApplicationResult<TestData>.NotFound(message),
            ApplicationErrorKind.Forbidden => ApplicationResult<TestData>.Forbidden(message),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(new ApplicationError(kind, message));
        var readValue = () => result.Value;
        readValue.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failure_ShouldAcceptApplicationError()
    {
        var error = new ApplicationError(ApplicationErrorKind.NotFound, "Missing.");

        var result = ApplicationResult<TestData>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeSameAs(error);
    }

    [Fact]
    public void Success_WithNullValue_ShouldThrow()
    {
        var action = () => ApplicationResult<TestData>.Success(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Failure_WithEmptyMessage_ShouldThrow()
    {
        var action = () => ApplicationResult<TestData>.BadRequest(string.Empty);

        action.Should().Throw<ArgumentException>();
    }

    private sealed record TestData(string Value);
}
