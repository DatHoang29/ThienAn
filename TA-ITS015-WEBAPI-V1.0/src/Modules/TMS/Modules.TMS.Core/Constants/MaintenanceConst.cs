namespace Modules.TMS.Core.Constants
{
    /// <summary>
    /// Constants cho Maintenance Schedule
    /// Author: System
    /// Created date: 10/12/2025
    /// </summary>
    public static class MaintenanceConst
    {
        /// <summary>
        /// Trạng thái lịch bảo trì
        /// </summary>
        public static class ScheduleStatus
        {
            /// <summary>
            /// Đăng ký
            /// </summary>
            // Canonical (english lowercase)
            public const string Register = "register";

            /// <summary>
            /// Xác nhận
            /// </summary>
            public const string Confirm = "confirm";

            /// <summary>
            /// Đang thực hiện
            /// </summary>
            public const string Process = "process";

            /// <summary>
            /// Hoàn thành
            /// </summary>
            public const string Finish = "finish";

            /// <summary>
            /// Hủy bỏ
            /// </summary>
            public const string Cancel = "cancel";

            /// <summary>
            /// Quá hạn
            /// </summary>
            public const string Overdue = "overdue";

            // Backward-compatible aliases (old canonical constant names)
            public const string Draft = Register;
            public const string Scheduled = Confirm;
            public const string InProgress = Process;
            public const string Completed = Finish;
            public const string Cancelled = Cancel;

            // Legacy (backward-compatible for old DB rows / older clients)
            public const string LegacyDraft = "Draft";
            public const string LegacyDraftLower = "draft";

            public const string LegacyScheduled = "Scheduled";
            public const string LegacyScheduleLower = "schedule";
            public const string LegacyScheduledLower = "scheduled";

            public const string LegacyInProgress = "InProgress";
            public const string LegacyInProgressUnderscore = "in_progress";
            public const string LegacyInProgressLower = "inprogress";

            public const string LegacyCompleted = "Completed";
            public const string LegacyCompletedLower = "completed";

            public const string LegacyCancelled = "Cancelled";
            public const string LegacyCanceled = "Canceled";
            public const string LegacyCancelledLower = "cancelled";
            public const string LegacyCanceledLower = "canceled";
        }

        /// <summary>
        /// Trạng thái thiết bị trong lịch bảo trì
        /// </summary>
        public static class EquipmentStatus
        {
            /// <summary>
            /// Chờ xử lý
            /// </summary>
            // Canonical (english lowercase)
            public const string Pending = "pending";
            public const string LegacyPending = "Pending";

            /// <summary>
            /// Đang thực hiện
            /// </summary>
            public const string InProgress = "in_progress";
            public const string LegacyInProgress = "InProgress";

            /// <summary>
            /// Đã hoàn thành
            /// </summary>
            // Note: Canonical value is 'checked' to align with FE equipment check flow.
            public const string Completed = "checked";
            public const string LegacyCompleted = "Completed";
            public const string LegacyCompletedLower = "completed";
            public const string LegacyFinishLower = "finish";

            /// <summary>
            /// Bỏ qua
            /// </summary>
            public const string Skipped = "skipped";
            public const string LegacySkipped = "Skipped";
        }

        /// <summary>
        /// Trạng thái công việc
        /// </summary>
        public static class TaskStatus
        {
            /// <summary>
            /// Chờ thực hiện
            /// </summary>
            // Canonical (english lowercase)
            public const string Pending = "pending";

            /// <summary>
            /// Đã thực hiện
            /// </summary>
            public const string Completed = "finish";

            /// <summary>
            /// Bỏ qua
            /// </summary>
            public const string Skipped = "skipped";

            // Legacy values (backward-compatible for old DB rows)
            public const string LegacyVnPending = "cho_thuc_hien";
            public const string LegacyVnCompleted = "da_thuc_hien";
            public const string LegacyVnSkipped = "bo_qua";

            // Legacy values (backward-compatible for old DB rows)
            public const string LegacyPending = "Pending";
            public const string LegacyInProgress = "InProgress";
            public const string LegacyInProgressLower = "inprogress";
            public const string LegacyCompleted = "Completed";
            public const string LegacyCompletedLower = "completed";
            public const string LegacySkipped = "Skipped";
        }

        /// <summary>
        /// Vai trò trong lịch bảo trì
        /// </summary>
        public static class Role
        {
            /// <summary>
            /// Trưởng nhóm
            /// </summary>
            // Canonical (english lowercase)
            public const string Leader = "leader";

            // Legacy (TitleCase)
            public const string LegacyLeader = "Leader";

            /// <summary>
            /// Thành viên
            /// </summary>
            public const string Member = "member";
            public const string LegacyMember = "Member";
        }

        /// <summary>
        /// Mức ưu tiên
        /// </summary>
        public static class Priority
        {
            /// <summary>
            /// Cao
            /// </summary>
            // Canonical (english lowercase)
            public const string High = "high";
            public const string LegacyHigh = "High";

            /// <summary>
            /// Trung bình
            /// </summary>
            public const string Medium = "medium";
            public const string LegacyMedium = "Medium";

            /// <summary>
            /// Thấp
            /// </summary>
            public const string Low = "low";
            public const string LegacyLow = "Low";
        }

        /// <summary>
        /// Tần suất bảo trì
        /// </summary>
        public static class Frequency
        {
            /// <summary>
            /// Hàng ngày
            /// </summary>
            // Canonical (english lowercase)
            public const string Daily = "daily";
            public const string LegacyDaily = "Daily";

            /// <summary>
            /// Hàng tuần
            /// </summary>
            public const string Weekly = "weekly";
            public const string LegacyWeekly = "Weekly";

            /// <summary>
            /// Hàng tháng
            /// </summary>
            public const string Monthly = "monthly";
            public const string LegacyMonthly = "Monthly";

            /// <summary>
            /// Hàng quý
            /// </summary>
            public const string Quarterly = "quarterly";
            public const string LegacyQuarterly = "Quarterly";

            /// <summary>
            /// Hàng năm
            /// </summary>
            public const string Yearly = "yearly";
            public const string LegacyYearly = "Yearly";
        }

        /// <summary>
        /// Loại thay đổi trong lịch sử
        /// </summary>
        public static class HistoryActionType
        {
            /// <summary>
            /// Tạo mới
            /// </summary>
            public const string Create = "create";
            public const string LegacyCreate = "Create";

            /// <summary>
            /// Cập nhật
            /// </summary>
            public const string Update = "update";
            public const string LegacyUpdate = "Update";

            /// <summary>
            /// Xóa
            /// </summary>
            public const string Delete = "delete";
            public const string LegacyDelete = "Delete";

            /// <summary>
            /// Thay đổi trạng thái
            /// </summary>
            public const string StatusChange = "status_change";
            public const string LegacyStatusChange = "StatusChange";

            /// <summary>
            /// Thêm thiết bị
            /// </summary>
            public const string AddEquipment = "add_equipment";
            public const string LegacyAddEquipment = "AddEquipment";

            /// <summary>
            /// Xóa thiết bị
            /// </summary>
            public const string RemoveEquipment = "remove_equipment";
            public const string LegacyRemoveEquipment = "RemoveEquipment";

            /// <summary>
            /// Cập nhật trạng thái thiết bị
            /// </summary>
            public const string UpdateEquipmentStatus = "update_equipment_status";
            public const string LegacyUpdateEquipmentStatus = "UpdateEquipmentStatus";

            /// <summary>
            /// Xác nhận đã kiểm tra thiết bị
            /// </summary>
            public const string ConfirmEquipmentChecked = "confirm_equipment_checked";
            public const string LegacyConfirmEquipmentChecked = "ConfirmEquipmentChecked";

            /// <summary>
            /// Phân công công việc
            /// </summary>
            public const string AssignTask = "assign_task";
            public const string LegacyAssignTask = "AssignTask";

            /// <summary>
            /// Xóa phân công
            /// </summary>
            public const string RemoveTask = "remove_task";
            public const string LegacyRemoveTask = "RemoveTask";

            /// <summary>
            /// Hoàn thành công việc
            /// </summary>
            public const string CompleteTask = "complete_task";
            public const string LegacyCompleteTask = "CompleteTask";
        }

        /// <summary>
        /// Loại cảnh báo
        /// </summary>
        public static class AlertType
        {
            /// <summary>
            /// Từ rule tự động
            /// </summary>
            public const string RuleTriggered = "rule_triggered";
            public const string LegacyRuleTriggered = "RuleTriggered";

            /// <summary>
            /// Yêu cầu thủ công
            /// </summary>
            public const string ManualRequest = "manual_request";
            public const string LegacyManualRequest = "ManualRequest";

            /// <summary>
            /// Lịch bảo trì đến hạn
            /// </summary>
            public const string ScheduleDue = "schedule_due";
            public const string LegacyScheduleDue = "ScheduleDue";
        }

        /// <summary>
        /// Trạng thái cảnh báo
        /// </summary>
        public static class AlertStatus
        {
            /// <summary>
            /// Đang chờ xử lý
            /// </summary>
            public const string Pending = "pending";
            public const string LegacyPending = "Pending";

            /// <summary>
            /// Đã xác nhận
            /// </summary>
            public const string Acknowledged = "acknowledged";
            public const string LegacyAcknowledged = "Acknowledged";

            /// <summary>
            /// Đã tạo lịch
            /// </summary>
            public const string Scheduled = "schedule";
            public const string LegacyScheduled = "Scheduled";
            public const string LegacyScheduledLower = "scheduled";

            /// <summary>
            /// Đã bỏ qua
            /// </summary>
            public const string Dismissed = "dismissed";
            public const string LegacyDismissed = "Dismissed";
        }
    }
}
