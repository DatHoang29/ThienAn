namespace Modules.TMS.Core.Constants
{
    /// <summary>
    /// Constants cho tính năng tự động xử lý sự kiện/sự cố (TmsIncidentAutomationConfig).
    /// Xem: docs/THIET-KE-KICH-BAN-XU-LY-SU-CO-TU-DONG.md.
    /// Author: SonTH
    /// Created date: 24/07/2026
    /// </summary>
    public static class IncidentAutomationConst
    {
        /// <summary>
        /// State của incident dùng để kích hoạt (bắt đầu đếm ngược) automation run.
        /// Xem mục 6.4, 8.3.
        /// </summary>
        public static class TriggerState
        {
            public const string Registration = "register";
            public const string Verified = "verify";
        }

        /// <summary>
        /// Chính sách xử lý khi một action lỗi trong lúc apply. Xem mục 7.5.
        /// </summary>
        public static class FailurePolicy
        {
            /// <summary>
            /// Dừng step tiếp theo và gọi luồng reset mặc định cho các thiết bị đã applied, nếu đủ điều kiện. Mặc định.
            /// </summary>
            public const string StopAndResetDefault = "stop-reset";

            /// <summary>
            /// Ghi lỗi, tiếp tục action khác; run kết thúc với warning.
            /// </summary>
            public const string Continue = "continue";

            /// <summary>
            /// Retry đủ số lần rồi dừng và reset mặc định theo policy.
            /// </summary>
            public const string RetryThenStop = "retry-stop";

            /// <summary>
            /// Kế thừa chính sách của run/config. Chỉ dùng ở cấp TmsTrafficPlanWorkDetail.
            /// </summary>
            public const string Inherit = "inherit";
        }

        /// <summary>
        /// Chính sách đưa thiết bị về cấu hình mặc định khi kết thúc/lỗi. Xem mục 7.6.
        /// </summary>
        public static class ResetPolicy
        {
            /// <summary>
            /// Gọi chung luồng manual để áp cấu hình mặc định hiện hành của thiết bị nếu có và đủ điều kiện.
            /// </summary>
            public const string ResetToDefault = "reset-default";

            /// <summary>
            /// Không gửi lệnh mặc định; chỉ dùng cho action được phê duyệt rõ.
            /// </summary>
            public const string NoReset = "no-reset";

            /// <summary>
            /// Kế thừa chính sách của run/config. Chỉ dùng ở cấp TmsTrafficPlanWorkDetail.
            /// </summary>
            public const string Inherit = "inherit";
        }

        /// <summary>
        /// State machine của TmsIncidentAutomationRun. Xem mục 7.2.
        /// </summary>
        public static class RunStatus
        {
            public const string WarningPending = "WarningPending";
            public const string Starting = "Starting";
            public const string Running = "Running";
            public const string Active = "Active";
            public const string FinishPending = "FinishPending";
            public const string Canceling = "Canceling";
            public const string ResettingDefault = "ResettingDefault";
            public const string Completed = "Completed";
            public const string CompletedWithWarnings = "CompletedWithWarnings";
            public const string Canceled = "Canceled";
            public const string Failed = "Failed";
        }

        /// <summary>
        /// State machine của TmsIncidentAutomationAction. Xem mục 7.3.
        /// </summary>
        public static class ActionStatus
        {
            public const string Pending = "Pending";
            public const string WaitingPreviousStep = "WaitingPreviousStep";
            public const string AcquiringControl = "AcquiringControl";
            public const string Applying = "Applying";
            public const string RetryPending = "RetryPending";
            public const string Applied = "Applied";
            public const string Failed = "Failed";
            public const string Skipped = "Skipped";
        }

        /// <summary>
        /// Kết quả cập nhật thiết bị về cấu hình mặc định. Xem mục 7.4.
        /// </summary>
        public static class ResetStatus
        {
            public const string NotRequired = "NotRequired";
            public const string Pending = "Pending";
            public const string Resetting = "Resetting";
            public const string ResetToDefault = "ResetToDefault";
            public const string DefaultNotConfigured = "DefaultNotConfigured";
            public const string ResetSkippedOwnershipChanged = "ResetSkippedOwnershipChanged";
            public const string ResetSkippedStateChanged = "ResetSkippedStateChanged";
            public const string ResetSkippedHigherPriority = "ResetSkippedHigherPriority";
            public const string ResetFailed = "ResetFailed";
        }

        /// <summary>
        /// Loại thiết bị được điều khiển tự động.
        /// </summary>
        public static class DeviceType
        {
            public const string Vms = "VMS";
            public const string Lcs = "LCS";
        }

        /// <summary>
        /// Actor hệ thống khi automation tự thực hiện hành động (verify, execute, finish...).
        /// </summary>
        public const string SystemActor = "SYSTEM_AUTOMATION";

        /// <summary>
        /// Author: SonTH
        /// Description: Mã lý do kết quả của một lần thử lập lịch tự động (TryScheduleAsync).
        ///              Dùng cho ba việc: ghi log, trả về cho hệ thống ngoài khi tạo sự cố qua mediator,
        ///              và về sau phục vụ màn hình chẩn đoán "vì sao sự cố này không tự động chạy".
        ///
        ///              Trước đây TryScheduleAsync chỉ trả null ở 6 nhánh khác nhau mà không ghi gì,
        ///              nên khi tự động không chạy thì không có cách nào biết vì sao.
        /// Created date: 24/07/2026
        /// </summary>
        public static class ScheduleReason
        {
            public const string EventWindowPending = "EventWindowPending";
            public const string EventCompletedWithoutAutomation = "EventCompletedWithoutAutomation";
            public const string EventAutomationEnded = "EventAutomationEnded";
            public const string TriggerStateMismatch = "TriggerStateMismatch";
            public const string IncidentStateMismatch = "IncidentStateMismatch";
            /// <summary>Đã lập lịch thành công.</summary>
            public const string Scheduled = "Scheduled";

            /// <summary>Thiếu tham số đầu vào.</summary>
            public const string InvalidInput = "InvalidInput";

            /// <summary>Không tìm thấy sự cố, hoặc sự cố chưa có loại sự kiện.</summary>
            public const string IncidentNotFound = "IncidentNotFound";

            /// <summary>Loại sự kiện chưa có cấu hình tự động, hoặc cấu hình đang tắt / sai trigger state.</summary>
            public const string NoConfig = "NoConfig";

            /// <summary>Cấu hình tự động chưa chọn kịch bản.</summary>
            public const string NoTrafficPlan = "NoTrafficPlan";

            /// <summary>Nguồn phát hiện sự cố không nằm trong danh sách cho phép của cấu hình.</summary>
            public const string SourceNotAllowed = "SourceNotAllowed";

            /// <summary>Sự cố đang có một lần chạy tự động chưa kết thúc.</summary>
            public const string ActiveRunExists = "ActiveRunExists";

            /// <summary>Kịch bản không hợp lệ: đang tắt, sai loại sự kiện, hoặc không ở chế độ tự động.</summary>
            public const string PlanNotAutomatic = "PlanNotAutomatic";

            /// <summary>Ghi bản ghi lần chạy thất bại.</summary>
            public const string InsertFailed = "InsertFailed";

            /// <summary>
            /// Yêu cầu tạo sự cố bị trùng (hệ thống ngoài gọi lại sau timeout) nên không lập lịch lần nữa;
            /// lần gọi đầu tiên đã xử lý việc lập lịch. Chỉ dùng ở đường tích hợp.
            /// </summary>
            public const string DuplicateRequest = "DuplicateRequest";

            /// <summary>Lỗi ngoài dự kiến khi thử lập lịch.</summary>
            public const string UnexpectedError = "UnexpectedError";
        }

        /// <summary>
        /// Author: SonTH - 24/07/2026
        /// GroupCode của nhóm cấu hình SysOpConfig dùng chung cho phân hệ sự cố.
        /// Các thiết lập của tự động hóa nằm trong nhóm này dưới dạng từng Code riêng
        /// (automation_banner, automation_run_retention_days...), KHÔNG tách GroupCode mới.
        /// </summary>
        public const string ActionGroupConfigCode = "tms_incident_action_group";

        /// <summary>
        /// Author: SonTH - 24/07/2026
        /// Code cấu hình số ngày giữ lại lần chạy tự động đã kết thúc trước khi job dọn xoá.
        /// </summary>
        public const string RunRetentionDaysConfigCode = "automation_run_retention_days";

        /// <summary>
        /// Author: SonTH - 24/07/2026
        /// Số ngày giữ mặc định khi chưa có bản ghi cấu hình.
        /// </summary>
        public const int DefaultRunRetentionDays = 7;

        /// <summary>
        /// Author: SonTH
        /// Description: Mã lỗi khi run bị treo ở trạng thái chuyển tiếp quá lâu và bị worker cưỡng chế đóng.
        ///              Xảy ra khi tiến trình chết đúng lúc đang chuyển trạng thái (deploy, IIS recycle, crash).
        /// Created date: 24/07/2026
        /// </summary>
        public const string StuckTimeoutErrorCode = "StuckTimeout";

        /// <summary>
        /// Author: SonTH
        /// Description: Các trạng thái chuyển tiếp - run đang được xử lý dở. Nếu tiến trình chết ở đây thì
        ///              run mắc kẹt vĩnh viễn: worker chỉ quét WarningPending/FinishPending nên không nhìn tới,
        ///              còn TryScheduleAsync lại coi đây là "đang chạy" nên từ chối lập lịch mới cho sự cố đó.
        ///              Worker dùng danh sách này để phát hiện và cưỡng chế đóng.
        /// Created date: 24/07/2026
        /// </summary>
        public static readonly string[] TransitionalRunStatuses =
        {
            RunStatus.Starting,
            RunStatus.Running,
            RunStatus.Canceling,
            RunStatus.ResettingDefault
        };

        /// <summary>
        /// Loại action broadcast realtime cho automation run. Xem mục 13.2.
        /// </summary>
        public static class BroadcastAction
        {
            public const string WarningScheduled = "WarningScheduled";
            public const string Started = "Started";
            public const string FinishScheduled = "FinishScheduled";
            public const string ResettingDefault = "ResettingDefault";
            public const string Completed = "Completed";
            public const string Canceled = "Canceled";
            public const string Failed = "Failed";
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Danh sách các RunStatus đã kết thúc (không còn job/hành động nào chờ xử lý).
        /// Created date: 24/07/2026
        /// </summary>
        public static readonly string[] TerminalRunStatuses =
        {
            RunStatus.Completed,
            RunStatus.CompletedWithWarnings,
            RunStatus.Canceled,
            RunStatus.Failed
        };
    }
}
