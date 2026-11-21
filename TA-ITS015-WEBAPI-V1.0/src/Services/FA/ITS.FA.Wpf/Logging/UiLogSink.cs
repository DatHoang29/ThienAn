using Microsoft.Extensions.Logging;

namespace ITS.FA.Wpf.Logging;

/// <summary>Đẩy log của dịch vụ FA lên form. Author: ChuongLP. Created date: 02/11/2026.</summary>
public sealed class UiLogSink : ILoggerProvider
{
    /// <summary>Phát từ luồng nền; form tự chuyển về luồng giao diện.</summary>
    public event Action<LogLevel, string>? Logged;

    public ILogger CreateLogger(string categoryName) => new SinkLogger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class SinkLogger(UiLogSink sink, string category) : ILogger
    {
        private readonly string _shortCategory = category[(category.LastIndexOf('.') + 1)..];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= LogLevel.Information && category.StartsWith("ITS.", StringComparison.Ordinal);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var text = formatter(state, exception);
            if (exception is not null) text += $" | {exception.GetType().Name}: {exception.Message}";
            sink.Logged?.Invoke(logLevel, $"[{_shortCategory}] {text}");
        }
    }
}
