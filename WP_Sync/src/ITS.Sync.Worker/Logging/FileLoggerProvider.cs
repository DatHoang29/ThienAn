using System.Text;

namespace ITS.Sync.Worker.Logging;

/// <summary>
/// Logger ghi ra FILE theo ngày. Khi chạy như Windows Service sẽ KHÔNG có console,
/// nên cần file log để theo dõi (kết nối NATS, kết quả đồng bộ, lỗi...).
/// Cố tình viết đơn giản, không thêm thư viện ngoài.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly LogLevel _minLevel;
    private readonly int _retainDays;
    private readonly object _gate = new();

    public FileLoggerProvider(string directory, LogLevel minLevel = LogLevel.Information, int retainDays = 14)
    {
        _directory = directory;
        _minLevel = minLevel;
        _retainDays = retainDays;

        Directory.CreateDirectory(_directory);
        CleanupOldFiles();
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    /// <summary>Ghi một dòng log, khóa để nhiều luồng không tranh nhau file.</summary>
    internal void Write(string line)
    {
        var path = Path.Combine(_directory, $"sync-{DateTime.Now:yyyy-MM-dd}.log");
        lock (_gate)
        {
            try
            {
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Không để lỗi ghi log làm sập service.
            }
        }
    }

    internal bool IsEnabled(LogLevel level) => level >= _minLevel && level != LogLevel.None;

    /// <summary>Xóa file log cũ hơn số ngày cấu hình để không phình đĩa.</summary>
    private void CleanupOldFiles()
    {
        try
        {
            var limit = DateTime.Now.AddDays(-_retainDays);
            foreach (var file in Directory.EnumerateFiles(_directory, "sync-*.log"))
            {
                if (File.GetLastWriteTime(file) < limit) File.Delete(file);
            }
        }
        catch
        {
            // Bỏ qua - dọn log không được thì cũng không ảnh hưởng chạy.
        }
    }

    public void Dispose() { }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        private readonly string _shortCategory = category.Split('.').LastOrDefault() ?? category;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{Short(logLevel)}] {_shortCategory}: {message}";
            if (exception != null) line += Environment.NewLine + exception;

            provider.Write(line);
        }

        private static string Short(LogLevel level) => level switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };
    }
}
