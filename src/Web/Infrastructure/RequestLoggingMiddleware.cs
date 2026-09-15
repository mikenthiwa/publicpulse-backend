using System.Diagnostics;

namespace Web.Infrastructure;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = context.TraceIdentifier,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
            ["ProblemDetailsTraceId"] = Activity.Current?.Id ?? context.TraceIdentifier
        });
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            LogCancellation();
            throw;
        }
        catch
        {
            logger.LogError(new EventId(1002, "RequestFailed"),
                "HTTP {Method} {Path} failed after {ElapsedMilliseconds} ms",
                context.Request.Method, context.Request.Path.Value, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }

        if (context.RequestAborted.IsCancellationRequested)
        {
            LogCancellation();
            return;
        }

        var status = context.Response.StatusCode;
        var level = status >= 500 ? LogLevel.Error : status >= 400 ? LogLevel.Warning : LogLevel.Information;
        logger.Log(level, new EventId(1000, "RequestCompleted"),
            "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds} ms",
            context.Request.Method, context.Request.Path.Value, status, Stopwatch.GetElapsedTime(started).TotalMilliseconds);

        void LogCancellation() => logger.LogInformation(new EventId(1001, "RequestCancelled"),
            "HTTP {Method} {Path} was cancelled after {ElapsedMilliseconds} ms",
            context.Request.Method, context.Request.Path.Value, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}
