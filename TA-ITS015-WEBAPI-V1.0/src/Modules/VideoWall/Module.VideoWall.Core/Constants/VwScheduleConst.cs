namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Hằng số dùng chung của Lịch phát kịch bản tự động (VwSchedule).
    ///              Khớp với FE: views/videoWall/schedule/component/scheduleOptions.ts
    /// Created date: 06/10/2026
    /// </summary>
    public static class VwScheduleConst
    {
        /// <summary>
        /// Tiền tố Id recurring job Hangfire. MỖI bản ghi VwSchedule là MỘT recurring job
        /// có Id = JobIdPrefix + VwSchedule.ID — nhờ prefix này mà lúc khởi động dọn được job mồ côi.
        /// </summary>
        public const string JobIdPrefix = "vw-schedule-";

        /// <summary>
        /// Lịch "Một lần" bị trễ quá ngưỡng này (server tắt đúng giờ chạy, Hangfire chạy bù khi bật lại)
        /// thì bỏ qua, không chuyển kịch bản muộn làm sai màn hình đang hiển thị.
        /// </summary>
        public const int OnceMaxDelayMinutes = 30;

        /// <summary>
        /// Khoá cấu hình hoạt động (SysOpConfig) của lịch phát kịch bản.
        /// </summary>
        public static class OpConfig
        {
            /// <summary>SysOpConfig.GroupCode — khớp SysConfigData loại operation_config_type.</summary>
            public const string GroupCode = "vw_config";

            /// <summary>
            /// Khoảng đệm (phút) chống trùng giờ: 2 lịch đang bật (kể cả cùng kịch bản) chạy cách nhau
            /// không quá số phút này thì bị chặn khi lưu. 0 = chỉ chặn khi trùng đúng phút.
            /// </summary>
            public const string ConflictBufferMinutes = "vw_schedule_conflict_buffer_minutes";

            /// <summary>Dùng khi chưa cấu hình / giá trị không hợp lệ.</summary>
            public const int DefaultConflictBufferMinutes = 0;
        }

        /// <summary>
        /// Giá trị VwSchedule.ScheduleType.
        /// Đã BỎ loại Cron (4) — người vận hành khó nhập đúng; lịch cũ ScheduleType = 4 không còn được chạy.
        /// </summary>
        public static class ScheduleType
        {
            public const int Daily = 1;
            public const int Weekly = 2;
            public const int Once = 3;

            /// <summary>Giá trị lớn nhất hợp lệ (dùng cho validator).</summary>
            public const int Max = Once;
        }

        /// <summary>
        /// Giá trị VwSchedule.Action.
        /// </summary>
        public static class Action
        {
            /// <summary>Chuyển sang kịch bản đích (TargetSceneId).</summary>
            public const string ActivateScene = "activate_scene";

            /// <summary>Tắt màn hình — TẠM ẨN, chưa xử lý.</summary>
            public const string CloseScreens = "close_screens";

            /// <summary>Bật màn hình — TẠM ẨN, chưa xử lý.</summary>
            public const string OpenScreens = "open_screens";

            /// <summary>
            /// Các hành động đang được hỗ trợ. close_screens / open_screens tạm ẩn:
            /// muốn mở lại thì thêm vào đây và bổ sung nhánh trong VwScheduleJobService.ExecuteActionAsync.
            /// </summary>
            public static readonly string[] Supported = { ActivateScene };
        }
    }
}
