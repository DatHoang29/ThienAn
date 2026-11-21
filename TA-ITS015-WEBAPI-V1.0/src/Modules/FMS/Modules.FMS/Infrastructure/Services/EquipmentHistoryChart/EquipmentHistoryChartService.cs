using Modules.FMS.Controllers.EquipmentHistoryChart.Dto;
using Shared.DTO.Dtos.System;

namespace Modules.FMS.Infrastructure.Services.EquipmentHistoryChart
{
    public class EquipmentHistoryChartService
    {
        /// <summary>ConfigTypeCode cho trạng thái thiết bị</summary>
        private const string ConfigTypeEquipmentState = "equipment_state";

        /// <summary>ConfigTypeCode cho màu trạng thái thiết bị</summary>
        private const string ConfigTypeEquipmentStateColor = "equipment_state_color";

        internal const string StateActive = "active";
        internal const string StateInactive = "inactive";
        internal const string StateError = "error";
        internal const string StateFault = "fault";

        private const string StateCodeActive = "1";
        private const string StateCodeInactive = "0";
        private const string StateCodeError = "2";
        private const string StateCodeFault = "3";

        private static readonly string[] FixedStateCodes = { StateCodeInactive, StateCodeActive, StateCodeError, StateCodeFault };

        private const string DefaultColorActive = "#67C23A";
        private const string DefaultColorInactive = "#909399";
        private const string DefaultColorError = "#E6A23C";
        private const string DefaultColorFault = "#F56C6C";

        private List<SysConfigDataResponse>? _configDataList = null;

        public EquipmentHistoryChartService(List<SysConfigDataResponse>? configDataList) {
            this._configDataList = configDataList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Create date: 22/01/2025
        /// Description: Lấy cấu hình màu sắc theo trạng thái thiết bị
        /// </summary>
        public EquipmentStateInfo GetStateInfo(string state)
        {
            var stateCode = "";
            var stateColor = "";

            if (state == StateActive)
            {
                stateCode = StateCodeActive;
                stateColor = this.GetConfigDataValue(ConfigTypeEquipmentStateColor, StateActive) ?? DefaultColorActive;
            }
            else if (state == StateInactive)
            {
                stateCode = StateCodeInactive;
                stateColor = this.GetConfigDataValue(ConfigTypeEquipmentStateColor, StateInactive) ?? DefaultColorInactive;
            }
            else if (state == StateError)
            {
                stateCode = StateCodeError;
                stateColor = this.GetConfigDataValue(ConfigTypeEquipmentStateColor, StateError) ?? DefaultColorError;
            }
            else if (state == StateFault)
            {
                stateCode = StateCodeFault;
                stateColor = this.GetConfigDataValue(ConfigTypeEquipmentStateColor, StateFault) ?? DefaultColorFault;
            }

            var stateName = this._configDataList?.FirstOrDefault(config =>
                    config.ConfigTypeCode != null
                    && config.Code != null
                    && config.ConfigTypeCode.Equals(ConfigTypeEquipmentState, StringComparison.OrdinalIgnoreCase)
                    && config.Code.Equals(stateCode, StringComparison.OrdinalIgnoreCase)
                )?.Name ?? state;

            var stateList = this._configDataList?.Where(config =>
                    config.ConfigTypeCode != null
                    && config.Code != null
                    && config.Value != null
                    && config.ConfigTypeCode.Equals(ConfigTypeEquipmentState, StringComparison.OrdinalIgnoreCase)
                    && config.Value.Equals(state, StringComparison.OrdinalIgnoreCase)
                )
                // 4 mã gốc (0/1/2/3) chỉ thuộc đúng bucket của nó: tránh 1 mã bị đếm vào 2 bucket
                // (vd DB cấu hình mã "2" có Value = "fault" -> trạng thái Lỗi bị đếm cả vào "error" lẫn "fault").
                .Where(config => config.Code == stateCode || !FixedStateCodes.Contains(config.Code))
                .Select(config => config.Code).Distinct().ToList() ?? new List<string>();

            // Luôn gồm mã gốc của bucket (kể cả khi DB chưa seed / cấu hình bị sửa) để line/bar lịch sử lỗi vẫn đếm được
            if (!string.IsNullOrEmpty(stateCode) && !stateList.Contains(stateCode))
                stateList.Add(stateCode);

            return new EquipmentStateInfo() { Name = stateName, StateList = stateList, Color = stateColor };
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy giá trị cấu hình theo code
        /// Create date: 22/01/2025
        /// </summary>
        public string? GetConfigDataValue(string configTypeCode, string? configDataCode)
        {
            return this._configDataList?.FirstOrDefault(config =>
                    config.ConfigTypeCode != null
                    && config.Code != null
                    && config.ConfigTypeCode.Equals(configTypeCode, StringComparison.OrdinalIgnoreCase)
                    && config.Code.Equals(configDataCode, StringComparison.OrdinalIgnoreCase)
                )?.Value ?? null;
        }
    }
}
