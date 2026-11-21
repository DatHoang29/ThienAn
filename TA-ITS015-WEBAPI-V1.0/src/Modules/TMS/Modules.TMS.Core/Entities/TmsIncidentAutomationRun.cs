using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: SonTH
    /// Description: Runtime của một lần tự động xử lý sự kiện/sự cố (state machine bền vững).
    ///              Bảng này là NGUỒN SỰ THẬT DUY NHẤT cho việc lập lịch: TmsIncidentAutomationWorker quét
    ///              theo (Status, ExecuteDueAt) và (Status, AutoFinishAt) để kích hoạt đúng lúc, nên không
    ///              lưu job id của scheduler ngoài (không dùng Hangfire cho luồng này).
    ///              Chưa bao gồm điều khiển thiết bị (VMS/LCS) - xem docs mục 8.6-8.11 cho phần mở rộng sau.
    ///              Xem: docs/THIET-KE-KICH-BAN-XU-LY-SU-CO-TU-DONG.md mục 6.7, 7.2.
    /// Created date: 24/07/2026
    /// </summary>
    [SugarTable("TmsIncidentAutomationRun", TableDescription = "Runtime một lần tự động xử lý sự kiện/sự cố")]
    [SugarIndex("index_{table}_Inc", nameof(IncidentId), OrderByType.Asc)]
    [SugarIndex("index_{table}_St", nameof(Status), OrderByType.Asc)]
    // Hai index phục vụ vòng quét của TmsIncidentAutomationWorker
    [SugarIndex("index_{table}_StExe", nameof(Status), OrderByType.Asc, nameof(ExecuteDueAt), OrderByType.Asc)]
    [SugarIndex("index_{table}_StFin", nameof(Status), OrderByType.Asc, nameof(AutoFinishAt), OrderByType.Asc)]
    public class TmsIncidentAutomationRun : EntityTenant
    {
        /// <summary>
        /// Incident được xử lý (TmsIncident.ID).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Incident được xử lý")]
        public string? IncidentId { get; set; }

        /// <summary>
        /// Số lần chạy trên incident (1, 2, 3...).
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "1", ColumnDescription = "Số lần chạy trên incident")]
        public int RunNo { get; set; } = 1;

        /// <summary>
        /// Snapshot loại sự kiện tại thời điểm schedule.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Snapshot loại sự kiện")]
        public string? EventTypeId { get; set; }

        /// <summary>
        /// Cấu hình nguồn (TmsIncidentAutomationConfig.ID).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Cấu hình nguồn")]
        public string? AutomationConfigId { get; set; }

        /// <summary>
        /// Version cấu hình đã dùng (snapshot tại thời điểm schedule).
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "1", ColumnDescription = "Version cấu hình đã dùng")]
        public int ConfigVersion { get; set; } = 1;

        /// <summary>
        /// Kịch bản nguồn (TmsTrafficPlan.ID).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Kịch bản nguồn")]
        public string? TrafficPlanId { get; set; }

        /// <summary>
        /// Version kịch bản đã dùng (snapshot tại thời điểm schedule).
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "1", ColumnDescription = "Version kịch bản đã dùng")]
        public int PlanVersion { get; set; } = 1;

        /// <summary>
        /// Snapshot toàn bộ cấu hình (JSON) tại thời điểm schedule để audit/retry;
        /// không đọc lại config live sau khi run đã tạo.
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Snapshot cấu hình (JSON)")]
        public string? ConfigSnapshotJson { get; set; }

        /// <summary>
        /// State của incident đã kích hoạt run: Registration hoặc Verified.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "State đã kích hoạt run")]
        public string? TriggerState { get; set; }

        /// <summary>
        /// State machine của run. Xem IncidentAutomationConst.RunStatus.
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, DefaultValue = "WarningPending", ColumnDescription = "State machine của run")]
        public string Status { get; set; } = "WarningPending";

        /// <summary>
        /// Khóa idempotency, ví dụ incident:{id}:run:{runNo}.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Khóa idempotency")]
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// Thời điểm bắt đầu cảnh báo.
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm bắt đầu cảnh báo")]
        public DateTime? WarningStartedAt { get; set; }

        /// <summary>
        /// Thời điểm được phép chạy (kết thúc đếm ngược cảnh báo).
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm được phép chạy")]
        public DateTime? ExecuteDueAt { get; set; }

        /// <summary>
        /// Thời điểm bắt đầu apply (job Execute claim thành công).
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm bắt đầu apply")]
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Thời điểm kịch bản được kích hoạt thành công (incident đã InProgress).
        /// Auto-finish tính từ mốc này.
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm kích hoạt thành công")]
        public DateTime? ActivatedAt { get; set; }

        /// <summary>
        /// Thời điểm dự kiến tự động kết thúc = ActivatedAt + AutoFinishAfterSeconds.
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm dự kiến tự động kết thúc")]
        public DateTime? AutoFinishAt { get; set; }

        /// <summary>
        /// Thời điểm run hoàn tất (Completed/Canceled/Failed).
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm run hoàn tất")]
        public DateTime? FinishedAt { get; set; }

        /// <summary>
        /// Lý do hủy (nếu Status = Canceled).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Lý do hủy")]
        public string? CancelReason { get; set; }

        /// <summary>
        /// Mã lỗi cuối (nếu Status = Failed).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Mã lỗi cuối")]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Thông tin lỗi (đã loại dữ liệu nhạy cảm).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Thông tin lỗi")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Actor khởi tạo run: SYSTEM_AUTOMATION hoặc account người bấm "chạy ngay".
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Actor khởi tạo run")]
        public string? StartedBy { get; set; }
    }
}
