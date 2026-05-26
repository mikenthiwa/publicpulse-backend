using FluentAssertions;
using Web.Common.Models;

namespace Web.UnitTests.Infrastructure;

public sealed class ApiResponseTests
{
    [Fact]
    public void Ok_ShouldReturnSuccessfulResponseWithDataAndDefaultMessage()
    {
        var data = new TestData("Road repair");

        var response = ApiResponse<TestData>.Ok(data);

        response.Success.Should().BeTrue();
        response.Message.Should().Be("Request completed successfully.");
        response.Data.Should().Be(data);
    }

    [Fact]
    public void Ok_ShouldReturnSuccessfulResponseWithCustomMessage()
    {
        var data = new TestData("Street light");

        var response = ApiResponse<TestData>.Ok(data, "Custom message.");

        response.Success.Should().BeTrue();
        response.Message.Should().Be("Custom message.");
        response.Data.Should().Be(data);
    }

    private sealed record TestData(string Name);
}
