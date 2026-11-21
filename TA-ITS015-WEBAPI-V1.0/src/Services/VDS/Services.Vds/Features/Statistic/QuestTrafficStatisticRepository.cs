using Services.Shared.Persistence;
using Services.Vds.Shared.Persistence;
using Services.Vds.Configuration;
using Shared.Utility.Utilities;
using Shared.QuestHa;
using Shared.QuestHa.Enums;

namespace Services.Vds.Features.Statistic;

/// <summary>
/// Author: KienLT
/// Description: Đọc/ghi bảng <c>VdsTrafficStatistic</c> trên QuestDB bằng SqlSugar/PgWire.
///
/// Cùng khuôn với <c>QuestTrafficDataRepository</c>: không SELECT kiểm tra trước khi ghi vì bảng đã khai
/// <c>DEDUP UPSERT KEYS(fromTime, vendorEventId, lane)</c> — replay cùng bộ khoá thì QuestDB ghi đè.
///
/// Quy tắc bắt buộc cho MỌI method ĐỌC ở đây: phải có điều kiện trên <c>FromTime</c>. QuestDB phân vùng
/// theo ngày; có filter thời gian thì chỉ đọc vài phân vùng, không có thì quét cả bảng và server tự huỷ
/// query.
/// Created date: 26/08/2026
/// </summary>
public sealed class QuestTrafficStatisticRepository : QuestRepositoryBase
{
    // KienLT - 27/10/2026: Direct không resolve/call journal; Shadow dùng chung chính sách lỗi.
    // KienLT - 31/10/2026: ghi HA qua QuestHaManager (cửa vào duy nhất của Shared.QuestHa); null = Direct, giữ đường cũ.
    private readonly QuestHaManager? _questHa;

    public QuestTrafficStatisticRepository(
        DatabaseManager databaseManager, IVdsConfigManager config, QuestAvailabilityGate gate,
        QuestHaManager? questHa = null)
        : base(databaseManager, config, gate, questHa)
    {
        _questHa = questHa;
    }

    /// <summary>Đóng dấu audit rồi ghi parameterized.</summary>
    public async Task InsertAsync(VdsTrafficStatistic entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ct.ThrowIfCancellationRequested();

        Canonicalize(entity); // KienLT - 27/10/2026: dùng chung với đường journal QuestDB HA

        // KienLT - 27/10/2026: Journal ACK sau SQL commit; không chạm node chính/cầu dao ghi trực tiếp.
        if (_questHa?.WriteMode == QuestWriteModeEnum.Journal)
        {
            await _questHa.AppendAsync(Modules.VDS.Core.Helper.VdsBaseHelper.QuestTableHp.TrafficStatistic, entity, ct).ConfigureAwait(false);
            return;
        }

        await GuardWriteAsync(db => db.Insertable(entity)
            .UseParameter()
            .ExecuteCommandAsync(), ct);

        // KienLT - 27/10/2026: chỉ sao chép sau khi node chính ghi thành công; không chuẩn hoá lại.
        if (_questHa?.WriteMode == QuestWriteModeEnum.Shadow)
            _questHa.CopyToShadow(Modules.VDS.Core.Helper.VdsBaseHelper.QuestTableHp.TrafficStatistic, entity);
    }

    /// <summary>Đếm bản ghi trong cửa sổ <c>[from, to)</c>.</summary>
    public Task<int> CountAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        ReadAsync(db => db.Queryable<VdsTrafficStatistic>()
            .Where(x => x.FromTime >= from && x.FromTime < to)
            .CountAsync(ct), ct);

    /// <summary>Đọc đúng một bộ DEDUP key trong cửa sổ 1ms quanh đầu chu kỳ — để kiểm chứng khi vận hành.</summary>
    public Task<List<VdsTrafficStatistic>> ByPeriodKeyAsync(
        DateTime fromTime, long vendorEventId, string? lane, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var to = fromTime.AddMilliseconds(1);
        var laneKey = lane ?? string.Empty;

        return ReadAsync(db => db.Queryable<VdsTrafficStatistic>()
            .Where(x => x.FromTime >= fromTime
                        && x.FromTime < to
                        && x.VendorEventId == vendorEventId
                        && x.Lane == laneKey)
            .ToListAsync(ct), ct);
    }

    /// <summary>Các bản ghi mới nhất trong cửa sổ — để kiểm tra bằng mắt.</summary>
    public Task<List<VdsTrafficStatistic>> LatestAsync(
        DateTime from, DateTime to, int take = 10, CancellationToken ct = default) =>
        ReadAsync(db => db.Queryable<VdsTrafficStatistic>()
            .Where(x => x.FromTime >= from && x.FromTime < to)
            .OrderBy(x => x.FromTime, OrderByType.Desc)
            .Take(take)
            .ToListAsync(ct), ct);
}
