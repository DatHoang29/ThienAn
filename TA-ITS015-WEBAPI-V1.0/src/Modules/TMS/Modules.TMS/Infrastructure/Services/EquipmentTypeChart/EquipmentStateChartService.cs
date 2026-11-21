using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Controllers.EquipmentTypeChart.Dto;
using Shared.DTO.Constants.Application;
using Shared.DTO.Dtos.System;
using Shared.Infrastructure.Services;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;

namespace Modules.TMS.Infrastructure.Services.EquipmentTypeChart
{
    public class EquipmentStateChartService
    {
        private List<SysConfigDataResponse> _configDataList;
        private EquipmentTypeChartService _ultilityService;
        private IMessageBus _messageBus;
        private BaseCacheService _cache;
        public EquipmentStateChartService(IMessageBus messageBus, EquipmentTypeChartService ultilityService)
        {
            this._messageBus = messageBus;
            this._ultilityService = ultilityService;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 20/02/2025
        /// Description: Equipment state chart
        /// </summary>
        public async Task<EquipmentStateChartOutput> BuildEquipmentStateChart(string equimentTypeId)
        {
            try
            {
                await this.LoadDefaultData();

                var equipmentList = await this._messageBus.InvokeAsync<List<EquipmentOutput>>(new EquipmentInput() { });

                var chart = new EquipmentStateChartOutput();

                var activeStateInfo = this._ultilityService.GetStateInfo(this._configDataList, "active");
                var inactiveStateInfo = this._ultilityService.GetStateInfo(this._configDataList, "inactive");
                var errorStateInfo = this._ultilityService.GetStateInfo(this._configDataList, "error");
                var faultStateInfo = this._ultilityService.GetStateInfo(this._configDataList, "fault");

                var activeStateCount = this.CountEquipmentByState(equimentTypeId, activeStateInfo, equipmentList);
                var inactiveStateCount = this.CountEquipmentByState(equimentTypeId, inactiveStateInfo, equipmentList);
                var errorStateCount = this.CountEquipmentByState(equimentTypeId, errorStateInfo, equipmentList);
                var faultStateCount = this.CountEquipmentByState(equimentTypeId, faultStateInfo, equipmentList);

                var allStateCount = activeStateCount + inactiveStateCount + errorStateCount + faultStateCount;

                var dataList = allStateCount > 0
                    ? new object[]{
                            new { Name = activeStateInfo.Name, Value = activeStateCount, ItemStyle = new { Color = activeStateInfo.Color } },
                            new { Name = inactiveStateInfo.Name, Value = inactiveStateCount, ItemStyle = new { Color = inactiveStateInfo.Color } },
                            new { Name = errorStateInfo.Name, Value = errorStateCount, ItemStyle = new { Color = errorStateInfo.Color } },
                            new { Name = faultStateInfo.Name, Value = faultStateCount, ItemStyle = new { Color = faultStateInfo.Color } }
                        }
                    : new object[] { };

                chart.SeriesData = dataList;

                return chart;
            }
            catch
            {
                return new EquipmentStateChartOutput();
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Đếm số lượng thiết bị theo trạng thái
        /// Created date: 20/02/2025
        /// </summary>
        private int CountEquipmentByState(string equipmentTypeId, EquipmentStateInfo stateInfo, List<EquipmentOutput>? equipmentList)
        {
            var count = equipmentList?.Where(equip =>
                    equip.EquipmentTypeId == equipmentTypeId
                    && equip.EquipmentState != null
                    && equip.IsDelete == null
                    && stateInfo.StateList.Contains(equip.EquipmentState.ToString())
                ).Count() ?? 0;

            return count;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Load dữ liệu mặc định
        /// Created date: 20/02/2025
        /// </summary>
        private async Task LoadDefaultData()
        {
            try
            {
                // Nạp config IN-PROCESS qua MessBus (không dùng HTTP self-call BaseApiClient tới ApiGateway:BaseUrl).
                // Tránh phụ thuộc cổng gateway (4401) khi chạy Kestrel; MessBus in-process luôn chạy.
                var configList = await this._messageBus.InvokeAsync<List<SysConfigDataOutput>>(new SysConfigDataInput() { });
                this._configDataList = configList?.Adapt<List<SysConfigDataResponse>>();
            }
            catch { }
        }
    }
}
