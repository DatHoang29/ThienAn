using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Services.Shared.Logging;

/// <summary>
/// Author: TAC Team
/// Description: Facade logging tĩnh dùng ZLogger (zero-allocation). Cấu hình pipeline (ConfigureLogging):
/// Console chung cho mọi kind + 1 <see cref="KindFileLogProcessor"/> mở N file riêng (mỗi <see cref="LogKind"/>
/// một file) theo PathTemplate cấu hình trong Configuration/Logging.json (section "Logging",
/// bind <see cref="LoggingOptions"/>). Cung cấp các hàm tiện lợi gọi được ở bất cứ đâu không cần inject
/// ILogger — tương tự LogHelper.WriteLog / LogHelper.LogErrorException bên DeviceMonitor.
///
/// Thiết kế API: trước đây có 6 hàm tổ hợp (Debug/Info/Warn/Error × 2 overload) × 5 kind, nhưng kind không
/// tạo routing khác nhau (chỉ là label). Hai thứ thực sự tạo khác biệt là <see cref="LogLevel"/> và
/// Exception có/không. Nên giờ chỉ còn 1 hàm <see cref="Log"/> tổng quát + 2 shortcut <see cref="Info"/>/
/// <see cref="Error(Exception, string?, string, string?)"/> cho 2 pattern chiếm đa số call site.
///
/// Thư mục + tên file log cấu hình qua <see cref="LoggingOptions"/> (PathTemplate với placeholder
/// ${shortdate}, ${basedir}, ${kind}). Mặc định logs/${shortdate}/${kind}.log.
/// Created date: 17/07/2026
/// </summary>
public static class LogManager
{
    private static readonly ConcurrentDictionary<string, ILogger> _loggers = new(StringComparer.Ordinal);
    private static ILoggerFactory? _factory;

    /// <summary>
    /// Cấu hình logging chuẩn: clear providers + minimum Information + ZLogger Console + file processor
    /// route theo <see cref="LogKind"/>. Gọi trong composition root trên ILoggingBuilder (trước Build).
    /// </summary>
    /// <param name="builder">ILoggingBuilder từ HostApplicationBuilder.Logging.</param>
    /// <param name="configuration">IConfiguration chứa section "Logging" (bind <see cref="LoggingOptions"/>).
    /// Nếu null hoặc section thiếu, dùng <see cref="LoggingOptions"/> mặc định.</param>
    public static void ConfigureLogging(ILoggingBuilder builder, IConfiguration? configuration = null)
    {
        var loggingOptions = configuration?.GetSection("Logging").Get<LoggingOptions>() ?? new LoggingOptions();

        builder.ClearProviders();
        builder.SetMinimumLevel(LogLevel.Information);
        // Microsoft framework logs (HTTP, hosting) chỉ Warn+ để giảm noise.
        builder.AddFilter("Microsoft", LogLevel.Warning);
        builder.AddFilter("System", LogLevel.Warning);

        // Console chung (format cùng kiểu với file processor để dễ đối chiếu).
        builder.AddZLoggerConsole(options =>
        {
            options.UsePlainTextFormatter(formatter =>
            {
                formatter.SetPrefixFormatter($"{0:local-longdate}|{1}|{2}|",
                    (in ZLogger.MessageTemplate template, in ZLogger.LogInfo info) =>
                        template.Format(info.Timestamp, info.LogLevel, info.Category.Name));
                formatter.SetSuffixFormatter($" [{0}]",
                    (in ZLogger.MessageTemplate template, in ZLogger.LogInfo info) =>
                        template.Format(string.IsNullOrWhiteSpace(info.MemberName) ? "-" : info.MemberName));
            });
        });

        // Processor route mỗi kind ra file riêng theo PathTemplate (config-driven) + rolling + cleanup.
        // Duplicate Error: mọi log level >= Error cũng ghi vào file của kind Error.
        var processor = new KindFileLogProcessor(loggingOptions, LogKind.Main, enableErrorDuplication: true);
        builder.AddZLoggerLogProcessor(processor);
    }

    /// <summary>
    /// Khởi tạo backing factory cho các hàm tiện lợi static. Gọi 1 LẦN sau khi build host
    /// (giống ConfigManager.SetInstance) — truyền ILoggerFactory resolve từ DI.
    /// </summary>
    public static void SetFactory(ILoggerFactory factory)
    {
        if (factory is null)
            throw new ArgumentNullException(nameof(factory));

        _factory = factory;
        _loggers.Clear(); // factory mới -> tạo lại logger theo kind.
    }

    /// <summary>Đã khởi tạo backing factory (SetFactory đã được gọi)?</summary>
    public static bool IsInitialized => _factory is not null;

    // ════════════════════════════════════════════════════════════════════════
    // CORE — 1 hàm tổng quát duy nhất. 2 thứ tạo khác biệt là LogLevel + Exception.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ghi log tổng quát. Đây là hàm duy nhất mọi route đều đi qua. Warning/Debug/Trace gọi trực tiếp
    /// hàm này (ví dụ <c>LogManager.Log(LogLevel.Warning, "...", kind: LogKind.Nats)</c>).
    /// </summary>
    /// <param name="level">Cấp độ log (Debug/Info/Warning/Error/...).</param>
    /// <param name="message">Nội dung log.</param>
    /// <param name="kind">Loại log (= category name, quyết định file đầu ra). Mặc định <see cref="LogKind.Main"/>.</param>
    /// <param name="exception">Exception kèm (nếu có) — sẽ render stack trace.</param>
    /// <param name="function">Tên hàm gọi — tự capture qua [CallerMemberName].</param>
    public static void Log(
        LogLevel level,
        string message,
        string kind = LogKind.Main,
        Exception? exception = null,
        [CallerMemberName] string? function = null)
    {
        var logger = GetLogger(kind);
        if (logger is null)
            return; // Chưa SetFactory — bỏ qua (tránh nổ khi log trước khi host ready).

        // MemberName trong ZLogger LogInfo chỉ set khi logger.ZLog*() được dùng; với logger.Log()
        // (string) thì không. Nên phải tự push function qua scope để processor suffix bắt được.
        using (function is null ? null : logger.BeginScope(new KeyValuePair<string, object?>("Function", function)))
            logger.Log(level, exception, message);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SHORTCUTS — chỉ 2 pattern chiếm ~90% call site. Mỗi hàm 1 dòng delegate tới Log().
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Shortcut ghi log Information (pattern phổ biến nhất).</summary>
    public static void Info(
        string message,
        string kind = LogKind.Main,
        [CallerMemberName] string? function = null)
        => Log(LogLevel.Information, message, kind, exception: null, function);

    /// <summary>
    /// Shortcut ghi log Error kèm exception (tương đương LogHelper.LogErrorException cũ).
    /// Mặc định <paramref name="kind"/> = <see cref="LogKind.Error"/> (route ra error-*.log).
    /// </summary>
    public static void Error(
        Exception exception,
        string? message = null,
        string kind = LogKind.Error,
        [CallerMemberName] string? function = null)
        => Log(LogLevel.Error, message ?? exception.Message, kind, exception, function);

    private static ILogger? GetLogger(string kind)
    {
        var factory = _factory;
        if (factory is null)
            return null;

        var key = string.IsNullOrWhiteSpace(kind) ? LogKind.Main : kind;
        return _loggers.GetOrAdd(key, factory.CreateLogger);
    }
}
