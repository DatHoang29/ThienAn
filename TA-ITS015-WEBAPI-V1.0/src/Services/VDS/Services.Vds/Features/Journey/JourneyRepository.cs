using Services.Shared.Persistence;
using Services.Vds.Configuration;
using Services.Vds.Shared.Persistence;
using Shared.Utility.Utilities;

namespace Services.Vds.Features.Journey;

/// <summary>
/// Kết quả ghi hành trình: <c>Created=false</c> nghĩa là đã có hành trình cho cùng chuyến.
/// <c>SameStart=false</c> (chỉ khi <c>Created=false</c>) nghĩa là lượt B này ĐÃ được ghép với một lượt A KHÁC —
/// bên gọi không được nâng thêm sự cố cho chuyến này.
/// </summary>
/// <param name="SupersededIds">Hành trình XA hơn cùng lượt B đã bị xoá mềm vì chuyến vừa ghi gần hơn (luật camera gần nhất).</param>
public readonly record struct JourneyInsertResult(string Id, bool Created, bool SameStart = true, IReadOnlyList<string>? SupersededIds = null);
/// <summary>
/// Author: KienLT
/// Description: Ghi bảng <c>TmsTrafficJourney</c> (hành trình A→B khớp theo biển số) trên SQL SERVER.
///
/// <para><b>Luỹ đẳng theo (SegmentId, LicensePlate, FromTime)</b>: <see cref="JourneyJob"/> chạy theo chu
/// kỳ với cửa sổ quét CHỒNG LẤN, nên cùng một chuyến A→B sẽ được ghép lại nhiều lượt. Không chống trùng
/// thì mỗi lượt job ghi một dòng mới cho cùng một chuyến.</para>
///
/// Ở SQL Server nên xử lý trùng an toàn với race: tra trước, không có thì thử insert, lỗi trùng khoá thì
/// tra lại — giống <c>IncidentRepository</c>.
/// Created date: 04/09/2026
/// </summary>
public sealed class JourneyRepository : VdsRepositoryBase
{
    public JourneyRepository(DatabaseManager databaseManager, IVdsConfigManager config)
        : base(databaseManager, config)
    {
    }

    public async Task<JourneyInsertResult> InsertIdempotentAsync(
        TmsTrafficJourney entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ct.ThrowIfCancellationRequested();

        var existing = await FindAsync(entity.SegmentId, entity.LicensePlate, entity.FromTime, ct)
            .ConfigureAwait(false);
        if (existing is not null) return new JourneyInsertResult(existing.ID, false);

        // MỘT lượt B chỉ được một hành trình. Hai đường ghép (theo bản tin / đối soát) có thể nhìn thấy tập lượt A
        // khác nhau (lượt A đến trễ) nên chọn FromTime khác nhau cho CÙNG lượt B; khoá theo FromTime không bắt
        // được ca đó. Chuyến ghi trước được giữ.
        var sameEnd = await FindByEndAsync(entity.SegmentId, entity.LicensePlate, entity.ToTime, ct)
            .ConfigureAwait(false);
        if (sameEnd is not null) return new JourneyInsertResult(sameEnd.ID, false, SameStart: false);

        // Cùng lượt B chỉ giữ hành trình từ camera GẦN NHẤT có bắt được xe (mọi đoạn, kể cả đoạn dài
        // 06→02 song song 06→04 + 04→02). Có chuyến gần hơn ⇒ bỏ; ứng viên gần hơn chuyến đã ghi ⇒ ghi rồi xoá mềm chuyến xa.
        var endingAt = await FindEndingAtAsync(entity.LicensePlate, entity.ToEquipmentId, entity.ToTime, ct).ConfigureAwait(false);
        var nearest = entity.FromTime is { } candidateFrom
            ? JourneyNearestRules.Decide(candidateFrom, entity.DistanceMeter ?? int.MaxValue,
                endingAt.Select(x => new JourneyNearestRules.Existing(x.ID, x.FromTime, x.DistanceMeter)), EndTolerance)
            : new JourneyNearestRules.Decision(true, null, []);
        if (!nearest.Insert) return new JourneyInsertResult(nearest.NearerId!, false, SameStart: false);

        if (string.IsNullOrEmpty(entity.ID)) entity.ID = Guid.NewGuid().ToString();
        entity.TenantId = TenantId;
        entity.CreateTime = BaseUtil.TimeServer.GetTimeServer();
        entity.CreateUId = ServiceUserId;
        // KHÔNG gán IsDelete: cột này là MỐC THỜI GIAN xoá mềm, null nghĩa là chưa xoá.

        try
        {
            await Client.Insertable(entity).ExecuteCommandAsync(ct);
            if (nearest.Superseded.Count > 0) await SoftDeleteAsync(nearest.Superseded, ct).ConfigureAwait(false);
            return new JourneyInsertResult(entity.ID, true, SupersededIds: nearest.Superseded);
        }
        catch (Exception ex) when (ex is SqlSugarException or System.Data.Common.DbException)
        {
            var raced = await FindAsync(entity.SegmentId, entity.LicensePlate, entity.FromTime, ct)
                .ConfigureAwait(false);
            if (raced is not null) return new JourneyInsertResult(raced.ID, false);
            throw;
        }
    }

