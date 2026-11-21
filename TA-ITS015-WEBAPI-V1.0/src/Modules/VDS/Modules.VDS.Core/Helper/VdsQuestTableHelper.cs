using System.Reflection;
using Modules.VDS.Core.Entities;
using Shared.QuestHa.Enums;
using Shared.QuestHa.Infrastructure.Services.Journal.Schema;

namespace Modules.VDS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Định nghĩa bảng QuestDB của VDS cho QuestDB HA (Shared.QuestHa) — sinh từ chính entity
/// (<c>[SugarTable]</c>, <c>[SugarColumn]</c> + <c>ColumnDataType</c>) để entity vẫn là NGUỒN DUY NHẤT của cột/kiểu.
/// DEDUP UPSERT KEYS khai tay theo DDL thật (đối chiếu <c>SHOW COLUMNS</c> trên QuestDB dev 27/10/2026):
/// <c>VdsTrafficData(detectTime, vendorEventId)</c>, <c>VdsTrafficStatistic(fromTime, vendorEventId, lane)</c>.
/// Không có cột <c>journalSeq</c> (tuỳ chọn trong Shared.QuestHa) ⇒ không phải đổi schema QuestDB.
/// Created date: 27/10/2026
/// </summary>
public class VdsQuestTableHelper
{
    private static readonly Lazy<QuestTableSpec> _trafficData =
        new(() => Build<VdsTrafficData>("detectTime", "detectTime", "vendorEventId"));

    private static readonly Lazy<QuestTableSpec> _trafficStatistic =
        new(() => Build<VdsTrafficStatistic>("fromTime", "fromTime", "vendorEventId", "lane"));

    /// <summary>Bảng dữ liệu xe thô <c>VdsTrafficData</c>.</summary>
    public static QuestTableSpec TrafficData => _trafficData.Value;

    /// <summary>Bảng thống kê theo chu kỳ <c>VdsTrafficStatistic</c>.</summary>
    public static QuestTableSpec TrafficStatistic => _trafficStatistic.Value;

    /// <summary>Mọi bảng VDS được nhân bản qua journal (thứ tự: bảng đầu dùng để probe node).</summary>
    public static IReadOnlyList<QuestTableSpec> All => [TrafficData, TrafficStatistic];

    /// <summary>
    /// Dựng định nghĩa bảng từ entity <typeparamref name="T"/>. Ném <see cref="InvalidOperationException"/> khi entity
    /// có cột không ánh xạ được (kiểu chưa hỗ trợ, chuỗi thiếu <c>ColumnDataType</c> symbol/varchar, tên cột khác tên
    /// thuộc tính — payload journal serialize theo tên thuộc tính) hoặc khoá DEDUP không có trong entity.
    /// </summary>
    public static QuestTableSpec Build<T>(string designated, params string[] dedupKeys) where T : VdsQuestEntityTenant
    {
        var type = typeof(T);
        var table = type.GetCustomAttribute<SugarTable>()?.TableName ?? type.Name;
        var columns = new List<QuestColumn>();
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || !p.CanWrite) continue;
            var attr = p.GetCustomAttribute<SugarColumn>(inherit: true);
            if (attr?.IsIgnore == true) continue;

            var name = string.IsNullOrEmpty(attr?.ColumnName) ? p.Name : attr!.ColumnName;
            if (!string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"{table}.{p.Name}: tên cột '{name}' khác tên thuộc tính — payload journal không khớp cột.");

            var isDesignated = string.Equals(name, designated, StringComparison.OrdinalIgnoreCase);
            var isKey = dedupKeys.Contains(name, StringComparer.OrdinalIgnoreCase);
            columns.Add(new QuestColumn(name, ColumnType(table, p, attr), isKey, isDesignated));
        }

        foreach (var key in dedupKeys.Append(designated))
            if (!columns.Any(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"{table}: không có cột '{key}' (khoá DEDUP/designated).");

        return new QuestTableSpec(table, columns);
    }

    private static QuestColumnTypeEnum ColumnType(string table, PropertyInfo p, SugarColumn? attr)
    {
        var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        if (t == typeof(string))
            return attr?.ColumnDataType?.ToLowerInvariant() switch
            {
                "symbol" => QuestColumnTypeEnum.Symbol,
                "varchar" => QuestColumnTypeEnum.Varchar,
                // Thiếu ColumnDataType ⇒ SqlSugar sinh STRING (kiểu chuỗi cũ) — bắt khai rõ như VdsQuestEntityTenant yêu cầu.
                _ => throw new InvalidOperationException($"{table}.{p.Name}: chuỗi phải khai ColumnDataType symbol/varchar."),
            };
        if (t == typeof(DateTime)) return QuestColumnTypeEnum.Timestamp;
        if (t == typeof(long)) return QuestColumnTypeEnum.Long;
        if (t == typeof(int)) return QuestColumnTypeEnum.Int;
        if (t == typeof(short)) return QuestColumnTypeEnum.Short;
        if (t == typeof(double)) return QuestColumnTypeEnum.Double;
        if (t == typeof(bool)) return QuestColumnTypeEnum.Boolean;
        throw new InvalidOperationException($"{table}.{p.Name}: kiểu {t.Name} chưa hỗ trợ trong Shared.QuestHa.");
    }
}
