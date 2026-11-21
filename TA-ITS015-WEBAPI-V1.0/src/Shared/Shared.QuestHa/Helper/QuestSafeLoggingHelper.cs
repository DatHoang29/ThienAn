using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Shared.QuestHa.Helper;

/// <summary>
/// Author: KienLT
/// Description: Log vận hành không chứa thông điệp provider SQL; chi tiết chỉ ở Debug.
/// MỌI mức log — kể cả Debug — không được chứa chuỗi kết nối/mật khẩu (CODING_RULES §17).
/// Không bao giờ truyền object exception cho logger (logger sẽ in nguyên ToString() gồm message/inner); thay vào đó log
/// chuỗi đã làm sạch: loại + message đã che Password/Pwd/chuỗi kết nối, cho từng inner exception, kèm stack trace.
/// Created date: 29/10/2026
/// </summary>
internal static class QuestSafeLoggingHelper
{
    private const string Masked = "***";
    private const int MaxDepth = 8;

    // Khoá thường gặp trong chuỗi kết nối SQL Server / Npgsql. Hai cặp key=value trở lên liên tiếp ⇒ coi là chuỗi kết nối.
    private const string ConnectionKeys =
        @"server|host|data\s+source|addr|address|network\s+address|database|initial\s+catalog|user\s*id|uid|user\s*name|username|user|" +
        @"password|pwd|port|integrated\s+security|trusted_connection|encrypt|trustservercertificate|timeout|connect\s+timeout|" +
        @"connection\s+timeout|command\s+timeout|pooling|servercompatibilitymode|application\s+name|multipleactiveresultsets";

    private static readonly Regex ConnectionString = new(
        @"(?:\b(?:" + ConnectionKeys + @")\s*=\s*(?:""[^""]*""|'[^']*'|[^;\r\n]*)\s*(?:;|$)\s*){2,}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline, TimeSpan.FromMilliseconds(200));

    private static readonly Regex Secret = new(
        // giá trị không nháy che tới ';' hoặc hết dòng (mật khẩu có khoảng trắng không lộ phần sau).
        @"\b(password|pwd)\s*=\s*(?:""[^""]*""|'[^']*'|[^;\r\n]*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

    // không còn LogDebug(error, …) (in nguyên message/inner chứa chuỗi kết nối).
    /// <summary>Ghi cảnh báo chỉ loại/mã lỗi; chi tiết exception ĐÃ LÀM SẠCH chỉ ở Debug.</summary>
    public static void LogSafeWarning(this ILogger logger, Exception error, string message, params object?[] args)
    {
        logger.LogSafeDebug(error, message, args);
        logger.LogWarning(message + " Lỗi {ErrorType}.", args.Append((object?)QuestErrorClassifierHelper.SafeDescribe(error)).ToArray());
    }

    /// <summary>
    /// Ghi Debug kèm chi tiết exception đã làm sạch (<see cref="SafeExceptionText"/>) thay cho object exception.
    /// Không dựng chuỗi khi Debug tắt.
    /// </summary>
    public static void LogSafeDebug(this ILogger logger, Exception error, string message, params object?[] args)
    {
        if (!logger.IsEnabled(LogLevel.Debug)) return;
        logger.LogDebug(message + " Chi tiết: {SafeError}", args.Append((object?)SafeExceptionText(error)).ToArray());
    }

    /// <summary>
    /// Che giá trị Password/Pwd và thay mọi đoạn giống chuỗi kết nối (≥ 2 cặp key=value; liên tiếp) bằng
    /// <c>[connection string ***]</c>. null/rỗng trả rỗng. Hết thời gian regex ⇒ trả nhãn che toàn bộ (an toàn hơn để lộ).
    /// </summary>
    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        try
        {
            var result = ConnectionString.Replace(text, "[connection string " + Masked + "] ");
            return Secret.Replace(result, m => m.Groups[1].Value + "=" + Masked);
        }
        catch (RegexMatchTimeoutException)
        {
            return "[message " + Masked + "]";
        }
    }

    /// <summary>
    /// Mô tả exception an toàn để log: với từng exception trong chuỗi inner (tối đa 8 cấp, gồm inner của AggregateException)
    /// ghi <c>Loại: message đã che</c>; cuối cùng là stack trace (cũng qua <see cref="Redact"/>). Không đọc Exception.Data.
    /// </summary>
    public static string SafeExceptionText(Exception error)
    {
        var sb = new StringBuilder();
        Append(sb, error, 0);
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, Exception error, int depth)
    {
        if (depth >= MaxDepth) { sb.Append(" ---> …"); return; }
        if (depth > 0) sb.Append(" ---> ");
        sb.Append(error.GetType().FullName).Append(": ").Append(Redact(error.Message));
        if (error is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions.Take(4)) Append(sb, inner, depth + 1);
        }
        else if (error.InnerException is { } inner)
        {
            Append(sb, inner, depth + 1);
        }
        if (depth == 0 && error.StackTrace is { } stack) sb.Append(Environment.NewLine).Append(Redact(stack));
    }
}
