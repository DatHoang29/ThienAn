using Services.Shared.Persistence;
using Services.Vds.Shared.Persistence;
using Services.Vds.Configuration;
using Shared.Utility.Utilities;
using Shared.QuestHa;
using Shared.QuestHa.Enums;

namespace Services.Vds.Pipeline.Persistence;

/// <summary>
/// Author: KienLT
/// Description: Đọc/ghi bảng <c>VdsTrafficData</c> trên QuestDB bằng SqlSugar/PgWire.
///
/// <para>Connection string bắt buộc có <c>ServerCompatibilityMode=NoTypeLoading</c>; nếu thiếu, Npgsql
/// tải catalog kiểu PostgreSQL từ QuestDB rồi parameter chuỗi thất bại với lỗi
/// <c>NpgsqlDbType 'Text' isn't present</c>.</para>
///
/// Quy tắc bắt buộc cho MỌI method ĐỌC ở đây: phải có điều kiện trên <c>DetectTime</c>.
/// QuestDB phân vùng theo ngày; có filter thời gian thì nó chỉ đọc vài phân vùng, không có thì quét cả
/// bảng và server tự huỷ query với lỗi <c>cancelled by user</c>.
/// Created date: 24/08/2026
/// </summary>
public sealed class QuestTrafficDataRepository : QuestRepositoryBase
{
    // KienLT - 27/10/2026: Direct không resolve/call journal; Shadow dùng chung chính sách lỗi.
    // KienLT - 31/10/2026: ghi HA qua QuestHaManager (cửa vào duy nhất của Shared.QuestHa); null = Direct, giữ đường cũ.
    private readonly QuestHaManager? _questHa;

    public QuestTrafficDataRepository(
        DatabaseManager databaseManager, IVdsConfigManager config, QuestAvailabilityGate gate,
        QuestHaManager? questHa = null)
        : base(databaseManager, config, gate, questHa)
    {
        _questHa = questHa;
    }

    /// <summary>
    /// Đóng dấu audit rồi ghi parameterized bằng SqlSugar/PgWire. Không SELECT kiểm tra trước: bảng đã
    /// khai <c>DEDUP UPSERT KEYS(detectTime, vendorEventId)</c>, nên replay cùng khóa sẽ ghi đè.
    /// </summary>
    public async Task InsertAsync(VdsTrafficData entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ct.ThrowIfCancellationRequested();

        Canonicalize(entity); // KienLT - 27/10/2026: dùng chung với đường journal QuestDB HA

        // KienLT - 27/10/2026: Journal ACK sau SQL commit; không chạm node chính/cầu dao ghi trực tiếp.
        if (_questHa?.WriteMode == QuestWriteModeEnum.Journal)
        {
            await _questHa.AppendAsync(Modules.VDS.Core.Helper.VdsBaseHelper.QuestTableHp.TrafficData, entity, ct).ConfigureAwait(false);
            return;
        }

        await GuardWriteAsync(db => db.Insertable(entity)
            .UseParameter()
            .ExecuteCommandAsync(), ct);

        // KienLT - 27/10/2026: chỉ sao chép sau khi node chính ghi thành công; không chuẩn hoá lại.
        if (_questHa?.WriteMode == QuestWriteModeEnum.Shadow)
            _questHa.CopyToShadow(Modules.VDS.Core.Helper.VdsBaseHelper.QuestTableHp.TrafficData, entity);
    }

    /// <summary>Đếm bản ghi trong cửa sổ <c>[from, to)</c>.</summary>
    public Task<int> CountAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        ReadAsync(db => db.Queryable<VdsTrafficData>()
            .Where(x => x.DetectTime >= from && x.DetectTime < to)
            .CountAsync(ct), ct);

    /// <summary>Đọc đúng một DEDUP key trong cửa sổ 1ms quanh designated timestamp.</summary>
    public Task<List<VdsTrafficData>> ByEventKeyAsync(
        DateTime detectTime, long vendorEventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var to = detectTime.AddMilliseconds(1);

        return ReadAsync(db => db.Queryable<VdsTrafficData>()
            .Where(x => x.DetectTime >= detectTime
                        && x.DetectTime < to
                        && x.VendorEventId == vendorEventId)
            .ToListAsync(ct), ct);
    }

    /// <summary>Các bản ghi mới nhất trong cửa sổ — để kiểm tra bằng mắt.</summary>
    public Task<List<VdsTrafficData>> LatestAsync(
        DateTime from, DateTime to, int take = 10, CancellationToken ct = default) =>
        ReadAsync(db => db.Queryable<VdsTrafficData>()
            .Where(x => x.DetectTime >= from && x.DetectTime < to)
            .OrderBy(x => x.DetectTime, OrderByType.Desc)
            .Take(take)
            .ToListAsync(ct), ct);
}
