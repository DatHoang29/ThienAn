using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Utf8StringInterpolation;
using ZLogger;
using ZLogger.Formatters;

namespace Services.Shared.Logging;

/// <summary>
/// Author: TAC Team
/// Description: Async log processor route mỗi log entry ra file riêng theo logger category (LogKind),
/// hỗ trợ path template + rolling (daily qua <c>${shortdate}</c> + size) + cleanup keep-N-files, cấu hình
/// qua <see cref="LoggingOptions"/> (bind từ Configuration/Logging.json section "Logging").
///
/// Vì <see cref="IZLoggerEntry"/> là pooled, ta KHÔNG Post() lại entry sang writer khác (gây use-after-return).
/// Duplicate Error: format entry 1 lần ra <see cref="ArrayBufferWriter{T}"/> độc lập, copy byte[] rồi đẩy
/// vào <see cref="_errorChannel"/> cho loop nền ghi vào file của kind Error.
///
/// Rolling: mỗi kind giữ 1 <see cref="KindWriter"/> (writer hiện tại + size tracking + sequence). Khi
/// <c>${shortdate}</c> đổi (qua ngày) hoặc size vượt <see cref="LoggingKindDefaults.RollingSizeKB"/> →
/// dispose writer cũ (flush queue) + mở writer mới với path/sequence mới.
///
/// Cleanup: chạy 1 lần mỗi khi roll qua ngày (không dùng timer). Quét thư mục prefix của kind, xóa file
/// cũ hơn <see cref="LoggingKindDefaults.KeepFileCount"/> file gần nhất.
/// Created date: 20/07/2026
/// </summary>
internal sealed class KindFileLogProcessor : IAsyncLogProcessor
{
    private readonly ZLoggerOptions _options;
    private readonly IZLoggerFormatter _formatter;
    private readonly LoggingOptions _loggingOptions;

    // Một KindWriter cho mỗi category. KindWriter có lock riêng để serialize roll.
    private readonly ConcurrentDictionary<string, KindWriter> _writers = new(StringComparer.Ordinal);
    private readonly string _fallbackKind;

    // === Duplicate Error (level >= Error → thêm vào file của kind Error) ===
    private readonly bool _enableErrorDuplication;
    private FileStream? _errorStream;
    private readonly Channel<byte[]> _errorChannel;
    private readonly Task _errorLoopTask;
    private readonly CancellationTokenSource _errorLoopCts;
    private string? _errorCurrentPath;        // path đang mở của error stream
    private long _errorCurrentSize;           // size đã ghi
    private int _errorSequence;               // sequence khi roll size
    private readonly SemaphoreSlim _errorLock = new(1, 1);

    private volatile bool _disposed;

    /// <param name="loggingOptions">Cấu hình từ Logging.json (đã bind). Nếu null dùng mặc định.</param>
    /// <param name="fallbackKind">Kind dùng khi entry có category không nằm trong <paramref name="loggingOptions"/>.</param>
    /// <param name="enableErrorDuplication">
    /// true (mặc định): mọi log level &gt;= Error được duplicate thêm ra file của kind Error ngoài file của kind gọi.
    /// </param>
    public KindFileLogProcessor(
        LoggingOptions? loggingOptions,
        string fallbackKind = LogKind.Main,
        bool enableErrorDuplication = true)
    {
        _loggingOptions = loggingOptions ?? new LoggingOptions();
        _fallbackKind = fallbackKind;
        _enableErrorDuplication = enableErrorDuplication;

        _options = new ZLoggerOptions();
        // Format: 2026-07-20 14:32:11.123|Warning|Nats|message [Function=RegisterInput]
        _options.UsePlainTextFormatter(formatter =>
        {
            formatter.SetPrefixFormatter($"{0:local-longdate}|{1}|{2}|",
                (in MessageTemplate template, in LogInfo info) =>
                    template.Format(info.Timestamp, info.LogLevel, info.Category.Name));
            formatter.SetSuffixFormatter($" [{0}]",
                (in MessageTemplate template, in LogInfo info) =>
                    template.Format(string.IsNullOrWhiteSpace(info.MemberName) ? "-" : info.MemberName));
        });
        _formatter = _options.CreateFormatter();

        // Error stream + background loop. Channel unbounded để Post() không bao giờ khoá caller.
        _errorChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _errorLoopCts = new CancellationTokenSource();

        if (_enableErrorDuplication)
        {
            // Mở error stream ngay theo template của kind Error (hoặc fallback).
            var errorPath = ResolvePath(LogKind.Error, DateTimeOffset.Now);
            _errorCurrentPath = errorPath;
            EnsureDirectory(errorPath);
            _errorStream = OpenFileStream(errorPath, GetKindOptions(LogKind.Error).FileShareRead);
            _errorSequence = 0;
            _errorCurrentSize = 0;
        }

        _errorLoopTask = Task.Run(() => WriteErrorLoopAsync(_errorLoopCts.Token));
    }

