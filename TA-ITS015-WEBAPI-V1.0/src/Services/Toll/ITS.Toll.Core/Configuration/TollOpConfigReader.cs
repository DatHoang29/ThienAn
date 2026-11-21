using ITS.Toll.Core.Persistence;
using Microsoft.Extensions.Options;
using Modules.CfgSystem.Core.Entities;
using Services.Shared.Configuration;
using Services.Shared.Persistence;
using Shared.DTO.Enums;

namespace ITS.Toll.Core.Configuration;

/// <summary>
/// Đọc cấu hình Toll TỪ BẢNG SysOpConfig (nhóm <see cref="GroupCode"/>) bằng REPOSITORY DÙNG CHUNG
/// (kế thừa <see cref="TollRepositoryBase"/> → <c>SqlSugarAccess</c>), dùng entity <see cref="SysOpConfig"/> THẬT
/// của Modules.CfgSystem.Core (không tạo entity riêng, không dùng MessBus).
/// Mỗi tham số/endpoint là 1 dòng:
///   - Tham số chung: base_url / timeout_seconds / date_format.
///   - Mỗi endpoint 1 dòng: Code trong <see cref="_catalog"/>, Value = URL; DataType/RequiresDateRange cố định theo Code;
///     Enabled suy từ Status (Disable ⇒ tắt).
/// Author: ChuongLP
/// Created date: 26/08/2026
/// </summary>
public sealed class TollOpConfigReader : TollRepositoryBase
{
    /// <summary>Nhóm cấu hình (GroupCode) của Toll trong SysOpConfig.</summary>
    public const string GroupCode = "toll_config";

    // Mã tham số chung.
    public const string CodeBaseUrl = "base_url";
    public const string CodeTimeoutSeconds = "timeout_seconds";
    public const string CodeDateFormat = "date_format";

    private const string DefaultDateFormat = "yyyy-MM-dd HH:mm:ss";
    private const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// Catalog endpoint cố định: Code cấu hình -> (Key, DataType, có cần khoảng ngày).
    /// URL lấy từ Value của dòng cấu hình; Enabled suy từ Status.
    /// </summary>
    private static readonly (string Code, string Key, string DataType, bool RequiresDateRange)[] _catalog =
    {
        ("api_master",          "master",         "Master",         false),
        ("api_station",         "station",        "Station",        false),
        ("api_lane",            "lane",           "Lane",           false),
        ("api_shift",           "shift",          "Shift",          false),
        ("api_transaction_in",  "transactionIn",  "TransactionIn",  true),
        ("api_transaction_out", "transactionOut", "TransactionOut", true),
        ("api_out_summary",     "outSummary",     "OutSummary",     false),
        ("api_access_log",      "accessLog",      "AccessLog",      true),
    };

    public TollOpConfigReader(DatabaseManager databaseManager, IOptions<DbConnectionOptions> dbOptions, TollServiceOptions service)
        : base(databaseManager, dbOptions, service)
    {
    }

    /// <summary>
    /// Nạp cấu hình từ SysOpConfig (nhóm toll_config). Ném lỗi rõ nếu không có dòng nào (chưa cấu hình).
    /// </summary>
    public TollOptions Load()
    {
        var rows = Client.Queryable<SysOpConfig>()
            .Where(x => x.GroupCode == GroupCode && x.IsDelete == null)
            .ToList();

        if (rows.Count == 0)
            throw new InvalidOperationException(
                $"Chưa có cấu hình Toll trong bảng SysOpConfig (GroupCode='{GroupCode}'). " +
                "Hãy tạo cấu hình trên màn hình 'Cấu hình hoạt động' rồi chạy lại.");

        // Gom theo Code (không phân biệt hoa/thường), mỗi Code lấy dòng đầu.
        var byCode = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .GroupBy(x => x.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        string? GetValue(string code) => byCode.TryGetValue(code, out var r) ? r.Value?.Trim() : null;

        var options = new TollOptions
        {
            BaseUrl = GetValue(CodeBaseUrl),
            DateFormat = string.IsNullOrWhiteSpace(GetValue(CodeDateFormat)) ? DefaultDateFormat : GetValue(CodeDateFormat)!,
            TimeoutSeconds = int.TryParse(GetValue(CodeTimeoutSeconds), out var timeout) && timeout > 0 ? timeout : DefaultTimeoutSeconds,
        };

        foreach (var (code, key, dataType, requiresDateRange) in _catalog)
        {
            if (!byCode.TryGetValue(code, out var row)) continue;      // chưa cấu hình endpoint này -> bỏ qua
            if (string.IsNullOrWhiteSpace(row.Value)) continue;        // không có URL -> bỏ qua

            options.Endpoints.Add(new TollApiEndpoint
            {
                Key = key,
                Name = string.IsNullOrWhiteSpace(row.Name) ? key : row.Name!.Trim(),
                Url = row.Value!.Trim(),
                DataType = dataType,
                RequiresDateRange = requiresDateRange,
                Enabled = row.Status != BaseEnums.StatusEnum.Disable,   // Disable ⇒ tắt; Enable/null ⇒ bật
            });
        }

        return options;
    }
}
