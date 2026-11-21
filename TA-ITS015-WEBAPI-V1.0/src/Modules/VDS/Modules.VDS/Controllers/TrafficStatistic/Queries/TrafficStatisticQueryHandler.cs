using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Controllers.TrafficStatistic.Dto;
using Modules.VDS.Controllers.TrafficStatistic.Validators;
using Modules.VDS.Infrastructure.Services.TrafficStatistic;
using Shared.DTO.Dtos.Vds;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Controllers.TrafficStatistic.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Query handler cho số liệu thống kê chu kỳ (QuestDB). Thay bốn endpoint ĐỌC của
    /// <c>tmsTrafficStatistic</c> (Page/GetList/GetByEquipment/ListByDate). Bốn endpoint GHI
    /// (Add/Update/Delete/BatchDelete) KHÔNG chuyển: worker là nguồn ghi, QuestDB dedupe bằng UPSERT KEYS.
    ///
    /// Mọi truy vấn có filter <c>fromTime</c> bắt buộc (validator) và bọc qua cầu dao. FE gửi UTC+0 nên
    /// convert qua <c>BaseUtil.TimeServer.GetTimeServer</c> trước khi dựng mốc.
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficStatisticQueryHandler : IWolverineHandler
    {
        private readonly VdsTrafficStatisticService _repo;
        private readonly IStringLocalizer _localizer;

        public TrafficStatisticQueryHandler(VdsTrafficStatisticService repo, IStringLocalizer localizer)
        {
            _repo = repo;
            _localizer = localizer;
        }

        /// <summary>Phân trang thống kê trong cửa sổ thời gian.</summary>
        public Task<SqlSugarPagedList<TrafficStatisticOutput>> HandleAsync(PageTrafficStatisticInput command)
        {
            var from = BaseUtil.TimeServer.ToUtc(command.FromDate) ?? command.FromDate!.Value;
            var to = BaseUtil.TimeServer.ToUtc(command.ToDate) ?? command.ToDate!.Value;

            return _repo.GuardAsync(questDb => questDb.Queryable<VdsTrafficStatistic>()
                .Where(x => x.FromTime >= from && x.FromTime < to)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), x => x.Direction == command.Direction)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Lane), x => x.Lane == command.Lane)
                .Select(x => new TrafficStatisticOutput
                {
                    FromTime = x.FromTime,
                    ToTime = x.ToTime,
                    PeriodMinutes = x.PeriodMinutes,
                    VendorEventId = x.VendorEventId,
                    CameraIp = x.CameraIp,
                    EquipmentId = x.EquipmentId,
                    Direction = x.Direction,
                    ZoneId = x.ZoneId,
                    Lane = x.Lane,
                    TotalVehicle = x.TotalVehicle,
                    ShortVehicle = x.ShortVehicle,
                    MediumVehicle = x.MediumVehicle,
                    LongVehicle = x.LongVehicle,
                    OtherVehicle = x.OtherVehicle,
                    AvgSpeed = x.AvgSpeed,
                    SpaceMeanKmh = x.SpaceMeanKmh,
                    FlowVph = x.FlowVph,
                    DensityVpkm = x.DensityVpkm,
                    SpaceOccupy = x.SpaceOccupy,
                    TimeOccupy = x.TimeOccupy,
                    SpaceHeadway = x.SpaceHeadway,
                    TimeHeadway = x.TimeHeadway,
                    KmNumber = x.KmNumber,
                    MetNumber = x.MetNumber,
                })
                .OrderBy(x => x.FromTime, OrderByType.Desc)
                .ToPagedListAsync(command.Page, command.PageSize));
        }

        /// <summary>Danh sách thống kê trong cửa sổ thời gian.</summary>
        public Task<List<TrafficStatisticOutput>> HandleAsync(TrafficStatisticListInput command)
        {
            var from = BaseUtil.TimeServer.ToUtc(command.FromDate) ?? command.FromDate!.Value;
            var to = BaseUtil.TimeServer.ToUtc(command.ToDate) ?? command.ToDate!.Value;

            return _repo.GuardAsync(async questDb =>
            {
                // Lấy ENTITY rồi map trong RAM: SqlSugar KHÔNG dịch được MapOutput(x) trong .Select
                // (báo "MapOutput(x) no support"). Số dòng thống kê trong một cửa sổ nhỏ nên map RAM an toàn.
                var rows = await questDb.Queryable<VdsTrafficStatistic>()
                    .Where(x => x.FromTime >= from && x.FromTime < to)
                    .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                    .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), x => x.Direction == command.Direction)
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Lane), x => x.Lane == command.Lane)
                    .OrderBy(x => x.FromTime, OrderByType.Desc)
                    .ToListAsync();

                return rows.Select(MapOutput).ToList();
            });
        }

        /// <summary>Thống kê theo khoảng ngày — cho 2 report FastReport (thay ListByDate của TMS).</summary>
        public Task<List<TrafficStatisticOutput>> HandleAsync(TrafficStatisticByDateInput command)
        {
            var from = BaseUtil.TimeServer.ToUtc(command.FromDate) ?? command.FromDate!.Value;
            var to = BaseUtil.TimeServer.ToUtc(command.ToDate) ?? command.ToDate!.Value;

            return _repo.GuardAsync(async questDb =>
            {
                var rows = await questDb.Queryable<VdsTrafficStatistic>()
                    .Where(x => x.FromTime >= from && x.FromTime < to)
                    .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
                    .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                    .OrderBy(x => x.FromTime, OrderByType.Desc)
                    .ToListAsync();

                return rows.Select(MapOutput).ToList();
            });
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Đường gọi IN-PROCESS từ TMS.Report (<c>TrafficStatisticSectionReportService</c>,
        /// <c>TrafficStatisticZoneReportService</c>) qua <c>IMessageBus.InvokeAsync</c> — thay Refit
        /// <c>IVdsTrafficStatisticApi.ListByDate</c> (đã xoá).
        ///
        /// <para>Map request Shared.DTO sang <see cref="TrafficStatisticByDateInput"/>, tự chạy
        /// <see cref="TrafficStatisticByDateValidator"/> (đường message bus không qua FluentValidation của MVC)
        /// rồi chạy ĐÚNG logic của endpoint HTTP <c>ListByDate</c>.</para>
        ///
        /// <para><b>Giữ Kind như đường HTTP.</b> <see cref="TrafficStatisticOutput"/> gắn
        /// <c>UtcDateTimeConverter</c> trên <c>FromTime</c>/<c>ToTime</c> nên bên TMS trước đây nhận giá trị
        /// UTC có hậu tố Z (Kind = Utc). In-process không qua JSON nên map bằng <c>ToUtc</c> để TMS nhận đúng
        /// Kind đó.</para>
        /// Created date: 13/10/2026
        /// </summary>
        public async Task<List<VdsTrafficStatisticResponse>> HandleAsync(VdsTrafficStatisticRequest command)
        {
            var input = new TrafficStatisticByDateInput
            {
                FromDate = command.FromDate,
                ToDate = command.ToDate,
                ZoneId = command.ZoneId,
                EquipmentId = command.EquipmentId,
            };

            new TrafficStatisticByDateValidator(_localizer).ValidateAndThrow(input);

            var rows = await HandleAsync(input);
            return rows.Select(MapResponse).ToList();
        }

        /// <summary>Bản ghi mới nhất của một thiết bị trong cửa sổ (thay GetByEquipment).</summary>
        public Task<TrafficStatisticOutput?> HandleAsync(TrafficStatisticByEquipmentInput command)
        {
            var from = BaseUtil.TimeServer.ToUtc(command.FromDate) ?? command.FromDate!.Value;
            var to = BaseUtil.TimeServer.ToUtc(command.ToDate) ?? command.ToDate!.Value;

            return _repo.GuardAsync(async questDb =>
            {
                var entity = await questDb.Queryable<VdsTrafficStatistic>()
                    .Where(x => x.FromTime >= from && x.FromTime < to)
                    .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                    .OrderBy(x => x.FromTime, OrderByType.Desc)
                    .FirstAsync();

                return entity is null ? null : MapOutput(entity);
            });
        }

        /// <summary>
        /// Snapshot statistic: một dòng mới nhất cho mỗi cặp (thiết bị, làn) trong cửa sổ, kèm cờ quá hạn.
        ///
        /// <para>Dùng chung cho cả <c>Snapshot</c> gọi một lần và <c>Stream</c> gọi lặp theo chu kỳ SSE. Hai
        /// ngưỡng chặn (cửa sổ ≤ 1 ngày, ≤ 100 thiết bị, ≤ 20000 dòng) là để một client SSE không kéo cả
        /// bảng về mỗi vài giây.</para>
        /// </summary>
        public async Task<StatisticSnapshotOutput> HandleAsync(StatisticSnapshotInput command, CancellationToken cancellationToken)
        {
            var (from, to) = command.Window();
            if (!string.IsNullOrWhiteSpace(command.LicensePlate)) throw Oops.Oh(BaseMsg.Vds.Exception.StatisticPlateNotSupported);
            if (to - from > TimeSpan.FromDays(1) || command.EquipmentIds.Length > 100)
                throw Oops.Oh(BaseMsg.Vds.Exception.StatisticSnapshotTooLarge);

            // KienLT - 30/10/2026: truyền token cho GuardAsync để huỷ request dừng cả bước đọc journal/chọn node/mở lease.
            var rows = await _repo.GuardAsync(questDb => questDb.Queryable<VdsTrafficStatistic>().Where(x => x.FromTime >= from && x.FromTime < to)
                .WhereIF(command.EquipmentIds.Length > 0, x => command.EquipmentIds.Contains(x.EquipmentId!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), x => x.Direction == command.Direction)
                .OrderBy(x => x.FromTime, OrderByType.Desc).Take(20001).ToListAsync(cancellationToken), cancellationToken);
            if (rows.Count > 20000) throw Oops.Oh(BaseMsg.Vds.Exception.StatisticResultTooLarge);

            var now = DateTime.UtcNow;
            return new StatisticSnapshotOutput
            {
                ComputedAt = now,
                Source = "VdsTrafficStatistic",
                Rows = rows.GroupBy(x => new { x.EquipmentId, x.Lane })
                    .Select(g =>
                    {
                        var row = g.OrderByDescending(x => x.FromTime).ThenByDescending(x => x.VendorEventId).First();
                        // Quá hạn = trễ hơn hai chu kỳ: một chu kỳ trống có thể chỉ là lệch nhịp ghi.
                        return new StatisticSnapshotRowOutput
                        {
                            Data = MapOutput(row),
                            Stale = now - BaseUtil.TimeServer.ToUtc(row.ToTime ?? row.FromTime)
                                > TimeSpan.FromMinutes(Math.Max(1, (int)(row.PeriodMinutes ?? 5)) * 2)
                        };
                    }).ToList()
            };
        }

        /// <summary>Map entity → output TRONG RAM (không nhúng vào .Select vì SqlSugar không dịch được).</summary>
        private static TrafficStatisticOutput MapOutput(VdsTrafficStatistic x) => new()
        {
            FromTime = x.FromTime,
            ToTime = x.ToTime,
            PeriodMinutes = x.PeriodMinutes,
            VendorEventId = x.VendorEventId,
            CameraIp = x.CameraIp,
            EquipmentId = x.EquipmentId,
            Direction = x.Direction,
            ZoneId = x.ZoneId,
            Lane = x.Lane,
            TotalVehicle = x.TotalVehicle,
            ShortVehicle = x.ShortVehicle,
            MediumVehicle = x.MediumVehicle,
            LongVehicle = x.LongVehicle,
            OtherVehicle = x.OtherVehicle,
            AvgSpeed = x.AvgSpeed,
            SpaceMeanKmh = x.SpaceMeanKmh,
            FlowVph = x.FlowVph,
            DensityVpkm = x.DensityVpkm,
            SpaceOccupy = x.SpaceOccupy,
            TimeOccupy = x.TimeOccupy,
            SpaceHeadway = x.SpaceHeadway,
            TimeHeadway = x.TimeHeadway,
            KmNumber = x.KmNumber,
            MetNumber = x.MetNumber,
        };

        /// <summary>
        /// Author: KienLT
        /// Description: Map output → response cross-module. <c>FromTime</c>/<c>ToTime</c> qua <c>ToUtc</c> để
        /// khớp giá trị bên gọi từng nhận qua JSON (<c>UtcDateTimeConverter</c> dán Kind Utc, không dịch giá trị).
        /// Created date: 13/10/2026
        /// </summary>
        private static VdsTrafficStatisticResponse MapResponse(TrafficStatisticOutput x) => new()
        {
            FromTime = BaseUtil.TimeServer.ToUtc(x.FromTime),
            ToTime = BaseUtil.TimeServer.ToUtc(x.ToTime),
            PeriodMinutes = x.PeriodMinutes,
            VendorEventId = x.VendorEventId,
            CameraIp = x.CameraIp,
            EquipmentId = x.EquipmentId,
            Direction = x.Direction,
            ZoneId = x.ZoneId,
            Lane = x.Lane,
            TotalVehicle = x.TotalVehicle,
            ShortVehicle = x.ShortVehicle,
            MediumVehicle = x.MediumVehicle,
            LongVehicle = x.LongVehicle,
            OtherVehicle = x.OtherVehicle,
            AvgSpeed = x.AvgSpeed,
            SpaceMeanKmh = x.SpaceMeanKmh,
            FlowVph = x.FlowVph,
            DensityVpkm = x.DensityVpkm,
            SpaceOccupy = x.SpaceOccupy,
            TimeOccupy = x.TimeOccupy,
            SpaceHeadway = x.SpaceHeadway,
            TimeHeadway = x.TimeHeadway,
            KmNumber = x.KmNumber,
            MetNumber = x.MetNumber,
        };
    }
}
