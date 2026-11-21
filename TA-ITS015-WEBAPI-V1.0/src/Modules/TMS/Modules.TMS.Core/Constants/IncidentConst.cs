namespace Modules.TMS.Core.Constants
{
    /// <summary>
    /// Author: SonTH
    /// Description: Constants cấp sự cố (TmsIncident), dùng chung cho cả luồng thủ công và tự động.
    ///              Tách khỏi IncidentAutomationConst vì ở đây có cả giá trị của luồng thủ công.
    /// Created date: 24/07/2026
    /// </summary>
    public static class IncidentConst
    {
        /// <summary>
        /// Author: SonTH
        /// Description: Chế độ xử lý sự cố. CHỈ trả lời một câu hỏi: sự cố này do chế độ nào xử lý.
        ///              KẾT QUẢ xử lý không nằm ở đây - nó thuộc luồng thủ công, ghi ở TmsIncident.State
        ///              (Finished / Canceled / Skipped) cùng FinishedBy / FinishedDate.
        ///
        ///              Vì sao không có giá trị kiểu "auto-failed" / "auto-canceled": tạo thêm giá trị như vậy
        ///              là dựng một bộ từ vựng SONG SONG với State cho cùng một sự việc, và khi hai bên lệch
        ///              nhau thì không có quy tắc nào nói cái nào đúng. Luồng thủ công là trung tâm, tự động
        ///              chỉ là lớp bổ trợ nên không được tự định nghĩa kết quả riêng.
        ///
        ///              Kết quả vẫn suy ra đầy đủ mà không mất thông tin, vì tự động hóa chuyển trạng thái
        ///              bằng chính máy trạng thái thủ công với Actor = IncidentAutomationConst.SystemActor,
        ///              nên các cột audit trên chính bảng sự cố đã kể trọn câu chuyện:
        ///                auto  + FinishedBy = SYSTEM_AUTOMATION            -> tự động trọn vẹn
        ///                auto  + FinishedBy là người (hoặc còn trống)      -> tự động dừng giữa đường, người tiếp tục
        ///                manual                                            -> thủ công hoàn toàn
        ///              Cách này còn đúng cả sau khi bảng TmsIncidentAutomationRun bị job dọn xoá, vì các cột
        ///              audit nằm trên bảng sự cố chứ không nằm ở bảng run.
        ///
        ///              Loại cấu hình tương ứng cho giao diện: tms_incident_process_mode.
        /// Created date: 24/07/2026
        /// </summary>
        public static class ProcessMode
        {
            /// <summary>Xử lý thủ công. Mặc định cho mọi sự cố.</summary>
            public const string Manual = "manual";

            /// <summary>
            /// Tự động hóa đã thực sự áp kịch bản lên sự cố này.
            /// Chỉ đặt khi tự động chuyển sự cố sang InProgress thành công - xem TmsIncidentAutomationService.ExecuteAsync.
            /// KHÔNG đặt ở thời điểm lập lịch: lúc đó tự động chưa làm gì, và nếu người trực hủy trong khi
            /// còn đếm ngược thì sự cố thực chất được xử lý thủ công hoàn toàn.
            /// </summary>
            public const string Auto = "auto";
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Tạo sự cố từ bản tin VDS đã có (tab Sự kiện).
        /// Created date: 23/10/2026
        /// </summary>
        public static class FromVdsEvent
        {
            /// <summary>
            /// <c>TmsModules.KeyName</c> của module CCTV: camera thuộc module này mới stream được, nên sự cố trỏ
            /// sang các camera đó (camera VDS phát hiện không xem trực tiếp được). Cùng ý nghĩa với
            /// <c>Catalog:CctvModuleId</c> của Services.Vds.
            /// </summary>
            public const string CctvModuleKey = "CCTV";

            /// <summary>Hình thức tạo sự cố từ sự kiện (CreateIncidentFromVdsEventInput.Mode).</summary>
            public static class Mode
            {
                /// <summary>Vào luồng xử lý như sự cố VDS tự tạo: Đăng ký, tự động (nếu có cấu hình) hoặc thủ công.</summary>
                public const string Process = "process";

                /// <summary>Chỉ ghi nhận sự việc đã qua: tạo thẳng ở trạng thái kết thúc, không chạy tự động/thiết bị.</summary>
                public const string Record = "record";
            }
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Cửa sổ hiển thị sự cố trên màn hình giám sát (bảng + icon bản đồ) theo SysOpConfig nhóm
        /// <c>operation_config</c>. Bảng nhận số ngày từ FE; luồng icon không có tham số nên BE tự đọc cùng mã.
        /// Created date: 26/10/2026
        /// </summary>
        public static class DisplayWindow
        {
            public const string GroupCode = "operation_config";

            /// <summary>Sự cố đang mở (Đăng ký/Đã xác nhận/Đang xử lý) chỉ hiện nếu bắt đầu trong N ngày gần nhất.</summary>
            public const string OpenDaysLimitCode = "open_incident_days_limit";

            /// <summary>Sự cố đã đóng chỉ hiện nếu bắt đầu trong N ngày gần nhất.</summary>
            public const string ClosedDaysLimitCode = "closed_incident_days_limit";

            public const int DefaultOpenDays = 7;
            public const int DefaultClosedDays = 1;
        }
    }
}