    /// <summary>Hành trình cùng biển kết thúc ở CÙNG lượt B (camera cuối + mốc kết thúc ± dung sai), MỌI đoạn.</summary>
    private async Task<List<TmsTrafficJourney>> FindEndingAtAsync(
        string? licensePlate, string? toEquipmentId, DateTime? toTime, CancellationToken ct)
    {
        if (toTime is null || string.IsNullOrWhiteSpace(toEquipmentId)) return [];
        var tenantId = TenantId;
        var from = toTime.Value - EndTolerance;
        var to = toTime.Value + EndTolerance;
        return await Client.Queryable<TmsTrafficJourney>()
            .Where(x => x.IsDelete == null && x.TenantId == tenantId
                        && x.LicensePlate == licensePlate && x.ToEquipmentId == toEquipmentId
                        && x.ToTime >= from && x.ToTime <= to)
            .ToListAsync(ct);
    }

    /// <summary>Xoá mềm (đặt mốc <c>IsDelete</c>) các hành trình bị chuyến gần hơn thay thế.</summary>
    private async Task SoftDeleteAsync(IReadOnlyList<string> ids, CancellationToken ct)
    {
        var now = BaseUtil.TimeServer.GetTimeServer();
        var userId = ServiceUserId;
        var list = ids.ToList();
        await Client.Updateable<TmsTrafficJourney>()
            .SetColumns(x => new TmsTrafficJourney { IsDelete = now, UpdateTime = now, UpdateUId = userId })
            .Where(x => list.Contains(x.ID))
            .ExecuteCommandAsync(ct);
    }

    /// <summary>Sai lệch cho phép khi so mốc kết thúc: hai đường ghép lấy mốc B từ bản tin hoặc từ QuestDB.</summary>
    private static readonly TimeSpan EndTolerance = TimeSpan.FromSeconds(1);

    private async Task<TmsTrafficJourney?> FindByEndAsync(
        string? segmentId, string? licensePlate, DateTime? toTime, CancellationToken ct)
    {
        if (toTime is null) return null;
        var tenantId = TenantId;
        var from = toTime.Value - EndTolerance;
        var to = toTime.Value + EndTolerance;
        return await Client.Queryable<TmsTrafficJourney>()
            .Where(x => x.IsDelete == null && x.TenantId == tenantId
                        && x.SegmentId == segmentId
                        && x.LicensePlate == licensePlate
                        && x.ToTime >= from && x.ToTime <= to)
            .FirstAsync(ct);
    }

    private async Task<TmsTrafficJourney?> FindAsync(
        string? segmentId, string? licensePlate, DateTime? fromTime, CancellationToken ct)
    {
        var tenantId = TenantId;
        return await Client.Queryable<TmsTrafficJourney>()
            .Where(x => x.IsDelete == null && x.TenantId == tenantId
                        && x.SegmentId == segmentId
                        && x.LicensePlate == licensePlate
                        && x.FromTime == fromTime)
            .FirstAsync(ct);
    }
}