    /// <summary>
    /// Ghi entry ra file của kind tương ứng (theo category name). Fallback về fallbackKind.
    /// Nếu entry có level &gt;= Error, đồng thời format + ghi duplicate ra file của kind Error.
    /// </summary>
    public void Post(IZLoggerEntry log)
    {
        var category = log.LogInfo.Category.Name;
        var kind = _loggingOptions.Kinds.ContainsKey(category) || IsKnownKind(category)
            ? category
            : _fallbackKind;

        var writer = GetOrAddWriter(kind);

        // PHẢI format duplicate TRƯỚC khi Post() cho kind writer. Lý do: kind writer có background loop
        // riêng sẽ format entry rồi Return() về pool — nếu ta format sau Post() có thể trúng entry đã bị
        // thu hồi/ghi đè. Ở đây entry vẫn còn hợp lệ (Post() chỉ enqueue, chưa xử lý).
        // Format ra buffer độc lập để: (1) track size cho duplicate error, (2) copy byte cho error channel.
        if (_enableErrorDuplication && log.LogInfo.LogLevel >= LogLevel.Error)
        {
            var buffer = new ArrayBufferWriter<byte>(256);
            log.FormatUtf8(buffer, _formatter);
            _errorChannel.Writer.TryWrite(buffer.WrittenSpan.ToArray());
        }

        // Kind writer lo phần format + Return() entry về đúng pool của nó. Roll size check nội bộ
        // qua FileInfo.Length (không cần biết byte count trước).
        writer.Write(log);
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        _errorChannel.Writer.TryComplete();
        _errorLoopCts.Cancel();
        try { await _errorLoopTask.ConfigureAwait(false); }
        catch { /* best-effort flush khi shutdown */ }
        _errorLoopCts.Dispose();
        _errorLock.Dispose();

        if (_errorStream is not null)
        {
            try { await _errorStream.DisposeAsync().ConfigureAwait(false); }
            catch { /* best-effort */ }
        }

        foreach (var kv in _writers)
        {
            try { kv.Value.Dispose(); }
            catch { /* best-effort */ }
        }
        _writers.Clear();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Kind writer (1 cho mỗi category) — có rolling logic.
    // ════════════════════════════════════════════════════════════════════════

    private KindWriter GetOrAddWriter(string kind)
        => _writers.GetOrAdd(kind, k => new KindWriter(k, GetKindOptions(k), _options, _loggingOptions.BaseDirectory));

    private LoggingKindOptions GetKindOptions(string kind)
    {
        if (_loggingOptions.Kinds.TryGetValue(kind, out var opt))
            return opt;
        // Map từ Defaults sang LoggingKindOptions (để giữ type đồng nhất).
        return new LoggingKindOptions
        {
            PathTemplate = _loggingOptions.Defaults.PathTemplate,
            RollingSizeKB = _loggingOptions.Defaults.RollingSizeKB,
            KeepFileCount = _loggingOptions.Defaults.KeepFileCount,
            FileShareRead = _loggingOptions.Defaults.FileShareRead
        };
    }

    private string ResolvePath(string kind, DateTimeOffset timestamp)
    {
        var opts = GetKindOptions(kind);
        var template = LogPathResolver.ResolveTemplate(opts.PathTemplate, kind, timestamp);
        return LogPathResolver.CombineWithBase(template, _loggingOptions.BaseDirectory);
    }

    private static void EnsureDirectory(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    private static FileStream OpenFileStream(string path, bool fileShareRead)
    {
        var share = fileShareRead ? FileShare.Read : FileShare.None;
        return new FileStream(path, FileMode.Append, FileAccess.Write, share);
    }

    private bool IsKnownKind(string category)
        => LogKind.All.Contains(category) || string.Equals(category, _fallbackKind, StringComparison.Ordinal);

    // ════════════════════════════════════════════════════════════════════════
    // Error stream loop (background channel consumer).
    // ════════════════════════════════════════════════════════════════════════

    private async Task WriteErrorLoopAsync(CancellationToken cancellationToken)
    {
        if (_errorStream is null)
        {
            // Drain channel (nếu có) để không leak memory, nhưng không ghi gì.
            await foreach (var _ in _errorChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) { }
            return;
        }

        var reader = _errorChannel.Reader;
        var stream = _errorStream;
        try
        {
            await foreach (var bytes in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await _errorLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        // Check roll size cho error stream.
                        var maxBytes = (long)GetKindOptions(LogKind.Error).RollingSizeKB * 1024;
                        if (maxBytes > 0 && _errorCurrentSize + bytes.Length > maxBytes && _errorCurrentSize > 0)
                        {
                            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                            await stream.DisposeAsync().ConfigureAwait(false);
                            _errorSequence++;
                            var newPath = LogPathResolver.AppendSequence(_errorCurrentPath!, _errorSequence);
                            EnsureDirectory(newPath);
                            stream = OpenFileStream(newPath, GetKindOptions(LogKind.Error).FileShareRead);
                            _errorStream = stream; // cập nhật reference (cho dispose)
                            _errorCurrentSize = 0;
                            // Khi roll size → trigger cleanup error file cũ.
                            CleanupOldFiles(LogKind.Error);
                        }

                        // Check roll ngày cho error stream.
                        var todayPath = ResolvePath(LogKind.Error, DateTimeOffset.Now);
                        if (!string.Equals(todayPath, PathWithoutSequence(_errorCurrentPath!), StringComparison.OrdinalIgnoreCase))
                        {
                            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                            await stream.DisposeAsync().ConfigureAwait(false);
                            _errorSequence = 0;
                            EnsureDirectory(todayPath);
                            stream = OpenFileStream(todayPath, GetKindOptions(LogKind.Error).FileShareRead);
                            _errorStream = stream;
                            _errorCurrentPath = todayPath;
                            _errorCurrentSize = 0;
                            CleanupOldFiles(LogKind.Error);
                        }

                        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                        _errorCurrentSize += bytes.Length;
                    }
                    finally { _errorLock.Release(); }
                }
                catch { /* bỏ qua 1 entry hỏng, giữ loop sống */ }
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch { /* best-effort */ }
        finally
        {
            try { await stream.FlushAsync(CancellationToken.None).ConfigureAwait(false); }
            catch { /* best-effort */ }
        }
    }

    /// <summary>Bỏ suffix sequence (.N) khỏi path để so sánh path "gốc" khi check roll ngày.</summary>
    private static string PathWithoutSequence(string path)
    {
        var (dir, name, ext) = LogPathResolver.SplitPath(path);
        // Tìm pattern ".N" cuối cùng trong name (vd. "TACError.2" → "TACError").
        var lastDot = name.LastIndexOf('.');
        if (lastDot > 0 && int.TryParse(name.AsSpan(lastDot + 1), out _))
            name = name[..lastDot];
        var fileName = name + ext;
        return string.IsNullOrEmpty(dir) ? fileName : Path.Combine(dir, fileName);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Cleanup old files — giữ KeepFileCount file gần nhất.
    // ════════════════════════════════════════════════════════════════════════

    private void CleanupOldFiles(string kind)
    {
        var opts = GetKindOptions(kind);
        if (opts.KeepFileCount <= 0)
            return;

        try
        {
            // Parse path template để biết thư mục quét + pattern tên file.
            var samplePath = ResolvePath(kind, DateTimeOffset.Now);
            var (dir, name, ext) = LogPathResolver.SplitPath(samplePath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return;

            // Pattern: tất cả file có cùng prefix name + cùng extension (bao gồm cả suffix .N).
            // Vd. name="TACNatsLog", ext=".txt" → match TACNatsLog.txt, TACNatsLog.1.txt, TACNatsLog.2.txt...
            var prefix = name;
            var files = Directory.EnumerateFiles(dir, $"{prefix}*{ext}")
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            // Giữ KeepFileCount file gần nhất, xóa còn lại.
            foreach (var file in files.Skip(opts.KeepFileCount))
            {
                try { file.Delete(); }
                catch { /* file đang mở / lock — bỏ qua */ }
            }
        }
        catch { /* cleanup best-effort — không được crash service */ }
    }

    /// <summary>
    /// Holder cho 1 kind writer: writer hiện tại + size tracking + sequence + lock riêng cho roll.
    /// </summary>
    private sealed class KindWriter : IDisposable
    {
        private readonly LoggingKindOptions _options;
        private readonly ZLoggerOptions _zloggerOptions;
        private readonly string _kind;
        private readonly string _baseDirectory;

        private AsyncStreamLineMessageWriter _writer;
        private FileStream _stream;
        private string _currentPath;
        private long _currentSize;
        private int _sequence;
        private string _currentShortDate = string.Empty; // yyyy-MM-dd hiện tại
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _disposed;

        public KindWriter(string kind, LoggingKindOptions options, ZLoggerOptions zloggerOptions, string baseDirectory)
        {
            _kind = kind;
            _options = options;
            _zloggerOptions = zloggerOptions;
            _baseDirectory = baseDirectory;

            var now = DateTimeOffset.Now;
            _currentPath = ResolvePath(now);
            _currentShortDate = now.LocalDateTime.ToString("yyyy-MM-dd");
            EnsureDirectory(_currentPath);
            _stream = OpenFileStream(_currentPath, _options.FileShareRead);
            _writer = new AsyncStreamLineMessageWriter(_stream, _zloggerOptions);
            _currentSize = new FileInfo(_currentPath).Exists ? new FileInfo(_currentPath).Length : 0;
            _sequence = 0;
        }

        /// <summary>Ghi entry vào writer. Tự check roll (date/size) trước khi ghi.</summary>
        public void Write(IZLoggerEntry log)
        {
            if (_disposed) return;

            var timestamp = log.LogInfo.Timestamp.Local;

            // Check roll nhanh (không lock): ngày đổi HOẶC size vượt (đo qua FileInfo — chính xác).
            var todayShortDate = timestamp.ToString("yyyy-MM-dd");
            var needsRoll = !string.Equals(todayShortDate, _currentShortDate, StringComparison.Ordinal)
                            || (_options.RollingSizeKB > 0 && CurrentFileSize() >= (long)_options.RollingSizeKB * 1024);

            if (needsRoll)
            {
                _lock.Wait();
                try
                {
                    // Re-check trong lock (tránh nhiều thread cùng roll).
                    var todayShortDate2 = timestamp.ToString("yyyy-MM-dd");
                    var dateChanged = !string.Equals(todayShortDate2, _currentShortDate, StringComparison.Ordinal);
                    var sizeExceeded = _options.RollingSizeKB > 0
                                       && CurrentFileSize() >= (long)_options.RollingSizeKB * 1024;

                    if (dateChanged || sizeExceeded)
                        RollInternal(timestamp, dateChanged);
                }
                finally { _lock.Release(); }
            }

            _writer.Post(log);
        }

        /// <summary>Đọc kích thước file hiện tại (chính xác — flush xong). Dùng cho roll size check.</summary>
        private long CurrentFileSize()
        {
            try
            {
                _stream.Flush();
                return new FileInfo(_currentPath).Length;
            }
            catch { return _currentSize; /* fallback — best-effort */ }
        }

        private void RollInternal(DateTimeOffset timestamp, bool dateChanged)
        {
            // Dispose writer cũ: flush queue (đợi background loop drain). Post() sync nên block.
            try
            {
                _writer.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
            }
            catch { /* best-effort — force open mới */ }
            try { _stream.Flush(); _stream.Dispose(); }
            catch { /* best-effort */ }

            if (dateChanged)
            {
                _sequence = 0;
                _currentPath = ResolvePath(timestamp);
                _currentShortDate = timestamp.LocalDateTime.ToString("yyyy-MM-dd");
            }
            else
            {
                _sequence++;
                _currentPath = LogPathResolver.AppendSequence(ResolvePath(timestamp), _sequence);
            }

            EnsureDirectory(_currentPath);
            _stream = OpenFileStream(_currentPath, _options.FileShareRead);
            _writer = new AsyncStreamLineMessageWriter(_stream, _zloggerOptions);
            _currentSize = 0;
        }

        private string ResolvePath(DateTimeOffset timestamp)
        {
            var template = LogPathResolver.ResolveTemplate(_options.PathTemplate, _kind, timestamp);
            return LogPathResolver.CombineWithBase(template, _baseDirectory);
        }

        private static void EnsureDirectory(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        private static FileStream OpenFileStream(string path, bool fileShareRead)
        {
            var share = fileShareRead ? FileShare.Read : FileShare.None;
            return new FileStream(path, FileMode.Append, FileAccess.Write, share);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _writer.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5)); }
            catch { /* best-effort */ }
            try { _stream.Dispose(); }
            catch { /* best-effort */ }
            _lock.Dispose();
        }
    }
}
