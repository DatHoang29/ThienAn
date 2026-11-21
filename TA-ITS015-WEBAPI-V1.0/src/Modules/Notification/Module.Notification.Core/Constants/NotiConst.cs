namespace Modules.Notification.Core.Constants
{
    /// <summary>
    /// Author: SonTH
    /// Description: Hằng số dùng chung của phân hệ Notification.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiConst
    {
        /// <summary>
        /// ConfigId của CSDL riêng cho phân hệ Notification (khai trong Configuration/Database.json).
        /// Toàn bộ bảng Noti* nằm trên DB này — BaseRepository của module tự trỏ Context về đây.
        /// </summary>
        public const string DbConfigId = "NotiDefault";

        /// <summary>Id của recurring job dọn dữ liệu hết hạn (Hangfire).</summary>
        public const string RetentionJobId = "noti-retention-scan";

        /// <summary>Cron dự phòng cho job dọn dẹp — 03:00 hằng ngày, khi SysOpConfig chưa có cấu hình.</summary>
        public const string RetentionCronFallback = "0 3 * * *";

        /// <summary>Nhàm cấu hình vận hành trên SysOpConfig (đọc qua MessBus, theo nếp Mail).</summary>
        public const string ConfigGroupCode = "noti_config";

        /// <summary>Key cấu hình cron của job dọn dẹp trên SysOpConfig.</summary>
        public const string ConfigKeyRetentionCron = "noti_timer_hangfire_scan";

        /// <summary>Số dòng NotiRecipient chèn mỗi lệnh INSERT (chống câu lệnh quá lớn).</summary>
        public const int InsertBatchSize = 1000;

        /// <summary>
        /// Ngưỡng số user nhận tín hiệu realtime: vượt quá thì broadcast KHÔNG kèm danh sách UserId
        /// (mọi client đều làm mới badge). Vừa tránh payload lớn, vừa tránh lộ danh sách người nhận.
        /// </summary>
        public const int MaxBroadcastUserIds = 200;

        /// <summary>Giá trị mặc định của NotiSettings.RetentionDays khi Notification.json thiếu.</summary>
        public const int DefaultRetentionDays = 90;

        /// <summary>Giá trị mặc định của NotiSettings.MaxRecipientsPerMessage khi Notification.json thiếu.</summary>
        public const int DefaultMaxRecipientsPerMessage = 20000;
    }
}
