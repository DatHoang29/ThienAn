using Modules.VDS.Infrastructure.Services.Vehicle.Plates;
using Shared.Utility.Utilities;
using Modules.TMS.Core.Entities;
using Modules.VDS.Controllers.Journey.Dto;
using Modules.VDS.Infrastructure.Services.Journey.Keys;
using Modules.VDS.Core.Dto;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Infrastructure;
using Modules.VDS.Infrastructure.Services.Journey.Rules;
using Modules.VDS.Infrastructure.Services.Journey;
using System.Linq.Expressions;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VDS.Controllers.Journey.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler cho hành trình A-B: phân trang và chi tiết kèm các sự cố liên quan.
///
/// <para>Bảng <c>TmsTrafficJourney</c> nằm ở SQL Server (worker ghi sau khi ghép cặp biển số), nên đọc
/// qua <see cref="SqlRepository{T}"/>.</para>
/// Created date: 21/09/2026
/// </summary>
public class JourneyQueryHandler : IWolverineHandler
{
    private static readonly Expression<Func<TmsTrafficJourney, PageJourneyOutput>> JourneyProjection = x => new PageJourneyOutput
    {
        ID = x.ID, SegmentId = x.SegmentId, LicensePlate = x.LicensePlate,
        FromEquipmentId = x.FromEquipmentId, ToEquipmentId = x.ToEquipmentId,
        FromTime = x.FromTime, ToTime = x.ToTime, TravelSeconds = x.TravelSeconds,
        DistanceMeter = x.DistanceMeter, AvgSpeedKmh = x.AvgSpeedKmh, Type = x.Type,
        Direction = x.Direction, IsValid = x.IsValid, InvalidReason = x.InvalidReason,
        SpeedLimitKmh = x.SpeedLimitKmh, IsOverSpeed = x.IsOverSpeed
    };

    private readonly BaseRepository<TmsTrafficJourney> _repo;
    private readonly VdsJourneyTripService _trips;

    /// <summary>
    /// Author: KienLT
    /// Description: Khởi tạo truy vấn hành trình và service gom chuyến.
    /// Created date: 22/10/2026
    /// </summary>
    public JourneyQueryHandler(BaseRepository<TmsTrafficJourney> repo, VdsJourneyTripService trips)
    {
        _repo = repo;
        _trips = trips;
    }

    /// <summary>Phân trang hành trình theo cửa sổ thời gian và bộ lọc.</summary>
    public async Task<SqlSugarPagedList<PageJourneyOutput>> HandleAsync(JourneyInput command)
        => await (await FilteredAsync(command))
            .OrderBy(x => x.ToTime, OrderByType.Desc).OrderBy(x => x.ID, OrderByType.Desc)
            .Select(JourneyProjection)
            .ToPagedListAsync(command.Page, command.PageSize);

    /// <summary>
    /// Author: KienLT
    /// Description: Phân trang theo CHUYẾN: cùng bộ lọc với <c>Page</c> (lọc từng hành trình trước), rồi gom bằng
    /// <see cref="VdsJourneyTripRules"/> với vị trí camera/đoạn rẽ theo Topology và ngưỡng nghỉ <c>vds_journey_trip_gap</c>.
    /// <para>Đọc trọn các hành trình trong cửa sổ đã giới hạn (Window bắt buộc) vì một chuyến không được bị cắt ngang
    /// giữa hai trang; chỉ chiếu cột cần cho lưới.</para>
    /// Created date: 21/10/2026
    /// </summary>
    public async Task<SqlSugarPagedList<JourneyTripOutput>> HandleAsync(JourneyTripInput command)
    {
        var filter = command.Filter;
        var rows = await (await FilteredAsync(filter))
            .OrderBy(x => x.LicensePlate).OrderBy(x => x.FromTime).OrderBy(x => x.ID)
            .Select(JourneyProjection)
            .ToListAsync();

        return await _trips.BuildAsync(rows, filter);
    }

    /// <summary>Bộ lọc dùng chung cho <c>Page</c> và <c>TripPage</c>: cửa sổ thời gian bắt buộc + điều kiện tuỳ chọn.</summary>
    private async Task<ISugarQueryable<TmsTrafficJourney>> FilteredAsync(JourneyInput command)
    {
        var (from, to) = command.Window();
        if (command.SpeedFrom > command.SpeedTo) throw Oops.Oh(BaseMsg.Vds.Exception.SpeedFromGreaterThanTo);
        var plate = VdsPlate.Normalize(command.LicensePlate);
        // Lọc theo vùng phải quy về danh sách thiết bị: hành trình chỉ lưu camera đầu/cuối, không lưu zoneId.
        var zoneEquipment = string.IsNullOrWhiteSpace(command.ZoneId)
            ? new List<string>()
            : await _repo.Context.Queryable<TmsZoneEquipment>()
                .Where(x => x.IsDelete == null && x.ZoneId == command.ZoneId)
                .Select(x => x.EquipmentId!).ToListAsync();

        return _repo.AsQueryable()
            .Where(x => x.IsDelete == null && x.ToTime >= from && x.ToTime < to)
            .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => zoneEquipment.Contains(x.FromEquipmentId!) || zoneEquipment.Contains(x.ToEquipmentId!))
            .WhereIF(!string.IsNullOrWhiteSpace(command.SegmentId), x => x.SegmentId == command.SegmentId)
            .WhereIF(!string.IsNullOrWhiteSpace(plate), x => x.LicensePlate == plate)
            .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.FromEquipmentId == command.EquipmentId || x.ToEquipmentId == command.EquipmentId)
            .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), x => x.Direction == command.Direction)
            .WhereIF(command.SpeedFrom.HasValue, x => x.AvgSpeedKmh >= command.SpeedFrom)
            .WhereIF(command.SpeedTo.HasValue, x => x.AvgSpeedKmh <= command.SpeedTo)
            .WhereIF(command.IsValid.HasValue, x => x.IsValid == command.IsValid);
    }

    /// <summary>Chi tiết một hành trình kèm các sự cố sinh ra từ chính hành trình đó.</summary>
    public async Task<JourneyDetailOutput> HandleAsync(JourneyDetailInput command)
    {
        var row = await _repo.AsQueryable().Where(x => x.ID == command.ID && x.IsDelete == null).FirstAsync()
            ?? throw Oops.Oh(BaseMsg.Vds.Exception.JourneyNotFound);
        // Sự cố tốc độ trung bình được worker gắn EventId = khóa hành trình; không có FromTime thì không có khóa.
        var keys = row.FromTime.HasValue
            ? Modules.VDS.Core.Constants.VdsJourneyEventConst.LookupEventIds(row.SegmentId, row.LicensePlate, row.FromTime.Value)
            : Array.Empty<string>();
        var incidents = await _repo.Context.Queryable<TmsIncident>()
            .Where(x => x.IsDelete == null && x.TenantId == row.TenantId && keys.Contains(x.EventId!)).ToListAsync();

        return new JourneyDetailOutput
        {
            Journey = row.Adapt<JourneyOutput>(),
            Incidents = incidents.Select(x => x.Adapt<VdsLinkedIncidentOutput>()).ToList(),
            FromRecord = new JourneyRecordOutput { EquipmentId = row.FromEquipmentId, DetectTime = BaseUtil.TimeServer.ToUtc(row.FromTime) },
            ToRecord = new JourneyRecordOutput { EquipmentId = row.ToEquipmentId, DetectTime = BaseUtil.TimeServer.ToUtc(row.ToTime) }
        };
    }
}
