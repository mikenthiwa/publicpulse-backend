using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Web.Infrastructure;
using Web.Tests.Logging;

namespace Web.UnitTests.Infrastructure;

public sealed class RequestLoggingTests
{
    [Theory]
    [InlineData(200, LogLevel.Information)]
    [InlineData(201, LogLevel.Information)]
    [InlineData(400, LogLevel.Warning)]
    [InlineData(401, LogLevel.Warning)]
    [InlineData(404, LogLevel.Warning)]
    [InlineData(500, LogLevel.Error)]
    public async Task Completion_RecordsOnlySafeFields(int status, LogLevel level)
    {
        using var provider = new CapturingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/Reports";
        context.Request.QueryString = new QueryString("?secret=private");
        context.Request.Headers.Authorization = "Bearer private";
        var middleware = new RequestLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = status;
            return Task.CompletedTask;
        }, factory.CreateLogger<RequestLoggingMiddleware>());
        await middleware.InvokeAsync(context);
        var entry = Assert.Single(provider.Entries);
        Assert.Equal(level, entry.Level);
        Assert.Equal("RequestCompleted", entry.EventId.Name);
        Assert.Equal(status, entry.Properties["StatusCode"]);
        Assert.Equal("/api/v1/Reports", entry.Properties["Path"]);
        Assert.True((double)entry.Properties["ElapsedMilliseconds"]! >= 0);
        Assert.Null(entry.Exception);
        Assert.DoesNotContain("private", string.Join(" ", entry.Properties.Values));
        Assert.Equal(context.TraceIdentifier, entry.Scope["RequestId"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cancellation_IsNotSuccessfulCompletion(bool throws)
    {
        using var provider = new CapturingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var context = new DefaultHttpContext { RequestAborted = new CancellationToken(true) };
        var middleware = new RequestLoggingMiddleware(_ => throws
            ? Task.FromException(new OperationCanceledException()) : Task.CompletedTask,
            factory.CreateLogger<RequestLoggingMiddleware>());
        if (throws) await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
        else await middleware.InvokeAsync(context);
        var entry = Assert.Single(provider.Entries);
        Assert.Equal("RequestCancelled", entry.EventId.Name);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task EscapingException_IsRethrownWithoutStackInSummary()
    {
        using var provider = new CapturingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var failure = new InvalidOperationException("failure");
        var middleware = new RequestLoggingMiddleware(_ => Task.FromException(failure),
            factory.CreateLogger<RequestLoggingMiddleware>());
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(new DefaultHttpContext())));
        var entry = Assert.Single(provider.Entries);
        Assert.Equal("RequestFailed", entry.EventId.Name);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public async Task ConcurrentRequests_KeepScopesIsolated()
    {
        using var provider = new CapturingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        var middleware = new RequestLoggingMiddleware(async _ =>
        {
            if (Interlocked.Increment(ref count) == 2) arrived.SetResult();
            await arrived.Task;
        }, factory.CreateLogger<RequestLoggingMiddleware>());
        async Task Run(string id)
        {
            using var activity = new Activity(id).Start();
            await middleware.InvokeAsync(new DefaultHttpContext { TraceIdentifier = id });
        }
        await Task.WhenAll(Run("one"), Run("two"));
        Assert.Equal(2, provider.Entries.Select(e => e.Scope["RequestId"]).Distinct().Count());
        Assert.Equal(2, provider.Entries.Select(e => e.Scope["TraceId"]).Distinct().Count());
    }
}
