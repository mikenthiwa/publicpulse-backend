using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Web.Features.Locations;
using Web.Infrastructure;

namespace Web.UnitTests.Infrastructure;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WithProviderFailure_ShouldLogWarning()
    {
        var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        var handler = new GlobalExceptionHandler(logger);
        var httpContext = CreateHttpContext();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new ReverseGeocodingProviderException("Provider failed."),
            CancellationToken.None);

        handled.Should().BeTrue();
        GetLogLevels(logger).Should().Contain(LogLevel.Warning);
        GetLogLevels(logger).Should().NotContain(LogLevel.Error);
    }

    [Fact]
    public async Task TryHandleAsync_WithProviderConfigurationFailure_ShouldLogError()
    {
        var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        var handler = new GlobalExceptionHandler(logger);
        var httpContext = CreateHttpContext();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new ProviderConfigurationException("Provider configuration is missing."),
            CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        GetLogLevels(logger).Should().Contain(LogLevel.Error);
        GetLogLevels(logger).Should().NotContain(LogLevel.Warning);
    }

    [Fact]
    public async Task TryHandleAsync_WithUnexpectedFailure_ShouldLogError()
    {
        var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        var handler = new GlobalExceptionHandler(logger);
        var httpContext = CreateHttpContext();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new Exception("Unexpected."),
            CancellationToken.None);

        handled.Should().BeTrue();
        GetLogLevels(logger).Should().Contain(LogLevel.Error);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/test";
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static LogLevel[] GetLogLevels(ILogger<GlobalExceptionHandler> logger)
    {
        return logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => (LogLevel)call.GetArguments()[0]!)
            .ToArray();
    }
}
