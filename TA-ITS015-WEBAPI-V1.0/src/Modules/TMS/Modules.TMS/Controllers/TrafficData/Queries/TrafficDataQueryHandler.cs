using Modules.TMS.Controllers.TrafficData.Dto;
using Modules.TMS.Controllers.Zone.Dto;
using Shared.Utility.Counting;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.TrafficData;
using Shared.DTO.Dtos.System;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using Shared.Utility.Utilities;

namespace Modules.TMS.Controllers.TrafficData.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 12/11/2024
    /// </summary>
    public class TrafficDataQueryHandler : IWolverineHandler
    {
        private IMessageBus _messageBus;
        private BaseRepository<TmsTrafficData> _trafficDataRepo;
        private List<SysConfigDataResponse> _configDataList;

        /// <summary>Cấu hình vận hành (VdsConfig) — dùng tra ngưỡng tốc độ theo hướng.</summary>
        private List<SysOpConfigResponse> _sysOpConfigList;
        private TrafficDataToDayChartService _dayChartService;
        private TrafficDataSpeedChartService _speedChartService;
        private TrafficDataService _trafficDataService;
        private BaseRepository<Modules.VDS.Core.Entities.VdsConfig> _vdsConfigRepo;

        public TrafficDataQueryHandler(IMessageBus messageBus, BaseRepository<TmsTrafficData> trafficDataRepo, TrafficDataToDayChartService dayChartService, TrafficDataService trafficDataService, TrafficDataSpeedChartService speedChartService, BaseRepository<Modules.VDS.Core.Entities.VdsConfig> vdsConfigRepo)
        {
            this._trafficDataRepo = trafficDataRepo;
            this._vdsConfigRepo = vdsConfigRepo;
            this._dayChartService = dayChartService;
            this._trafficDataService = trafficDataService;
            this._messageBus = messageBus;
            this._speedChartService = speedChartService;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 12/11/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageTmsTrafficDataOutput>> HandleAsync(PageTmsTrafficDataInput command)
        {
            var tdList = await _trafficDataRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Type), td => td.Type.Contains(command.Type))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Zone), td => td.ZoneId.Contains(command.Zone))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Lane), td => td.Lane.Contains(command.Lane))
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), td => td.LicensePlate.Contains(command.LicensePlate))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), td => td.Direction.Contains(command.Direction))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), td => td.EquipmentId.Contains(command.EquipmentId))
                .WhereIF(command.FromDate != null, td => td.DetectTime >= command.FromDate)
                .WhereIF(command.ToDate != null, td => td.DetectTime <= command.ToDate)
                .Select(td => new PageTmsTrafficDataOutput() { }, true)
                .OrderBuilder(command)
                .ToPagedListAsync(command.Page, command.PageSize);

            return tdList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 12/11/2024
        /// </summary>
        public async Task<List<TrafficDataOutput>> HandleAsync(TrafficDataInput command)
        {
            try
            {
                // Lượt xe bình thường chỉ còn MỘT tên "Normal" (bỏ tên cũ "Regular" ngày 22/09/2026). Gán ra
                // biến cục bộ để SqlSugar dịch được thành tham số thay vì phải đọc hằng tĩnh trong biểu thức.
                var normalType = TrafficCountFilter.NormalEventType;

                var dtList = await this._trafficDataRepo.AsQueryable()
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Type), td => td.Type.Contains(command.Type))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Zone), td => td.ZoneId.Contains(command.Zone))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Lane), td => td.Lane.Contains(command.Lane))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate),
                        td => td.LicensePlate.Contains(command.LicensePlate))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Direction),
                        td => td.Direction.Contains(command.Direction))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId),
                        td => td.EquipmentId.Contains(command.EquipmentId))
                    .WhereIF(command.FromDate != null, td => td.DetectTime >= command.FromDate)
                    .WhereIF(command.ToDate != null, td => td.DetectTime <= command.ToDate)
                    .WhereIF(command.IsCount == true, td => td.EventType == normalType)
                    .Select(td => new TrafficDataOutput() { }, true)
                    .ToListAsync();

                return dtList
                    .OrderByDescending(td => td.DetectTime).ToList()
                        ;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy traffic-data 60 phút trước đó tính từ thời điểm hiện tại
        /// Created date: 25/12/2024
        /// </summary>
        public async Task<List<TmsTrafficDataVolume>> HandleAsync(TmsTrafficDataHourVolumeInput command)
        {
            await this.LoadDefaultData();

            var vehicleTypeList = this._configDataList?
                .Where(config => config.ConfigTypeCode.Equals("vehicle_type", StringComparison.OrdinalIgnoreCase))
                .OrderBy(config => config.OrderNo)
                .ToList();

            var tdList = await this._trafficDataRepo.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Zone), td => td.ZoneId == command.Zone)
                .Where(td => td.DetectTime >= DateTime.Now.AddMinutes(-60) && td.DetectTime <= DateTime.Now)
                .Select(td => new TrafficDataOutput() { }, true)
                .ToListAsync();

            var dataList = this._trafficDataService.BuildTrafficDataVolumeList(tdList, vehicleTypeList);

            return dataList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy traffic-data 24 giờ trước đó tính từ thời điểm hiện tại
        /// Created date: 25/12/2024
        /// </summary>
        public async Task<List<TmsTrafficDataVolume>> HandleAsync(TmsTrafficDataDayVolumeInput command)
        {
            await this.LoadDefaultData();

            var vehicleTypeList = this._configDataList?
                .Where(config => config.ConfigTypeCode.Equals("vehicle_type", StringComparison.OrdinalIgnoreCase))
                .OrderBy(config => config.OrderNo)
                .ToList();

            var tdList = await this._trafficDataRepo.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Zone), td => td.ZoneId == command.Zone)
                .Where(td => td.DetectTime >= DateTime.Now.AddHours(-24) && td.DetectTime <= DateTime.Now)
                .Select(td => new TrafficDataOutput() { }, true)
                .ToListAsync();

            var dataList = this._trafficDataService.BuildTrafficDataVolumeList(tdList, vehicleTypeList);

            return dataList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 12/11/2024
        /// Description: Biểu đồ số lượng phương tiện của ngày hiện tại
        /// </summary>
        public async Task<TmsTrafficDataToDayChartOutput> HandleAsync(TmsTrafficDataToDayChartInput command)
        {
            await this.LoadDefaultData();

            var chart = new TmsTrafficDataToDayChartOutput();

            var vehicleTypeList = this._configDataList?
                .Where(config => config.ConfigTypeCode.Equals("vehicle_type", StringComparison.OrdinalIgnoreCase))
                .OrderBy(config => config.OrderNo)
                .ToList();

            List<int> hourList = new List<int>();
            for (int i = 0; i <= DateTime.Now.Hour; i++)
                hourList.Add(i);

            var tdList = await this._trafficDataRepo.AsQueryable()
                    .WhereIF(!string.IsNullOrEmpty(command.Zone), td => td.ZoneId == command.Zone)
                    .Where(td => td.DetectTime != null && td.DetectTime.Value.Date == DateTime.Now.Date)
                    .Select(td => new TrafficDataOutput() { }, true)
                    .ToListAsync();

            chart = this._dayChartService.BuildChart(tdList, vehicleTypeList, hourList);

            return chart;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Biểu đồ vận tốc trung bình hiện tại
        /// Created date: 12/11/2024
        ///
        /// Cutover 04/09/2026: số lấy từ <c>TmsZoneStatus</c> (worker VDS ghi) + ngưỡng từ <c>VdsConfig</c>,
        /// thay cho <c>Random</c> của bản cũ.
        /// </summary>
        public async Task<TmsTrafficDataAverage> HandleAsync(TmsTrafficAverageChartInput command)
        {
            await this.LoadDefaultData();

            var zoneList = await this._messageBus.InvokeAsync<List<ZoneOutput>>(new ZoneInput() { });
            return this._speedChartService.BuildTrafficDataAverage(
                zoneList, this._sysOpConfigList, command.Zone, command.Direction, BaseUtil.TimeServer.GetTimeServer());
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Load dữ liệu mặc định
        /// Created date: 12/11/2024
        /// </summary>
        private async Task LoadDefaultData()
        {
            try
            {
                var configDataListQuery = new SysConfigDataListRequest() { };
                var configDataListResponse = await BaseApiClient<IConfigDataApi>.Client().List(configDataListQuery);
                this._configDataList = configDataListResponse.Content?.Result;
            }
            catch { }

            try
            {
                // Cấu hình vận hành: dùng để tra ngưỡng tốc độ vds_route_{hướng}_max_speed cho biểu đồ
                // vận tốc trung bình. Lỗi ⇒ để null, service tự trả SpeedLimit = 0 (fail-closed).
                var rows = await _vdsConfigRepo.AsQueryable().Where(x => x.IsDelete == null && x.GroupCode == "vds_config"
                    && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable).ToListAsync();
                this._sysOpConfigList = rows.Adapt<List<SysOpConfigResponse>>();
            }
            catch { }
        }
    }
}
