using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Web.Tests.Logging;

public sealed record LogEntry(LogLevel Level, EventId EventId, Exception? Exception,
    Dictionary<string, object?> Properties, Dictionary<string, object?> Scope);

public sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
    public ConcurrentQueue<LogEntry> Entries { get; } = new();
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(this);
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;
    public void Dispose() { }

    private sealed class CaptureLogger(CapturingLoggerProvider provider) : ILogger
    {
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => provider._scopes.Push(state);
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var scopes = new Dictionary<string, object?>();
            provider._scopes.ForEachScope((scope, values) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> properties)
                    foreach (var property in properties) values[property.Key] = property.Value;
            }, scopes);
            var fields = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value) : new Dictionary<string, object?>();
            provider.Entries.Enqueue(new LogEntry(level, eventId, exception, fields, scopes));
        }
    }
}
