using Modules.TMS.Controllers.EquipmentTypeChart.Dto;
using Shared.DTO.Dtos.System;

namespace Modules.TMS.Infrastructure.Services.EquipmentTypeChart
{
    public class EquipmentTypeChartService
    {
        /// <summary>Mã trạng thái gốc: 0 inactive, 1 active, 2 error, 3 fault</summary>
        private static readonly string[] FixedStateCodes = { "0", "1", "2", "3" };

        /// <summary>
        /// Author: ThangNN
        /// Created date: 20/02/2025
        /// Description: Hàm lấy thông tin trạng thái của equipment
        /// </summary>
        public EquipmentStateInfo GetStateInfo(List<SysConfigDataResponse> configDataList, string state)
        {
            var stateCode = "";
            var stateColor = "";

            if (state == "active")
            {
                stateCode = "1";
                stateColor = this.GetConfigDataValue(configDataList, "equipment_state_color", "active") ?? "#67C23A";
            }
            else if (state == "inactive")
            {
                stateCode = "0";
                stateColor = this.GetConfigDataValue(configDataList, "equipment_state_color", "inactive") ?? "#909399";
            }
            else if (state == "error")
            {
                stateCode = "2";
                stateColor = this.GetConfigDataValue(configDataList, "equipment_state_color", "error") ?? "#E6A23C";
            }
            else if (state == "fault")
            {
                stateCode = "3";
                stateColor = this.GetConfigDataValue(configDataList, "equipment_state_color", "fault") ?? "#F56C6C";
            }

            var stateName = configDataList?.FirstOrDefault(config =>
                    config.ConfigTypeCode != null
                    && config.Code != null
                    && config.ConfigTypeCode.Equals("equipment_state", StringComparison.OrdinalIgnoreCase)
                    && config.Code.Equals(stateCode, StringComparison.OrdinalIgnoreCase)
                )?.Name ?? state;

            var stateList = configDataList?.Where(config =>
                    config.ConfigTypeCode != null
                    && config.Code != null
                    && config.Value != null
                    && config.ConfigTypeCode.Equals("equipment_state", StringComparison.OrdinalIgnoreCase)
                    && config.Value.Equals(state, StringComparison.OrdinalIgnoreCase)
                )
                // 4 mã gốc (0/1/2/3) chỉ thuộc đúng bucket của nó: tránh 1 mã bị đếm vào 2 bucket
                // (vd DB cấu hình mã "2" có Value = "fault" -> thiết bị Lỗi bị đếm cả vào "error" lẫn "fault").
                .Where(config => config.Code == stateCode || !FixedStateCodes.Contains(config.Code))
                .Select(config => config.Code).Distinct().ToList() ?? new List<string>();

            // Luôn gồm mã gốc của bucket (kể cả khi DB chưa seed / config bị xóa): tránh StateList rỗng khiến chart đếm 0 và FE trắng
            if (!string.IsNullOrEmpty(stateCode) && !stateList.Contains(stateCode))
                stateList.Add(stateCode);

            return new EquipmentStateInfo() { Name = stateName, StateList = stateList, Color = stateColor };
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 20/02/2025
        /// Description: Hàm lấy giá trị cấu hình configData
        /// </summary>
        public string? GetConfigDataValue(List<SysConfigDataResponse> configDataList, string configTypeCode, string? configDataCode)
        {
            return configDataList?.FirstOrDefault(config =>
                    config.ConfigTypeCode != null
                    && config.Code != null
                    && config.ConfigTypeCode.Equals(configTypeCode, StringComparison.OrdinalIgnoreCase)
                    && config.Code.Equals(configDataCode, StringComparison.OrdinalIgnoreCase)
                )?.Value ?? null;
        }
    }
}
