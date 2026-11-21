using System.Text.Json;
using ITS.Toll.Core.Configuration;
using ITS.Toll.Core.Logging;
using ITS.Toll.Core.Models;

namespace ITS.Toll.Core.Services;

/// <summary>
/// Điều phối luồng NHẬN dữ liệu do TCS chủ động đẩy sang ITS (push).
///   - Push 1 bản ghi kèm thao tác (Insert/Update/Delete) → upsert/xóa trong DB (qua <see cref="TollDataStore"/>).
///   - Báo import theo ngày → ITS tự gọi lại (pull) TCS lấy dữ liệu các ngày liên quan rồi lưu.
/// Chỉ xử lý dữ liệu giao dịch/tổng hợp: TransactionIn, TransactionOut, OutSummary
/// (master/danh mục không nhận qua push — đã có luồng pull tự động).
/// Author: ChuongLP
/// Created date: 12/09/2026
/// </summary>
public sealed class TollCallBackService
{
    private readonly TollDataStore _dataStore;
    private readonly TollApiClient _apiClient;
    private readonly TollOpConfigReader _configReader;
    private readonly TollLogger _logger;

    /// <summary>Các DataType được phép nhận qua push/import.</summary>
    public static readonly IReadOnlySet<string> SupportedDataTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TransactionIn", "TransactionOut", "OutSummary" };

    /// <summary>Các thao tác hợp lệ cho endpoint record.</summary>
    public static readonly IReadOnlySet<string> SupportedOperations =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Insert", "Update", "Delete" };

    public TollCallBackService(TollDataStore dataStore, TollApiClient apiClient, TollOpConfigReader configReader, TollLogger logger)
    {
        _dataStore = dataStore;
        _apiClient = apiClient;
        _configReader = configReader;
        _logger = logger;
    }

    /// <summary>Chuẩn hóa DataType về đúng chữ hoa/thường như catalog (null nếu không hỗ trợ).</summary>
    public static string? NormalizeDataType(string? dataType)
    {
        if (string.IsNullOrWhiteSpace(dataType)) return null;
        return SupportedDataTypes.FirstOrDefault(t => string.Equals(t, dataType.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Chuẩn hóa Operation về Insert/Update/Delete (null nếu không hợp lệ).</summary>
    public static string? NormalizeOperation(string? operation)
    {
        if (string.IsNullOrWhiteSpace(operation)) return null;
        return SupportedOperations.FirstOrDefault(o => string.Equals(o, operation.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Xử lý 1 bản ghi TCS đẩy sang theo thao tác. <paramref name="dataType"/> và <paramref name="operation"/>
    /// phải đã được chuẩn hóa (gọi Normalize* trước). Delete lấy id theo <paramref name="sourceId"/>;
    /// nếu rỗng thì tự rút từ <paramref name="data"/> theo khóa của DataType.
    /// </summary>
    public async Task<TollSaveResult> HandleRecordAsync(string dataType, string operation, JsonElement data, string? sourceId, CancellationToken ct = default)
    {
        _logger.Info(LogCategory.CallBack, $"[PUSH] Nhận record [{dataType}] · thao tác {operation}.");

        if (string.Equals(operation, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            var id = string.IsNullOrWhiteSpace(sourceId) ? ExtractSourceId(dataType, data) : sourceId!.Trim();
            var del = await _dataStore.DeleteRecordAsync(dataType, id ?? string.Empty, ct);
            LogResult(del);
            return del;
        }

        // Insert / Update → gộp về upsert (idempotent).
        var save = await _dataStore.UpsertRecordAsync(dataType, data, ct);
        LogResult(save);
        return save;
    }

    /// <summary>
    /// Báo import: ITS tự gọi lại TCS theo khoảng ngày rồi lưu (upsert). Chạy đồng bộ trong tác vụ nền
    /// do endpoint gọi (endpoint trả 202 ngay).
    /// </summary>
    public async Task<TollSaveResult> RepullRangeAsync(string dataType, DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        _logger.Info(LogCategory.CallBack, $"[IMPORT] Pull lại [{dataType}] khoảng {fromDate:yyyy-MM-dd HH:mm:ss} … {toDate:yyyy-MM-dd HH:mm:ss}.");

        TollOptions options;
        try
        {
            options = _configReader.Load();
        }
        catch (Exception ex)
        {
            var msg = $"Không đọc được cấu hình endpoint (SysOpConfig '{TollOpConfigReader.GroupCode}'): {ex.Message}";
            _logger.Error(LogCategory.CallBack, $"[IMPORT] {msg}");
            return new TollSaveResult(false, msg);
        }

        var endpoint = options.Endpoints.FirstOrDefault(e => string.Equals(e.DataType, dataType, StringComparison.OrdinalIgnoreCase));
        if (endpoint is null)
        {
            var msg = $"Chưa cấu hình endpoint cho DataType='{dataType}' trong SysOpConfig ('{TollOpConfigReader.GroupCode}').";
            _logger.Error(LogCategory.CallBack, $"[IMPORT] {msg}");
            return new TollSaveResult(false, msg);
        }

        // Luồng import/re-pull LUÔN gửi khoảng ngày để lấy đúng ngày TCS yêu cầu.
        // (OutSummary trong catalog vốn RequiresDateRange=false → ép true riêng cho re-pull,
        //  không ảnh hưởng hành vi pull tự động của form.)
        endpoint.RequiresDateRange = true;

        var result = await _apiClient.CallAsync(endpoint, options, fromDate, toDate, ct);
        if (!result.Success)
            return new TollSaveResult(false, result.Error ?? "Gọi API TCS thất bại");

        var save = await _dataStore.SaveAsync(result, ct);
        return save;
    }

    /// <summary>Rút id bên TCS từ payload theo khóa nghiệp vụ của DataType (dùng cho Delete khi không gửi kèm sourceId).</summary>
    private static string? ExtractSourceId(string dataType, JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) return null;
        // OutSummary xóa theo ticket_Confirmed_ID (cột ConfirmedId); transaction xóa theo transaction_ID.
        var key = string.Equals(dataType, "OutSummary", StringComparison.OrdinalIgnoreCase) ? "ticket_Confirmed_ID" : "transaction_ID";
        foreach (var p in data.EnumerateObject())
        {
            if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                return p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText();
        }
        return null;
    }

    private void LogResult(TollSaveResult r)
    {
        if (r.Ok) _logger.Success(LogCategory.CallBack, $"[PUSH] {r.Message}");
        else _logger.Error(LogCategory.CallBack, $"[PUSH] {r.Message}");
    }
}
