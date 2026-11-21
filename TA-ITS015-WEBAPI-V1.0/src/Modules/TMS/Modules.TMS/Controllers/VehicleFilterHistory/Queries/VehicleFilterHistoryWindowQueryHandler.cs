using Modules.TMS.Infrastructure.Services.VehicleFilterHistory.Rules;
using Modules.TMS.Core.Entities;
using Modules.TMS.Controllers.VehicleFilterHistory.Dto;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.VehicleFilterHistory.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler cho lịch sử đối chiếu biển số với danh sách lọc phương tiện.
///
/// <para>Bảng <c>TmsVehicleFilterHistory</c> nằm ở SQL Server (worker ghi), nên đọc qua
/// <see cref="BaseRepository{T}"/> của TMS, không qua cầu dao QuestDB.</para>
/// Created date: 21/09/2026
/// </summary>
public class VehicleFilterHistoryWindowQueryHandler : IWolverineHandler
{
    private readonly BaseRepository<TmsVehicleFilterHistory> _repo;

    public VehicleFilterHistoryWindowQueryHandler(BaseRepository<TmsVehicleFilterHistory> repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Điều kiện lọc chung của lượt và chuyến. Lọc theo camera dùng cột <c>EquipmentId</c> (worker ghi từ
    /// 23/10/2026) — dòng cũ không có camera nên không lọt khi lọc theo camera.
    /// </summary>
    private ISugarQueryable<TmsVehicleFilterHistory> Filter(VehicleFilterHistoryWindowInput command)
    {
        var (from, to) = TmsVehicleFilterHistoryReadRules.Window(command);
        var plate = TmsVehicleFilterHistoryReadRules.NormalizePlate(command.LicensePlate);

        return _repo.AsQueryable()
            .Where(x => x.IsDelete == null && x.DetectTime >= from && x.DetectTime < to)
            .WhereIF(!string.IsNullOrWhiteSpace(plate), x => x.LicensePlate!.Replace("-", "").Replace(".", "").Replace(" ", "").ToUpper().Contains(plate))
            .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
            .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
            .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), x => x.Direction == command.Direction)
            .WhereIF(!string.IsNullOrWhiteSpace(command.Type), x => x.Type == command.Type);
    }

    /// <summary>Phân trang lịch sử đối chiếu trong cửa sổ thời gian (từng lượt).</summary>
    public Task<SqlSugarPagedList<VehicleFilterHistoryWindowOutput>> HandleAsync(VehicleFilterHistoryWindowInput command)
    {
        var sort = TmsVehicleFilterHistorySortRules.Sort(command);

        return Filter(command)
            .OrderBy(sort).OrderBy(x => x.ID, OrderByType.Desc)
            .Select(x => new VehicleFilterHistoryWindowOutput
            {
                Id = x.ID,
                LicensePlate = x.LicensePlate,
                Type = x.Type,
                DetectTime = x.DetectTime,
                Direction = x.Direction,
                ZoneId = x.ZoneId,
                LaneId = x.LaneId,
                LocationId = x.LocationId,
                EquipmentId = x.EquipmentId,
                TripId = x.TripId
            })
            .ToPagedListAsync(command.Page, command.PageSize);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Phân trang theo CHUYẾN (mới nhất trước): mỗi dòng một chuyến kèm lộ trình camera theo thời gian.
    /// Dòng cũ chưa có <c>TripId</c> (trước 23/10/2026) mỗi lượt tính là một chuyến riêng.
    /// Created date: 23/10/2026
    /// </summary>
    public async Task<SqlSugarPagedList<VehicleFilterHistoryTripOutput>> HandleAsync(VehicleFilterHistoryTripInput command)
    {
        var window = command.ToWindow();

        // Bước 1: trang các chuyến (khoá = TripId, dòng cũ dùng ID) theo lượt cuối giảm dần.
        RefAsync<int> total = 0;
        var keys = await Filter(window)
            .GroupBy(x => SqlFunc.IsNull(x.TripId, x.ID))
            .Select(x => new TripKeyRow
            {
                Key = SqlFunc.IsNull(x.TripId, x.ID),
                LastTime = SqlFunc.AggregateMax(x.DetectTime)
            })
            .MergeTable()
            .OrderBy(x => x.LastTime, OrderByType.Desc)
            .ToPageListAsync(command.Page, command.PageSize, total);

        var result = new SqlSugarPagedList<VehicleFilterHistoryTripOutput>
        {
            Current = command.Page,
            Size = command.PageSize,
            Total = total.Value,
            TotalPages = command.PageSize > 0 ? (int)Math.Ceiling(total.Value / (double)command.PageSize) : 0,
            Records = new List<VehicleFilterHistoryTripOutput>(),
        };
        result.HasPrevPage = result.Current > 1;
        result.HasNextPage = result.Current < result.TotalPages;
        if (keys.Count == 0) return result;

        // Bước 2: mọi lượt của các chuyến trong trang (cùng điều kiện lọc) ⇒ dựng lộ trình theo thời gian.
        var keyList = keys.Select(k => k.Key).ToList();
        var hits = await Filter(window)
            .Where(x => keyList.Contains(SqlFunc.IsNull(x.TripId, x.ID)))
            .OrderBy(x => x.DetectTime)
            .Select(x => new
            {
                x.ID, x.TripId, x.LicensePlate, x.Type, x.DetectTime,
                x.Direction, x.LocationId, x.ZoneId, x.EquipmentId
            })
            .ToListAsync();

        var records = new List<VehicleFilterHistoryTripOutput>();
        var byKey = hits.GroupBy(h => h.TripId ?? h.ID).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var key in keyList)
        {
            if (!byKey.TryGetValue(key, out var rows) || rows.Count == 0) continue;
            var first = rows[0];
            var last = rows[^1];
            records.Add(new VehicleFilterHistoryTripOutput
            {
                TripId = key,
                LicensePlate = last.LicensePlate,
                Type = last.Type,
                FromTime = first.DetectTime,
                ToTime = last.DetectTime,
                HitCount = rows.Count,
                Direction = first.Direction,
                LocationId = first.LocationId,
                ZoneId = first.ZoneId,
                // Lộ trình: camera theo thời gian, gộp các lượt liền nhau cùng camera.
                EquipmentIds = rows
                    .Select(r => r.EquipmentId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Aggregate(new List<string>(), (route, id) =>
                    {
                        if (route.Count == 0 || !string.Equals(route[^1], id, StringComparison.OrdinalIgnoreCase)) route.Add(id!);
                        return route;
                    }),
            });
        }

        result.Records = records;
        return result;
    }

    private sealed class TripKeyRow
    {
        public string Key { get; set; } = "";
        public DateTime? LastTime { get; set; }
    }
}
