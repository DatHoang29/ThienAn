using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Infrastructure.Services.Persistence.Quest;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Controllers.TrafficData.Dto;
using Modules.VDS.Controllers.TrafficData.Validators;
using Modules.VDS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;
using Shared.Utility.Counting;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Controllers.TrafficData.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Query handler cho TrafficData (QuestDB)
    /// Created date: 24/08/2026
    /// </summary>
    public class TrafficDataQueryHandler : IWolverineHandler
    {
        private readonly VdsRepository<VdsTrafficData> _repo;
        private readonly Modules.VDS.Infrastructure.Services.TrafficData.VdsTrafficDataService _trafficData;
        private readonly Modules.VDS.Infrastructure.Services.TrafficData.VdsRecordReaderService _records;
        private readonly Modules.VDS.Infrastructure.Services.TrafficData.VdsVolumeBreakdownReader _volumeBreakdown;
        private readonly IStringLocalizer _localizer;

        public async Task<Shared.Utility.Helpers.EChartHelper.EChartResponse> HandleAsync(TrafficDataToDayChartInput command)
        {
            var now = DateTime.UtcNow;
            var from = BaseUtil.TimeServer.GetLocalDayUtcRange(now).FromUtc;
            var rows = await _trafficData.VolumeByHourAsync(new Modules.VDS.Infrastructure.Services.TrafficData.Sql.VolumeQueryBuilder.VolumeFilter {
                FromUtc = from, ToUtc = now, ZoneId = command.ZoneId, EquipmentId = command.EquipmentId, Direction = command.Direction
            }, true);
            var slices = rows.Select(r => new Shared.Utility.Counting.TrafficCountSlice(
                new Shared.Utility.Counting.TrafficCountScope(TrafficCountScopes.Zone, r.Zone ?? "", r.Direction), r.BucketTime, r.BucketTime.AddHours(1),
                r.VehicleSize == "short" ? r.VehicleCount : 0, r.VehicleSize == "medium" ? r.VehicleCount : 0,
                r.VehicleSize == "long" ? r.VehicleCount : 0,
                r.VehicleSize is not ("short" or "medium" or "long") ? r.VehicleCount : 0,
                null, null, null, 0, r.Zone));
            return Shared.Utility.Counting.TrafficCountChartBuilder.Build(Shared.Utility.Counting.TrafficCountMath.ByHour(slices, from, now));
        }

        public TrafficDataQueryHandler(
            VdsRepository<VdsTrafficData> repo,
            Modules.VDS.Infrastructure.Services.TrafficData.VdsTrafficDataService trafficData,
            Modules.VDS.Infrastructure.Services.TrafficData.VdsRecordReaderService records,
            Modules.VDS.Infrastructure.Services.TrafficData.VdsVolumeBreakdownReader volumeBreakdown,
            IStringLocalizer localizer)
        {
            _repo = repo;
            _trafficData = trafficData;
            _records = records;
            _volumeBreakdown = volumeBreakdown;
            _localizer = localizer;
        }

        /// <summary>
        /// Chi tiết một bản tin thô theo cặp eventId + detectTime.
        ///
        /// <para>Trước 22/09/2026 action <c>Detail</c> tự inject <c>VdsRecordReaderService</c> qua
        /// <c>[FromServices]</c> và trả THẲNG entity <c>VdsTrafficData</c> — là action duy nhất của module
        /// không đi qua message bus và công khai cấu trúc lưu trữ.</para>
        /// </summary>
        public async Task<TrafficDataOutput> HandleAsync(TrafficDataDetailInput command)
        {
            var row = await _records.ReadAsync(command.EventId, command.DetectTime);
            return new TrafficDataOutput
            {
                DetectTime = row.DetectTime, VendorEventId = row.VendorEventId, CameraIp = row.CameraIp,
                EquipmentId = row.EquipmentId, Direction = row.Direction, ZoneId = row.ZoneId, Lane = row.Lane,
                EventType = row.EventType, AidEventType = row.AidEventType, LicensePlate = row.LicensePlate, VehicleType = row.VehicleType,
                VehicleColor = row.VehicleColor, VehicleSize = row.VehicleSize, Speed = row.Speed,
                LengthMm = row.LengthMm, KmNumber = row.KmNumber, MetNumber = row.MetNumber,
                SpeedMin = row.SpeedMin, SpeedMax = row.SpeedMax, PlateColor = row.PlateColor
            };
        }

        /// <summary>Phân trang danh sách traffic data.</summary>
        public async Task<SqlSugarPagedList<PageTrafficDataOutput>> HandleAsync(PageTrafficDataInput command)
        {
            command.FromDate = BaseUtil.TimeServer.ToUtc(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.ToUtc(command.ToDate);
            return await _repo.GuardAsync(questDb => questDb.Queryable<VdsTrafficData>()
                .Where(x => x.IsDelete == null)
                .WhereIF(command.FromDate != null, x => x.DetectTime >= command.FromDate)
                .WhereIF(command.ToDate != null, x => x.DetectTime < command.ToDate)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventType), x => x.EventType == command.EventType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Lane), x => x.Lane == command.Lane)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), x => x.Direction == command.Direction)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.VehicleSize), x => x.VehicleSize == command.VehicleSize)
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), x => x.LicensePlate!.Contains(command.LicensePlate!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.VehicleType), x => x.VehicleType == command.VehicleType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.VehicleColor), x => x.VehicleColor == command.VehicleColor)
                .WhereIF(command.LengthFrom != null, x => x.LengthMm >= command.LengthFrom)
                .WhereIF(command.LengthTo != null, x => x.LengthMm <= command.LengthTo)
                .WhereIF(command.SpeedFrom != null, x => x.Speed >= command.SpeedFrom)
                .WhereIF(command.SpeedTo != null, x => x.Speed <= command.SpeedTo)
                .WhereIF(command.EventsOnly, x => x.EventType != TrafficCountFilter.NormalEventType)
                .Select(x => new PageTrafficDataOutput
                {
                    DetectTime = x.DetectTime,
                    VendorEventId = x.VendorEventId,
                    CameraIp = x.CameraIp,
                    EquipmentId = x.EquipmentId,
                    Direction = x.Direction,
                    ZoneId = x.ZoneId,
                    Lane = x.Lane,
                    EventType = x.EventType,
                    AidEventType = x.AidEventType,
                    LicensePlate = x.LicensePlate,
                    VehicleType = x.VehicleType,
                    VehicleColor = x.VehicleColor,
                    VehicleSize = x.VehicleSize,
                    Speed = x.Speed,
                    LengthMm = x.LengthMm,
                    KmNumber = x.KmNumber,
                    MetNumber = x.MetNumber,
                    SpeedMin = x.SpeedMin,
                    SpeedMax = x.SpeedMax,
                    PlateColor = x.PlateColor,
                })
                .OrderBy(x => x.DetectTime, OrderByType.Desc).OrderBy(x => x.VendorEventId, OrderByType.Desc)
                .ToPagedListAsync(command.Page, command.PageSize));
        }

        /// <summary>Danh sách traffic data (không phân trang).</summary>
        public async Task<List<TrafficDataOutput>> HandleAsync(TrafficDataListInput command)
        {
            command.FromDate = BaseUtil.TimeServer.ToUtc(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.ToUtc(command.ToDate);
            var rows = await _repo.GuardAsync(questDb => questDb.Queryable<VdsTrafficData>()
                .Where(x => x.IsDelete == null)
                .WhereIF(command.FromDate != null, x => x.DetectTime >= command.FromDate)
                .WhereIF(command.ToDate != null, x => x.DetectTime < command.ToDate)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventType), x => x.EventType == command.EventType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Lane), x => x.Lane == command.Lane)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), x => x.Direction == command.Direction)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.VehicleSize), x => x.VehicleSize == command.VehicleSize)
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), x => x.LicensePlate!.Contains(command.LicensePlate!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.VehicleType), x => x.VehicleType == command.VehicleType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.VehicleColor), x => x.VehicleColor == command.VehicleColor)
                .WhereIF(command.LengthFrom != null, x => x.LengthMm >= command.LengthFrom)
                .WhereIF(command.LengthTo != null, x => x.LengthMm <= command.LengthTo)
                .WhereIF(command.SpeedFrom != null, x => x.Speed >= command.SpeedFrom)
                .WhereIF(command.SpeedTo != null, x => x.Speed <= command.SpeedTo)
                .WhereIF(command.EventsOnly, x => x.EventType != TrafficCountFilter.NormalEventType)
                .Select(x => new TrafficDataOutput
                {
                    DetectTime = x.DetectTime,
                    VendorEventId = x.VendorEventId,
                    CameraIp = x.CameraIp,
                    EquipmentId = x.EquipmentId,
                    Direction = x.Direction,
                    ZoneId = x.ZoneId,
                    Lane = x.Lane,
                    EventType = x.EventType,
                    AidEventType = x.AidEventType,
                    LicensePlate = x.LicensePlate,
                    VehicleType = x.VehicleType,
                    VehicleColor = x.VehicleColor,
                    VehicleSize = x.VehicleSize,
                    Speed = x.Speed,
                    LengthMm = x.LengthMm,
                    KmNumber = x.KmNumber,
                    MetNumber = x.MetNumber,
                    SpeedMin = x.SpeedMin,
                    SpeedMax = x.SpeedMax,
                    PlateColor = x.PlateColor,
                })
                .OrderBy(x => x.DetectTime, OrderByType.Desc)
                .Take(10001).ToListAsync());
            if (rows.Count > 10000) throw Oops.Oh(BaseMsg.Vds.Exception.ResultTooLarge);
            return rows;
        }

        /// <summary>Lưu lượng xe theo giờ (SAMPLE BY 1h qua SQL thô — LINQ .SampleBy sinh SAI).</summary>
        public async Task<List<TrafficDataVolumeOutput>> HandleAsync(TrafficDataHourVolumeInput command)
        {
            // FE gửi UTC+0 ⇒ convert về giờ server trước khi dựng mốc. Validator đã bắt buộc có From/To.
            var filter = new Modules.VDS.Infrastructure.Services.TrafficData.Sql.VolumeQueryBuilder.VolumeFilter
            {
                FromUtc = BaseUtil.TimeServer.ToUtc(command.FromDate) ?? command.FromDate!.Value,
                ToUtc = BaseUtil.TimeServer.ToUtc(command.ToDate) ?? command.ToDate!.Value,
                ZoneId = command.ZoneId,
                Direction = command.Direction,
                Lane = command.Lane,
                EquipmentId = command.EquipmentId,
            };
            return await _trafficData.VolumeByHourAsync(filter, includeHour: true);
        }

        /// <summary>Lưu lượng xe theo ngày (SAMPLE BY 1d qua SQL thô — LINQ .SampleBy sinh SAI).</summary>
        public async Task<List<TrafficDataVolumeOutput>> HandleAsync(TrafficDataDayVolumeInput command)
        {
            var filter = new Modules.VDS.Infrastructure.Services.TrafficData.Sql.VolumeQueryBuilder.VolumeFilter
            {
                FromUtc = BaseUtil.TimeServer.ToUtc(command.FromDate) ?? command.FromDate!.Value,
                ToUtc = BaseUtil.TimeServer.ToUtc(command.ToDate) ?? command.ToDate!.Value,
                ZoneId = command.ZoneId,
                Direction = command.Direction,
                Lane = command.Lane,
                EquipmentId = command.EquipmentId,
            };
            return await _trafficData.VolumeByDayAsync(filter);
        }

        /// <summary>
        /// Đếm xe theo giờ tách direction × vehicleSize — cho report ngày/tháng.
        ///
        /// <para>Đi qua <c>VdsVolumeBreakdownReader</c> chứ không gọi thẳng service QuestDB: đó là nơi giữ
        /// chính sách lùi về <c>TmsTrafficSummary</c> khi QuestDB không kết nối được (dời từ feature
        /// <c>Report</c> đã xoá, 25/09/2026).</para>
        /// </summary>
        public Task<List<Shared.DTO.Dtos.Vds.VdsTrafficVolumeBreakdownRow>> HandleAsync(
            TrafficDataVolumeBreakdownInput command)
        {
            var from = BaseUtil.TimeServer.ToUtc(command.FromDate) ?? command.FromDate!.Value;
            var to = BaseUtil.TimeServer.ToUtc(command.ToDate) ?? command.ToDate!.Value;
            return _volumeBreakdown.ReadAsync(from, to, command.CountAllEventTypes);
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Đường gọi IN-PROCESS từ TMS.Report (<c>TrafficDataDayService</c>,
        /// <c>TrafficDataMonthService</c>) qua <c>IMessageBus.InvokeAsync</c> — thay Refit
        /// <c>IVdsTrafficDataApi.VolumeBreakdown</c> (đã xoá).
        ///
        /// <para>Map request Shared.DTO sang <see cref="TrafficDataVolumeBreakdownInput"/> rồi chạy ĐÚNG logic
        /// của endpoint HTTP <c>VolumeBreakdown</c> — gồm cả chính sách lùi về <c>TmsTrafficSummary</c> của
        /// <c>VdsVolumeBreakdownReader</c>. Đường message bus không qua MVC nên tự chạy
        /// <see cref="TrafficDataVolumeBreakdownValidator"/> (bắt buộc From/To, To &gt; From, trần 60 ngày)
        /// như đường HTTP.</para>
        /// Created date: 13/10/2026
        /// </summary>
        public Task<List<Shared.DTO.Dtos.Vds.VdsTrafficVolumeBreakdownRow>> HandleAsync(
            Shared.DTO.Dtos.Vds.VdsTrafficVolumeBreakdownRequest command)
        {
            var input = new TrafficDataVolumeBreakdownInput
            {
                FromDate = command.FromDate,
                ToDate = command.ToDate,
                CountAllEventTypes = command.CountAllEventTypes,
            };

            new TrafficDataVolumeBreakdownValidator(_localizer).ValidateAndThrow(input);

            return HandleAsync(input);
        }

        /// <summary>
        /// Mã loại sự kiện LƯU TRỮ của xe vượt ngưỡng tối đa: <c>TmsEventType.Code</c> ánh xạ từ AID <c>Speeding</c>
        /// (worker lưu mã TMS từ 19/10/2026).
        /// </summary>
        private const string SpeedingEventType = "speeding";

        /// <summary>
        /// Tên AID cũ — chỉ để môi trường chưa chạy script migrate QuestDB 19/10/2026 vẫn ra đủ dữ liệu cũ.
        /// QuestDB so SYMBOL phân biệt hoa thường nên phải liệt kê cả hai.
        /// </summary>
        private const string LegacySpeedingEventType = "Speeding";

        /// <summary>Trần số lượt trả về cho báo cáo vượt tốc độ — cùng trần với danh sách thô.</summary>
        private const int OverSpeedMaxRows = 10000;

        /// <summary>
        /// Author: KienLT
        /// Description: Đường gọi IN-PROCESS từ TMS.Report (<c>TmsOverSpeedReportService</c>): các lượt xe camera gắn cờ
        /// <c>Speeding</c> trong cửa sổ.
        ///
        /// <para>Lọc theo cờ của camera chứ không tự so <c>Speed</c> với ngưỡng: camera đánh giá theo ngưỡng TẠI THỜI
        /// ĐIỂM xe đi qua, còn ngưỡng hiện hành có thể đã đổi — so lại sẽ đánh giá sai dữ liệu cũ.</para>
        ///
        /// <para>Vượt trần thì ném <c>ResultTooLarge</c> (lỗi đã bản địa hoá, đi xuyên qua <c>InvokeVdsAsync</c>) thay vì
        /// cắt ngầm: báo cáo thiếu dòng mà không ai biết còn tệ hơn bắt người dùng thu hẹp bộ lọc.</para>
        /// Created date: 16/10/2026
        /// </summary>
        public async Task<List<Shared.DTO.Dtos.Vds.VdsOverSpeedRow>> HandleAsync(
            Shared.DTO.Dtos.Vds.VdsOverSpeedRequest command)
        {
            new TrafficDataOverSpeedValidator(_localizer).ValidateAndThrow(command);

            var from = BaseUtil.TimeServer.ToUtc(command.FromDate);
            var to = BaseUtil.TimeServer.ToUtc(command.ToDate);
            var equipmentIds = command.EquipmentIds;

            var rows = await _repo.GuardAsync(questDb => questDb.Queryable<VdsTrafficData>()
                .Where(x => x.IsDelete == null)
                .Where(x => x.DetectTime >= from && x.DetectTime < to)
                .Where(x => x.EventType == SpeedingEventType || x.EventType == LegacySpeedingEventType)
                .WhereIF(equipmentIds.Length > 0, x => equipmentIds.Contains(x.EquipmentId!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), x => x.Direction == command.Direction)
                .WhereIF(!string.IsNullOrWhiteSpace(command.VehicleSize), x => x.VehicleSize == command.VehicleSize)
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), x => x.LicensePlate!.Contains(command.LicensePlate!))
                .OrderBy(x => x.DetectTime, OrderByType.Desc)
                .Take(OverSpeedMaxRows + 1).ToListAsync());
            if (rows.Count > OverSpeedMaxRows) throw Oops.Oh(BaseMsg.Vds.Exception.ResultTooLarge);

            return rows.Select(x => new Shared.DTO.Dtos.Vds.VdsOverSpeedRow
            {
                DetectTime = BaseUtil.TimeServer.ToUtc(x.DetectTime),
                VendorEventId = x.VendorEventId,
                EquipmentId = x.EquipmentId,
                ZoneId = x.ZoneId,
                Direction = x.Direction,
                Lane = x.Lane,
                LicensePlate = x.LicensePlate,
                VehicleType = x.VehicleType,
                VehicleColor = x.VehicleColor,
                VehicleSize = x.VehicleSize,
                Speed = x.Speed,
                KmNumber = x.KmNumber,
                MetNumber = x.MetNumber,
                SpeedMax = x.SpeedMax,
            }).ToList();
        }

        /// <summary>Tốc độ trung bình và phân bổ cỡ xe.</summary>
        public async Task<TrafficDataAverageOutput> HandleAsync(TrafficDataAverageInput command)
        {
            command.FromDate = BaseUtil.TimeServer.ToUtc(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.ToUtc(command.ToDate);
            var result = await _repo.GuardAsync(questDb => questDb.Queryable<VdsTrafficData>()
                .Where(x => x.EventType == TrafficCountFilter.NormalEventType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), x => x.ZoneId == command.ZoneId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), x => x.Direction == command.Direction)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                .Where(x => x.IsDelete == null)
                .WhereIF(command.FromDate != null, x => x.DetectTime >= command.FromDate)
                .WhereIF(command.ToDate != null, x => x.DetectTime < command.ToDate)
                .Select(x => new TrafficDataAverageOutput
                {
                    Zone = command.ZoneId,
                    Direction = command.Direction,
                    AvgSpeed = SqlFunc.AggregateAvg(x.Speed),
                    TotalVehicle = SqlFunc.AggregateCount(x.VendorEventId),
                    ShortVehicle = SqlFunc.AggregateSum(x.VehicleSize == "short" ? 1 : 0),
                    MediumVehicle = SqlFunc.AggregateSum(x.VehicleSize == "medium" ? 1 : 0),
                    LongVehicle = SqlFunc.AggregateSum(x.VehicleSize == "long" ? 1 : 0),
                })
                .FirstAsync());

            return result ?? new TrafficDataAverageOutput { Zone = command.ZoneId, Direction = command.Direction };
        }
    }
}
