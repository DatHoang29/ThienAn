using Modules.FMS.Controllers.EquipmentHistory.Dto;
using Modules.FMS.Controllers.EquipmentHistoryChart.Dto;
using Modules.FMS.Core.Entities;
using Modules.FMS.Infrastructure;
using Modules.FMS.Infrastructure.Services.EquipmentHistoryChart;
using Shared.DTO.Dtos.System;
using Shared.DTO.Dtos.Tms;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using Shared.Utility.Apis.Tms;
using Wolverine;

namespace Modules.FMS.Controllers.EquipmentHistoryChart.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 22/01/2025
    /// </summary>
    public class EquipmentHistoryChartQueryHandler : IWolverineHandler
    {
        private List<SysConfigDataResponse> _configDataList = null;
        private List<TmsEquipmentResponse> _equipmentList = null;

        private BaseRepository<FmsEquipmentHistory> _baseRepository;

        public EquipmentHistoryChartQueryHandler(BaseRepository<FmsEquipmentHistory> baseRepository)
        {
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 22/01/2025
        /// Description: Biểu đồ lịch sử trạng thái lỗi thiết bị
        /// </summary>
        public async Task<InvalidEquipmentHistoryChartOutput> HandleAsync(InvalidEquipmentHistoryChartInput input)
        {
            await this.LoadDefaultData();

            var chart = new InvalidEquipmentHistoryChartOutput();
            var chartService = new InvalidEquipmentHistoryChartService();

            var fromDate = DateTime.Now.Date.AddDays(-7);
            var toDate = DateTime.Now.Date.AddDays(1).AddMicroseconds(-1);

            // Loại bỏ thiết bị đã xóa mềm (IsDelete != null) để lịch sử của chúng không bị tính vào biểu đồ
            var equipmentIdList = this._equipmentList?.Any(equip => equip.EquipmentTypeId != null && equip.EquipmentTypeId == input.EquipmentTypeId && equip.IsDelete == null) == true
                ? this._equipmentList.Where(equip => equip.EquipmentTypeId != null && equip.EquipmentTypeId == input.EquipmentTypeId && equip.IsDelete == null).Select(equip => equip.ID).ToList()
                : new List<string>();

            var historyList = equipmentIdList?.Count() > 0
                ? await this._baseRepository.AsQueryable()
                    .Where(item =>
                        item.EquipmentId != null
                        && item.StartTime != null
                        && equipmentIdList.Contains(item.EquipmentId)
                        && item.StartTime >= fromDate
                        && item.StartTime <= toDate
                    )
                    .Select(equip => new EquipmentHistoryOutput(), true)
                    .ToListAsync()
                : null;

            var ultilityService = new EquipmentHistoryChartService(this._configDataList);
            var inactiveStateInfo = ultilityService.GetStateInfo(EquipmentHistoryChartService.StateInactive);
            var errorStateInfo = ultilityService.GetStateInfo(EquipmentHistoryChartService.StateError);
            var faultStateInfo = ultilityService.GetStateInfo(EquipmentHistoryChartService.StateFault);

            var stateInfoList = new List<EquipmentStateInfo>()
            {
                inactiveStateInfo,
                errorStateInfo,
                faultStateInfo
            };

            chart = chartService.BuildChart(historyList, stateInfoList, fromDate, toDate);

            return chart;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 22/01/2025
        /// Description: Lấy dữ liệu cấu hình mặc định
        /// </summary>
        private async Task LoadDefaultData()
        {
            try
            {
                var configRequest = new SysConfigDataListRequest() { };
                var configResponse = await BaseApiClient<IConfigDataApi>.Client().List(configRequest);
                this._configDataList = configResponse.Content?.Result;

                var equipmentQuery = new TmsEquipmentListRequest() { };
                var equipmentResponse = await BaseApiClient<IEquipmentApi>.Client().List(equipmentQuery);
                this._equipmentList = equipmentResponse.Content?.Result;
            }
            catch { }
        }
    }
}
