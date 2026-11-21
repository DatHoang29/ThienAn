using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsTrafficPlanWorkDetail")]
    public class TmsTrafficPlanWorkDetails : EntityTenant
    {

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string TrafficPlanId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string WorkDetailId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Step { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string ExecuteOrgId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string State { get; set; }

        /// <summary>
        /// Rule chọn loại/vị trí thiết bị (TmsEquipmentEventRule.ID).
        /// Bắt buộc với bước điều khiển tự động (State = 2, ControlType = VMS/LCS).
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Rule chọn thiết bị (TmsEquipmentEventRule)")]
        public string? EquipmentEventRuleId { get; set; }

        /// <summary>
        /// ID cấu hình/template lệnh VMS/LCS hiện hữu (nếu module thiết bị có bảng cấu hình dùng chung).
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID cấu hình/template lệnh thiết bị")]
        public string? DeviceCommandConfigId { get; set; }

        /// <summary>
        /// Payload lệnh fallback khi chưa có bảng cấu hình lệnh dùng chung cho loại thiết bị.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Payload lệnh fallback (JSON)")]
        public string? CommandPayloadJson { get; set; }

        /// <summary>
        /// Version schema của CommandPayloadJson.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "1", ColumnDescription = "Version schema payload")]
        public int PayloadSchemaVersion { get; set; } = 1;

        /// <summary>
        /// Delay (giây) sau khi detail trước hoàn tất, trước khi thực thi detail này.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "0", ColumnDescription = "Delay (giây) sau detail trước")]
        public int DelayAfterPreviousSeconds { get; set; } = 0;

        /// <summary>
        /// Timeout (giây) cho một lần gửi lệnh thiết bị.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "30", ColumnDescription = "Timeout (giây) một lần gửi lệnh")]
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Số lần retry tối đa khi gửi lệnh lỗi.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "3", ColumnDescription = "Số lần retry tối đa")]
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Khoảng cách (giây) giữa các lần retry.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "5", ColumnDescription = "Khoảng cách (giây) giữa các lần retry")]
        public int RetryIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// Chính sách khi lỗi: Inherit (theo run), StopAndResetDefault, Continue, RetryThenStop.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, DefaultValue = "inherit", ColumnDescription = "Chính sách khi lỗi")]
        public string FailurePolicy { get; set; } = "inherit";

        /// <summary>
        /// Chính sách reset thiết bị về mặc định: Inherit (theo run), ResetToDefault, NoReset.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, DefaultValue = "inherit", ColumnDescription = "Chính sách reset thiết bị về mặc định")]
        public string ResetPolicy { get; set; } = "inherit";

        /// <summary>
        /// Detail có bắt buộc để run chuyển Active hay không.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "1", ColumnDescription = "Bắt buộc để run chuyển Active")]
        public bool IsRequired { get; set; } = true;

    }
}
