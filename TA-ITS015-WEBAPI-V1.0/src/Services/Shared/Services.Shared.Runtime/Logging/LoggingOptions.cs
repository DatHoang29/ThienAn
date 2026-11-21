using Microsoft.Extensions.Configuration;

namespace Services.Shared.Logging;

/// <summary>
/// Author: TAC Team
/// Description: POCO binding cho section "Logging" trong Configuration/Logging.json. Cấu hình thư mục + tên
/// file + rotation cho từng <see cref="LogKind"/> qua path template (giống NLog fileName).
/// Bind qua <c>services.Configure&lt;LoggingOptions&gt;(configuration.GetSection("Logging"))</c> hoặc
/// <c>configuration.GetSection("Logging").Get&lt;LoggingOptions&gt;()</c>.
///
/// Schema JSON:
/// <code>
/// {
///   "Logging": {
///     "BaseDirectory": "${basedir}",            // gốc dùng cho ${basedir} trong PathTemplate
///     "Defaults": { ... LoggingKindDefaults ... },
///     "Kinds": {
///       "Nats": { "PathTemplate": "LogFile/${shortdate}/TACNatsLog.txt", "KeepFileCount": 60 }
///     }
///   }
/// }
/// </code>
/// Created date: 20/07/2026
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>
    /// Thư mục gốc thay thế cho placeholder <c>${basedir}</c> trong PathTemplate.
    /// Mặc định chuỗi literal <c>"${basedir}"</c> → processor sẽ resolve thành <c>AppContext.BaseDirectory</c>.
    /// Đặt giá trị cụ thể (vd. <c>"C:\\TAC\\Logs"</c>) để pin cứng gốc.
    /// </summary>
    public string BaseDirectory { get; set; } = "${basedir}";

    /// <summary>Mặc định áp dụng cho mọi kind không khai báo trong <see cref="Kinds"/>.</summary>
    public LoggingKindDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Cấu hình per-kind. Key = tên kind (phải khớp const trong <see cref="LogKind"/>, vd. "Nats", "Main").
    /// Kind không có ở đây dùng <see cref="Defaults"/>. Mỗi value kế thừa mặc định, chỉ cần override field muốn đổi.
    /// </summary>
    public Dictionary<string, LoggingKindOptions> Kinds { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Author: TAC Team
/// Description: Mặc định cấu hình file log cho 1 kind. Dùng làm fallback cho mọi kind thiếu override.
/// Created date: 20/07/2026
/// </summary>
public class LoggingKindDefaults
{
    /// <summary>
    /// Template path file log. Hỗ trợ placeholder:
    /// <list type="bullet">
    /// <item><c>${basedir}</c> → <see cref="LoggingOptions.BaseDirectory"/> (mặc định AppContext.BaseDirectory).</item>
    /// <item><c>${shortdate}</c> → <c>yyyy-MM-dd</c> của thời gian hiện tại (đổi mỗi ngày → tự roll).</item>
    /// <item><c>${kind}</c> → tên kind viết thường (main, nats, error...).</item>
    /// </list>
    /// Path relative nối vào BaseDirectory; path absolute (có ':' hoặc bắt đầu '/'/'\') dùng nguyên.
    /// </summary>
    public string PathTemplate { get; set; } = "logs/${shortdate}/${kind}.log";

    /// <summary>
    /// Ngưỡng kích thước file (KB). Khi writer hiện tại vượt ngưỡng, roll sang file mới với suffix sequence
    /// (vd. <c>TACNatsLog.txt</c> → <c>TACNatsLog.1.txt</c> → <c>TACNatsLog.2.txt</c>). 0 = không roll theo size.
    /// </summary>
    public int RollingSizeKB { get; set; } = 10240; // 10 MB

    /// <summary>
    /// Số file gần nhất giữ lại khi cleanup chạy (mỗi lần roll qua ngày mới). File cũ hơn bị xóa.
    /// 0 = không cleanup (file tích lũy mãi).
    /// </summary>
    public int KeepFileCount { get; set; } = 30;

    /// <summary>
    /// true: mở file với <c>FileShare.Read</c> → tool ngoài (tail, Notepad) đọc được khi service đang chạy.
    /// false: file khóa độc quyền khi ghi.
    /// </summary>
    public bool FileShareRead { get; set; } = true;
}

/// <summary>
/// Author: TAC Team
/// Description: Cấu hình cho 1 kind cụ thể. Kế thừa <see cref="LoggingKindDefaults"/>, chỉ cần override
/// field muốn đổi (thường chỉ <see cref="LoggingKindDefaults.PathTemplate"/>).
/// Created date: 20/07/2026
/// </summary>
public sealed class LoggingKindOptions : LoggingKindDefaults { }
