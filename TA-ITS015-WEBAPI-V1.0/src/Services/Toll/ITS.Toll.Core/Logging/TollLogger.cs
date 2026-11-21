using System.Globalization;
using System.Text;

namespace ITS.Toll.Core.Logging;

/// <summary>
/// Phân loại log để ghi vào file riêng, tránh gom mọi thứ vào một file.
/// Author: ChuongLP
/// Created date: 26/08/2026
/// </summary>
public enum LogCategory
{
    /// <summary>Luồng đồng bộ: gọi API, nhận dữ liệu, lưu DB (ghi vào sync.log).</summary>
    Sync,

    /// <summary>Thao tác cấu hình: nạp/lưu cấu hình, thêm/xóa/sửa API (ghi vào config.log).</summary>
    Config,

    /// <summary>Luồng NHẬN API do TCS gọi tới (push/import + HTTP host) — ghi vào file riêng callback.log.</summary>
    CallBack,
}

/// <summary>
/// Một dòng log để tầng hiển thị (form) vẽ lên view.
/// </summary>
public sealed class TollLogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Level { get; init; } = "Info";
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Bộ ghi log DÙNG CHUNG cho toàn hệ Toll — NẰM Ở CORE (không phụ thuộc UI).
/// Cấu trúc thư mục: {directory}/yyyy-MM-dd/{sync|config|callback|error}.log
///   - sync.log     : toàn bộ luồng đồng bộ (gọi API → nhận dữ liệu → lưu DB).
///   - config.log   : thao tác cấu hình (nạp/lưu file, thêm/xóa API).
///   - callback.log : luồng NHẬN API do TCS gọi tới (push /record, import /import, HTTP host).
///   - error.log  : gom TẤT CẢ dòng Level = "Error" (dù thuộc luồng nào) để tra cứu lỗi tập trung.
/// Thread-safe; tự dọn các thư mục ngày cũ hơn số ngày giữ (_retainDays).
/// Author: ChuongLP
/// Created date: 24/08/2026 (chuyển sang Core + tách loại/thư mục ngày: 26/08/2026)
/// </summary>
public sealed class TollLogger
{
    private readonly string _directory;     // Thư mục log gốc (tuyệt đối)
    private readonly int _retainDays;       // Số ngày giữ log, thư mục ngày cũ hơn sẽ bị xóa
    private readonly object _gate = new();   // Khóa ghi file (thread-safe)

    /// <summary>Phát mỗi khi có một dòng log mới, để form hiển thị lên view (có thể chạy trên thread nền).</summary>
    public event Action<TollLogEntry>? EntryLogged;

    public TollLogger(string directory, int retainDays = 14)
    {
        _directory = string.IsNullOrWhiteSpace(directory) ? Path.Combine(AppContext.BaseDirectory, "Log")
            : Path.IsPathRooted(directory) ? directory : Path.Combine(AppContext.BaseDirectory, directory);
        _retainDays = retainDays;

        try
        {
            Directory.CreateDirectory(_directory);
            CleanupOldFolders();
        }
        catch
        {
            // Không để lỗi khởi tạo log làm sập app.
        }
    }

    /// <summary>Ghi log mức Info.</summary>
    public void Info(LogCategory category, string message) => Log(category, "Info", message);

    /// <summary>Ghi log mức Success.</summary>
    public void Success(LogCategory category, string message) => Log(category, "Success", message);

    /// <summary>Ghi log mức Warning.</summary>
    public void Warning(LogCategory category, string message) => Log(category, "Warning", message);

    /// <summary>Ghi log mức Error (đồng thời gom vào error.log).</summary>
    public void Error(LogCategory category, string message) => Log(category, "Error", message);

    /// <summary>
    /// Ghi một dòng log: vừa GHI FILE (theo loại + ngày), vừa phát sự kiện cho form hiển thị.
    /// </summary>
    public void Log(LogCategory category, string level, string message)
    {
        var now = DateTime.Now;
        WriteFile(now, category, level, message);
        EntryLogged?.Invoke(new TollLogEntry { Timestamp = now, Level = level, Message = message });
    }

    /// <summary>Ghi dòng log ra file theo loại + theo ngày. Lỗi được gom thêm vào error.log cùng ngày.</summary>
    private void WriteFile(DateTime now, LogCategory category, string level, string message)
    {
        var line = $"[{now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        var folder = Path.Combine(_directory, now.ToString("yyyy-MM-dd"));
        var fileName = category switch
        {
            LogCategory.Config => "config.log",
            LogCategory.CallBack => "callback.log",
            _ => "sync.log",
        };

        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, fileName), line + Environment.NewLine, Encoding.UTF8);

                // Lỗi luôn được gom thêm vào error.log để tra cứu tập trung.
                if (string.Equals(level, "Error", StringComparison.OrdinalIgnoreCase))
                    File.AppendAllText(Path.Combine(folder, "error.log"), line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Bỏ qua lỗi ghi log.
            }
        }
    }

    /// <summary>Dọn các thư mục ngày (yyyy-MM-dd) cũ hơn số ngày giữ (_retainDays).</summary>
    private void CleanupOldFolders()
    {
        var limit = DateTime.Now.Date.AddDays(-_retainDays);
        foreach (var dir in Directory.EnumerateDirectories(_directory))
        {
            var name = Path.GetFileName(dir);
            if (DateTime.TryParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                && day < limit)
            {
                try { Directory.Delete(dir, recursive: true); } catch { /* bỏ qua */ }
            }
        }
    }
}
