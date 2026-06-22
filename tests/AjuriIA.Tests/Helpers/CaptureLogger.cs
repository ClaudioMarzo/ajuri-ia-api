using Microsoft.Extensions.Logging;

namespace AjuriIA.Tests.Helpers;

public class CaptureLogger<T> : ILogger<T>
{
    public record LogEntry(LogLevel Level, string Message, Exception? Exception = null);
    public List<LogEntry> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }
}
