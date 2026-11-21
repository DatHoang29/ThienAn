using Modules.TMS.Core.Entities;
using Services.Shared.Persistence;
using Services.Vds.Configuration;
using Services.Vds.Shared.Persistence;
using Shared.Utility.Utilities;

namespace Services.Vds.Features.Summary;

/// <summary>
/// Author: KienLT
/// Description: Đọc/ghi bảng <c>TmsTrafficSummary</c> trên SQL SERVER — nơi lưu số liệu tổng hợp lâu dài
/// theo giờ/ngày/tháng.
///
/// <para><b>Vì sao ghi ở SQL Server chứ không phải QuestDB</b>: chính là để làm FALLBACK khi QuestDB lỗi.
/// Số đếm thời gian thực tính lại từ bảng thô QuestDB mỗi lần cần; nếu summary cũng nằm trên QuestDB thì
/// QuestDB chết là mất cả hai. Đặt summary trên SQL Server để báo cáo vẫn xem được khi QuestDB không tới
/// được.</para>
///
/// <para><b>Luỹ đẳng qua UPSERT theo khoá</b> <c>(TenantId, PeriodType, ScopeType, ScopeId, Direction,
/// FromTime)</c>: job chạy lại trên cùng giờ/scope thì CẬP NHẬT bản ghi đang có thay vì thêm dòng mới.
/// Direction có thể null (phạm vi zone không mang hướng) nên khoá so sánh coi null như chuỗi rỗng.</para>
/// Created date: 03/09/2026
/// </summary>
public sealed class SqlTrafficSummaryRepository : VdsRepositoryBase
{
    public async Task ReplacePeriodAsync(List<TmsTrafficSummary> rows, string period, DateTime from, DateTime to, CancellationToken ct)
    {
        var db = Client;
        var tenant = TenantId;
        await db.Ado.BeginTranAsync();
        try
        {
            await db.Deleteable<TmsTrafficSummary>().Where(x => x.TenantId == tenant && x.PeriodType == period
                && x.FromTime == from && (x.ScopeType == "equipment" || x.ScopeType == "zone")).ExecuteCommandAsync(ct);
            foreach (var row in rows)
            {
                row.ID = Guid.NewGuid().ToString(); row.TenantId = tenant;
                row.CreateTime = DateTime.Now; row.CreateUId = ServiceUserId;
            }
            if (rows.Count > 0) await db.Insertable(rows).ExecuteCommandAsync(ct);
            await db.Ado.CommitTranAsync();
        }
        catch { await db.Ado.RollbackTranAsync(); throw; }
    }
    public SqlTrafficSummaryRepository(DatabaseManager databaseManager, IVdsConfigManager config)
        : base(databaseManager, config)
    {
    }

    /// <summary>
    /// Ghi (hoặc cập nhật) một bản ghi tổng hợp. Đối chiếu theo khoá nghiệp vụ; trùng thì UPDATE các cột
    /// số liệu, không sinh dòng thứ hai.
    /// </summary>
    public async Task UpsertAsync(TmsTrafficSummary summary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ct.ThrowIfCancellationRequested();

        var tenantId = TenantId;
        var periodType = summary.PeriodType;
        var scopeType = summary.ScopeType;
        var scopeId = summary.ScopeId;
        var direction = summary.Direction ?? string.Empty;
        var fromTime = summary.FromTime;

        // Tìm bản ghi đang có theo khoá nghiệp vụ. So Direction qua chuỗi rỗng vì cột nullable.
        var existing = await Client.Queryable<TmsTrafficSummary>()
            .Where(x => x.IsDelete == null
                        && x.TenantId == tenantId
                        && x.PeriodType == periodType
                        && x.ScopeType == scopeType
                        && x.ScopeId == scopeId
                        && (x.Direction ?? "") == direction
                        && x.FromTime == fromTime)
            .FirstAsync(ct);

        if (existing is null)
        {
            summary.ID = string.IsNullOrEmpty(summary.ID) ? Guid.NewGuid().ToString() : summary.ID;
            summary.TenantId = tenantId;
            summary.CreateTime = BaseUtil.TimeServer.GetTimeServer();
            summary.CreateUId = ServiceUserId;
            await Client.Insertable(summary).ExecuteCommandAsync(ct);
            return;
        }

        // Chỉ cập nhật cột SỐ LIỆU; giữ nguyên khoá và dấu vết tạo.
        existing.ToTime = summary.ToTime;
        existing.SVehicleNumber = summary.SVehicleNumber;
        existing.MVehicleNumber = summary.MVehicleNumber;
        existing.LVehicleNumber = summary.LVehicleNumber;
        existing.OtherVehicleNumber = summary.OtherVehicleNumber;
        existing.AvgSpeed = summary.AvgSpeed;
        existing.MinSpeed = summary.MinSpeed;
        existing.MaxSpeed = summary.MaxSpeed;
        existing.AvgSpeedS = summary.AvgSpeedS;
        existing.AvgSpeedM = summary.AvgSpeedM;
        existing.AvgSpeedL = summary.AvgSpeedL;
        existing.AvgSpeedOther = summary.AvgSpeedOther;
        existing.SampleCount = summary.SampleCount;
        existing.SampleCountS = summary.SampleCountS;
        existing.SampleCountM = summary.SampleCountM;
        existing.SampleCountL = summary.SampleCountL;
        existing.SampleCountOther = summary.SampleCountOther;
        existing.ComputedAt = summary.ComputedAt;

        await Client.Updateable(existing).ExecuteCommandAsync(ct);
    }

    /// <summary>
    /// Các bản ghi summary của một <paramref name="periodType"/> có <c>FromTime</c> trong
    /// <c>[from, to)</c> — dùng để gộp lên mức lớn hơn (day từ hour, month từ day) mà không đụng QuestDB.
    /// </summary>
    public Task<List<TmsTrafficSummary>> QueryRangeAsync(
        string periodType, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var tenantId = TenantId;
        return Client.Queryable<TmsTrafficSummary>()
            .Where(x => x.IsDelete == null
                        && x.TenantId == tenantId
                        && x.PeriodType == periodType
                        && x.FromTime >= from
                        && x.FromTime < to)
            .ToListAsync(ct);
    }
}
