using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ITS.Toll.Core.Configuration;
using ITS.Toll.Core.Logging;

namespace ITS.Toll.Core.Persistence;

/// <summary>
/// Lưu vết đồng bộ bằng FILE (không đụng DB):
///   - Con trỏ resume: 1 file JSON / DataType tại {StatePath}/cursor/toll-cursor-{DataType}.json (ghi đè, atomic).
///   - Nhật ký: append 1 dòng JSON / lượt chạy vào {StatePath}/history/toll-sync-{yyyy-MM-dd}.jsonl.
/// Nhờ con trỏ, cửa sổ lấy dữ liệu tự cuộn tới hiện tại (hướng B) và tự sang ngày mới; lỗi thì watermark
/// KHÔNG nhích nên lần sau lặp lại đúng khoảng — không miss, chạy lại an toàn.
/// Ghi atomic: ghi ra file .tmp rồi rename đè để không đọc phải file hỏng khi service chết giữa chừng.
/// Author: ChuongLP
/// Created date: 09/09/2026
/// </summary>
public sealed class TollSyncCursorStore
{
    private readonly TollServiceOptions _options;
    private readonly TollLogger _logger;
    private readonly string _cursorDir;
    private readonly string _historyDir;
    private readonly object _ioLock = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    private static readonly JsonSerializerOptions JsonLineOpts = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public TollSyncCursorStore(TollServiceOptions options, TollLogger logger)
    {
        _options = options;
        _logger = logger;

        var root = ResolveStateRoot(options.StatePath);
        _cursorDir = Path.Combine(root, "cursor");
        _historyDir = Path.Combine(root, "history");
        Directory.CreateDirectory(_cursorDir);
        Directory.CreateDirectory(_historyDir);
    }

    /// <summary>Đọc con trỏ theo DataType. Không có file / lỗi đọc → trả con trỏ rỗng (bắt đầu mới).</summary>
    public TollSyncCursor Load(string dataType)
    {
        var path = CursorPath(dataType);
        try
        {
            lock (_ioLock)
            {
                if (!File.Exists(path))
                    return new TollSyncCursor { DataType = dataType };

                var json = File.ReadAllText(path, Encoding.UTF8);
                var cursor = JsonSerializer.Deserialize<TollSyncCursor>(json, JsonOpts);
                if (cursor is null)
                    return new TollSyncCursor { DataType = dataType };

                cursor.DataType = dataType; // đảm bảo đúng khóa dù file bị sửa tay
                return cursor;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(LogCategory.Sync, $"Không đọc được con trỏ '{dataType}' ({ex.Message}) — bắt đầu mới.");
            return new TollSyncCursor { DataType = dataType };
        }
    }

    /// <summary>Ghi con trỏ (atomic: .tmp rồi rename đè).</summary>
    public void Save(TollSyncCursor cursor)
    {
        var path = CursorPath(cursor.DataType);
        var tmp = path + ".tmp";
        try
        {
            lock (_ioLock)
            {
                var json = JsonSerializer.Serialize(cursor, JsonOpts);
                File.WriteAllText(tmp, json, new UTF8Encoding(false));
                File.Move(tmp, path, overwrite: true);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(LogCategory.Sync, $"Không ghi được con trỏ '{cursor.DataType}': {ex.Message}");
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* bỏ qua */ }
        }
    }

    /// <summary>Append 1 dòng nhật ký vào file history theo ngày (JSONL).</summary>
    public void AppendHistory(TollSyncHistoryEntry entry)
    {
        var path = Path.Combine(_historyDir, $"toll-sync-{DateTime.Now:yyyy-MM-dd}.jsonl");
        try
        {
            lock (_ioLock)
            {
                var line = JsonSerializer.Serialize(entry, JsonLineOpts);
                File.AppendAllText(path, line + Environment.NewLine, new UTF8Encoding(false));
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(LogCategory.Sync, $"Không ghi được history '{entry.DataType}': {ex.Message}");
        }
    }

    /// <summary>
    /// Tính FromDate cho lượt kế theo hướng B: <c>From = (watermark ?? now - InitialLookbackHours) - Overlap</c>.
    /// Overlap là cửa sổ chồng lấn để không sót bản ghi về trễ (upsert khử trùng).
    /// </summary>
    public DateTime ComputeFrom(TollSyncCursor cursor, DateTime now)
    {
        var overlap = TimeSpan.FromMinutes(Math.Max(0, _options.OverlapMinutes));
        var lookback = Math.Max(1, _options.InitialLookbackHours);
        var baseFrom = cursor.Watermark ?? now.AddHours(-lookback);
        var from = baseFrom - overlap;
        return from > now ? now : from;
    }

    /// <summary>Cập nhật con trỏ khi lưu DB THÀNH CÔNG: đẩy watermark = To, tăng số lần chạy trong ngày.</summary>
    public void MarkSuccess(TollSyncCursor cursor, DateTime to, int received, int inserted, int updated, int unchanged, int failed)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        cursor.RunsToday = cursor.RunDate == today ? cursor.RunsToday + 1 : 1;
        cursor.RunDate = today;
        cursor.Watermark = to;
        cursor.LastRunAt = DateTime.Now;
        cursor.LastStatus = "Success";
        cursor.LastError = null;
        cursor.LastReceived = received;
        cursor.LastInserted = inserted;
        cursor.LastUpdated = updated;
        cursor.LastUnchanged = unchanged;
        cursor.LastFailed = failed;
        Save(cursor);
    }

    /// <summary>Cập nhật con trỏ khi lỗi: KHÔNG đổi watermark (lần sau lặp lại đúng khoảng).</summary>
    public void MarkFailed(TollSyncCursor cursor, string? error)
    {
        cursor.LastRunAt = DateTime.Now;
        cursor.LastStatus = "Failed";
        cursor.LastError = error;
        Save(cursor);
    }

    private string CursorPath(string dataType) => Path.Combine(_cursorDir, $"toll-cursor-{Sanitize(dataType)}.json");

    private static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "unknown";
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
        return sb.ToString();
    }

    /// <summary>Thư mục gốc lưu state: tuyệt đối dùng luôn; tương đối tính từ thư mục chạy exe.</summary>
    private static string ResolveStateRoot(string? statePath)
    {
        var p = string.IsNullOrWhiteSpace(statePath) ? "State" : statePath.Trim();
        return Path.IsPathRooted(p) ? p : Path.Combine(AppContext.BaseDirectory, p);
    }
}
