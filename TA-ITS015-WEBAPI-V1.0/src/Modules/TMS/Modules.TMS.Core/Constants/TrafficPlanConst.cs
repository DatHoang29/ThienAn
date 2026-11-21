namespace Modules.TMS.Core.Constants
{
    /// <summary>
    /// Constants cho TmsTrafficPlan (kịch bản xử lý sự kiện/sự cố)
    /// Author: SonTH
    /// Created date: 24/07/2026
    /// </summary>
    public static class TrafficPlanConst
    {
        /// <summary>
        /// Chế độ thực thi của kịch bản.
        /// Xem: docs/THIET-KE-KICH-BAN-XU-LY-SU-CO-TU-DONG.md mục 5.2.
        /// </summary>
        public static class ExecutionMode
        {
            /// <summary>
            /// Chỉ dùng thủ công. Giá trị mặc định cho dữ liệu cũ.
            /// </summary>
            public const string Manual = "Manual";

            /// <summary>
            /// Chỉ dùng tự động.
            /// </summary>
            public const string Automatic = "Automatic";

            /// <summary>
            /// Dùng được cho cả thủ công và tự động.
            /// </summary>
            public const string Both = "Both";
        }
    }
}
