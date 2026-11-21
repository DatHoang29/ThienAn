using Shared.Utility.Helpers;
using Modules.TMS.Core.Entities;
using Modules.TMS.Controllers.ZoneStatus.Dto;
using Modules.VDS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Utility.Utilities;
using Modules.VDS.Core.Constants;
using Shared.Utility.Sse;
using Modules.TMS.Core.Constants;

namespace Modules.TMS.Controllers.ZoneStatus.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler cho snapshot trạng thái vùng VDS, kèm cờ quá hạn theo cấu hình.
///
/// <para>Dùng chung cho cả hai đường ra: <c>Snapshot</c> gọi một lần, <c>Stream</c> gọi lặp theo chu kỳ
/// SSE — nên ngưỡng quá hạn phải đọc lại mỗi lần chạy để đổi cấu hình có hiệu lực ngay, không cache.</para>
/// Created date: 21/09/2026
/// </summary>
public class ZoneStatusSnapshotQueryHandler : IWolverineHandler
{
    /// <summary>Ngưỡng quá hạn mặc định khi cấu hình thiếu hoặc không hợp lệ.</summary>
    private const int DefaultStaleSeconds = 30;

    private readonly BaseRepository<TmsZoneStatus> _repo;
    private readonly BaseRepository<VdsConfig> _settings;

    public ZoneStatusSnapshotQueryHandler(BaseRepository<TmsZoneStatus> repo, BaseRepository<VdsConfig> settings)
    {
        _repo = repo;
        _settings = settings;
    }

    /// <summary>Trạng thái hiện tại của các vùng kèm cờ quá hạn.</summary>
    public async Task<ZoneStatusSnapshotOutput> HandleAsync(ZoneStatusSnapshotInput command, CancellationToken cancellationToken)
    {
        using var diagnostic = SseDiagnosticOperation.Begin("zone-snapshot-sql", cancellationToken);
        diagnostic.Phase = "reading-config";
        try
        {
            // Một truy vấn cho cả hai khoá: Stream gọi lại mỗi nhịp SSE.
            var settings = await _settings.GetListAsync(x => x.IsDelete == null
                && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable
                && ((x.GroupCode == VdsConfigConst.Group.ConfigDefault && x.Code == VdsConfigConst.Code.ZoneStaleSeconds)
                    || (x.GroupCode == VdsConfigConst.Group.Config && x.Code == VdsConfigConst.Code.ZoneSlowWindow)));
            var configured = settings.FirstOrDefault(x => x.Code == VdsConfigConst.Code.ZoneStaleSeconds);
            var seconds = command.StaleAfterSeconds ?? BaseHelper.DurationHp.TryParseSeconds(configured?.Value) ?? DefaultStaleSeconds;
            if (seconds is < 1 or > 86400) seconds = DefaultStaleSeconds;

            // Tốc độ TB có hạn RIÊNG (cửa sổ tính TB + biên), dài hơn hạn trạng thái — xem ZoneSpeedFreshnessConst.
            var speedMaxAge = ZoneSpeedFreshnessConst.MaxAgeSeconds(
                settings.FirstOrDefault(x => x.Code == VdsConfigConst.Code.ZoneSlowWindow)?.Value);

            // ZoneStatus được worker ghi bằng giờ server-local ⇒ so sánh stale bằng giờ server (BaseUtil.TimeServer).
            var serverNow = BaseUtil.TimeServer.GetTimeServer();
            diagnostic.Phase = "sql-read";
            var rows = await _repo.AsQueryable().Where(x => x.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
                .ToListAsync(cancellationToken);

            diagnostic.Complete(rows.Count);
            return new ZoneStatusSnapshotOutput
            {
                ComputedAt = serverNow,
                StaleAfterSeconds = seconds,
                Zones = rows.Select(x => new ZoneStatusSnapshotRow
                {
                    ZoneId = x.ZoneId,
                    // Mốc tương lai cũng bị coi là quá hạn: đồng hồ lệch thì dữ liệu không đáng tin.
                    Condition = IsStale(x.UpdateTime, serverNow, seconds) ? "normal" : x.Condition,
                    // Trạng thái quá hạn nhưng tốc độ còn hạn ⇒ GIỮ số cuối (không ghi lại nghĩa là số không đổi).
                    AverageSpeed = ZoneSpeedFreshnessConst.IsFresh(x.UpdateTime, serverNow, speedMaxAge) ? x.AverageSpeed : null,
                    UpdateTime = x.UpdateTime,
                    Stale = IsStale(x.UpdateTime, serverNow, seconds)
                }).ToList()
            };
        }
        catch (OperationCanceledException ex) { diagnostic.Cancelled(ex); throw; }
        catch (Exception ex) { diagnostic.Failed(ex); throw; }
    }

    private static bool IsStale(DateTime? updateTime, DateTime serverNow, int staleAfterSeconds)
        => updateTime == null
           || (serverNow - updateTime.Value).TotalSeconds >= staleAfterSeconds
           || updateTime.Value > serverNow;
}
