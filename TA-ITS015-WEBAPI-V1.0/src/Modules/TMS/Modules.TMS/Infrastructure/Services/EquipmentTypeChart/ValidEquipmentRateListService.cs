using JasperFx.Core;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Controllers.EquipmentType.Dto;
using Modules.TMS.Controllers.EquipmentTypeChart.Dto;
using Shared.DTO.Dtos.System;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;

namespace Modules.TMS.Infrastructure.Services.EquipmentTypeChart
{
    public class ValidEquipmentRateListService
    {
        private List<SysConfigDataResponse> _configDataList;
        private EquipmentTypeChartService _ultilityService;
        private IMessageBus _messageBus;

        public ValidEquipmentRateListService(IMessageBus messageBus, EquipmentTypeChartService ultilityService)
        {
            this._ultilityService = ultilityService;
            this._messageBus = messageBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 20/02/2025
        /// Description: Danh sách thiết bị cùng tỉ lệ hoạt động
        /// </summary>
        public async Task<SqlSugarPagedList<PageValidEquipmentRateOutput>> ListValidEquipmentRate()
        {
            try
            {
                await this.LoadDefaultData();

                var equimentTypeList = await this._messageBus.InvokeAsync<List<EquipmentTypeOutput>>(new EquipmentTypeInput() { });
                var equipmentList = await this._messageBus.InvokeAsync<List<EquipmentOutput>>(new EquipmentInput() { });

                // Loại thiết bị đã chuyển sang trạng thái không dùng thì không hiển thị ở giao diện lịch sử trạng thái thiết bị
                equimentTypeList = equimentTypeList?.Where(et => et.Status != BaseEnums.StatusEnum.Disable).ToList();

                var dataList = new List<PageValidEquipmentRateOutput>();
                if ((equimentTypeList?.Count() ?? 0) == 0)
                    return null;

                var successRateRange = this.GetStateRange("success");
                if (successRateRange.From == 0)
                    successRateRange.From = 100;
                if (successRateRange.To == 0)
                    successRateRange.To = 100;

                var warningRateRange = this.GetStateRange("warning");
                if (warningRateRange.From == 0)
                    warningRateRange.From = 50;
                if (warningRateRange.To == 0)
                    warningRateRange.To = 99;

                var dangerRateRange = this.GetStateRange("danger");
                if (dangerRateRange.To == 0)
                    dangerRateRange.To = 49;

                var activeStateInfo = this._ultilityService.GetStateInfo(this._configDataList, "active");

                equimentTypeList.Each(et =>
                {
                    var total = this.GetEquipmentTotal(et.ID, equipmentList);

                    var activeCount = this.GetStateCount(et.ID, activeStateInfo, equipmentList);

                    var rate = total > 0
                        ? (int)(((decimal)activeCount / (decimal)total) * 100)
                        : 100;

                    var color = this.GetStateRateColor("success");
                    var colorLabel = color;
                    if (rate >= successRateRange.From && rate <= successRateRange.To)
                    {
                        color = this.GetStateRateColor("success");
                        colorLabel = color;
                    }
                    else if (rate >= warningRateRange.From && rate <= warningRateRange.To)
                    {
                        color = this.GetStateRateColor("warning");
                        colorLabel = this.GetStateRateColor("warning");
                    }
                    else if (rate >= dangerRateRange.From && rate <= dangerRateRange.To)
                    {
                        color = this.GetStateRateColor("danger");
                        colorLabel = color;
                    }

                    dataList.Add(new PageValidEquipmentRateOutput()
                    {
                        ID = et.ID,
                        Code = et.Code,
                        ActiveIcon = et.ActiveIcon,
                        InactiveIcon = et.InactiveIcon,
                        FaultIcon = et.FaultIcon,
                        DefaultIcon = et.DefaultIcon,
                        Name = et.Name,
                        Rate = rate,
                        Total = total,
                        ActiveCount = activeCount,
                        Color = color,
                        ColorLabel = colorLabel
                    });
                });

                return new SqlSugarPagedList<PageValidEquipmentRateOutput>()
                {
                    Records = dataList
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 20/02/2025
        /// Description: Đếm số lượng thiết bị theo phân loại
        /// </summary>
        private int GetEquipmentTotal(string equipmentTypeId, List<EquipmentOutput>? equipmentList)
        {
            return equipmentList?.Where(equip =>
                    equip.EquipmentTypeId == equipmentTypeId
                    && equip.IsDelete == null
                ).Count() ?? 0;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 20/02/2025
        /// Description: Đếm số thiết bị theo trạng thái (dùng để tính tỉ lệ hoạt động)
        /// </summary>
        private int GetStateCount(string equipmentTypeId, EquipmentStateInfo stateInfo, List<EquipmentOutput>? equipmentList)
        {
            return equipmentList?.Where(e =>
                    e.EquipmentTypeId == equipmentTypeId
                    && e.EquipmentState != null
                    && e.IsDelete == null
                    && stateInfo.HasState(e.EquipmentState.ToString())
                ).Count() ?? 0;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 20/02/2025
        /// Description: Load dữ liệu mặc định cần thiết
        /// </summary>
        private async Task LoadDefaultData()
        {
            try
            {
                // Nạp config IN-PROCESS qua MessBus (không dùng HTTP self-call BaseApiClient tới ApiGateway:BaseUrl).
                // Lý do: khi API chạy bằng Kestrel (cổng 5000/5001) thay vì IIS Express (4401), HTTP self-call tới 4401
                // sẽ fail -> config null -> lưới tính ra 0%. MessBus in-process luôn chạy, giống cách lấy danh sách thiết bị.
                var configList = await this._messageBus.InvokeAsync<List<SysConfigDataOutput>>(new SysConfigDataInput() { });
                this._configDataList = configList?.Adapt<List<SysConfigDataResponse>>();
            }
            catch { }
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 20/02/2025
        /// Description: Lấy khoảng giá trị cấu hình trạng thái thiết bị
        /// </summary>
        private (int From, int To) GetStateRange(string state)
        {
            int from = 0;
            int to = 0;

            var value = this._ultilityService.GetConfigDataValue(this._configDataList, "equipment_state_percent_range", state);

            var range = value?.Split("-") ?? new string[0];
            if (range.Length == 2)
            {
                int.TryParse(range[0], out from);
                int.TryParse(range[1], out to);
            }

            return from < to ? (from, to) : (to, from);
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 20/02/2025
        /// Description: Lấy cấu hình màu sắc tương ứng với trạng thái thiết bị
        /// </summary>
        private string GetStateRateColor(string type = "")
        {
            if (type == "success")
                return this._ultilityService.GetConfigDataValue(this._configDataList, "equipment_state_color", "active") ?? "#67C23A";

            if (type == "warning")
                return this._ultilityService.GetConfigDataValue(this._configDataList, "equipment_state_color", "warning") ?? "#E6A23C";

            if (type == "danger")
                return this._ultilityService.GetConfigDataValue(this._configDataList, "equipment_state_color", "fault") ?? "#F56C6C";

            return "#909399";
        }
    }
}
