using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: SonTH
    /// Description: Cấu hình tự động xử lý sự kiện/sự cố theo loại sự kiện (TmsEventType).
    ///              Mỗi EventType chỉ có tối đa một cấu hình active (unique theo TenantId + EventTypeId).
    ///              Xem: docs/THIET-KE-KICH-BAN-XU-LY-SU-CO-TU-DONG.md mục 6.4.
    /// Created date: 24/07/2026
    /// </summary>
    [SugarTable("TmsIncidentAutomationConfig", TableDescription = "Cấu hình tự động xử lý sự kiện/sự cố theo loại sự kiện")]
    [SugarIndex("index_{table}_Ev", nameof(EventTypeId), OrderByType.Asc)]
    [SugarIndex("index_{table}_St", nameof(Status), OrderByType.Asc)]
    public class TmsIncidentAutomationConfig : EntityTenant
    {
        /// <summary>
        /// Loại sự kiện áp dụng (TmsEventType.ID).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Loại sự kiện áp dụng (TmsEventType)")]
        public string? EventTypeId { get; set; }

        /// <summary>
        /// Bật/tắt tự động thực thi.
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "0", ColumnDescription = "Bật tự động thực thi")]
        public bool IsAutoExecuteEnabled { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Danh sách nguồn phát hiện sự cố được phép tự động xử lý, các mã cách nhau bởi dấu phẩy.
        ///              Mã lấy từ loại cấu hình tms_source_detect (user, vds, fa, api) - trùng với TmsIncident.Source.
        ///
        ///              NULL hoặc rỗng = áp dụng cho MỌI nguồn (giữ nguyên hành vi cũ, tương thích ngược).
        ///              Có giá trị = chỉ những nguồn được liệt kê mới tự động xử lý.
        ///
        ///              Mục đích: sự cố do thiết bị phát hiện (vds, fa) thường xảy ra lúc không có người trực
        ///              nên tự động hóa có giá trị; sự cố do người trực tự nhập (user) thì đã có người ngồi đó,
        ///              tự động chạy tiếp là dư và rủi ro nếu nhập sai lý trình.
        ///
        ///              Độ dài 256 vì đây là danh sách, khác TmsIncident.Source chỉ chứa một mã (16).
        /// Created date: 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Nguồn phát hiện được phép tự động (rỗng = mọi nguồn)")]
        public string? AllowedSources { get; set; }

        /// <summary>
        /// State của incident dùng để kích hoạt countdown: Registration hoặc Verified.
        /// Xem IncidentAutomationConst.TriggerState.
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, DefaultValue = "verify", ColumnDescription = "State kích hoạt countdown")]
        public string TriggerState { get; set; } = "verify";

        /// <summary>
        /// Cho phép system actor tự xác minh incident nếu TriggerState = Registration.
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "0", ColumnDescription = "Tự xác minh nếu trigger từ Registration")]
        public bool AutoVerifyIncident { get; set; }

        /// <summary>
        /// Thời gian (giây) cảnh báo trước khi tự chạy.
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "30", ColumnDescription = "Thời gian cảnh báo (giây) trước khi chạy")]
        public int WarningDelaySeconds { get; set; } = 30;

        /// <summary>
        /// Kịch bản tự động (TmsTrafficPlan.ID). ExecutionMode phải là Automatic hoặc Both, cùng EventTypeId.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Kịch bản tự động (TmsTrafficPlan)")]
        public string? TrafficPlanId { get; set; }

        /// <summary>
        /// Bật tự động kết thúc sau AutoFinishAfterSeconds tính từ ActivatedAt.
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "0", ColumnDescription = "Bật tự động kết thúc")]
        public bool IsAutoFinishEnabled { get; set; }

        /// <summary>
        /// Khoảng thời gian (giây) giữ trạng thái sau ActivatedAt trước khi tự kết thúc.
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Khoảng giữ trạng thái (giây) sau ActivatedAt")]
        public int? AutoFinishAfterSeconds { get; set; }

        /// <summary>Tự hoàn thành event VDS chưa được tiếp nhận, không chạy kịch bản. Mặc định tắt.</summary>
        [SugarColumn(IsNullable = false, DefaultValue = "0")]
        public bool IsUnclaimedAutoFinishEnabled { get; set; }

        /// <summary>Số giây từ lúc tạo hồ sơ tới khi hoàn thành nếu vẫn chưa được tiếp nhận.</summary>
        [SugarColumn(IsNullable = true)]
        public int? UnclaimedAutoFinishAfterSeconds { get; set; }

        /// <summary>Mốc UTC do server đặt khi bật lại hoặc đổi loại; không áp vào hồ sơ cũ.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? UnclaimedAutoFinishFromUtc { get; set; }

        /// <summary>
        /// Chính sách khi apply lỗi. Xem IncidentAutomationConst.FailurePolicy.
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, DefaultValue = "stop-reset", ColumnDescription = "Chính sách lỗi khi apply")]
        public string FailurePolicy { get; set; } = "stop-reset";

        /// <summary>
        /// Chính sách đưa thiết bị về cấu hình mặc định khi kết thúc/lỗi. Xem IncidentAutomationConst.ResetPolicy.
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, DefaultValue = "reset-default", ColumnDescription = "Chính sách reset thiết bị về mặc định")]
        public string ResetPolicy { get; set; } = "reset-default";

        /// <summary>
        /// Version cấu hình, tăng khi thay đổi; được snapshot vào automation run tại thời điểm schedule.
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "1", ColumnDescription = "Version cấu hình")]
        public int ConfigVersion { get; set; } = 1;

        /// <summary>
        /// Trạng thái cấu hình.
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>
        /// Ghi chú.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
