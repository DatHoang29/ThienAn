using Modules.Notification.Core.Constants;

namespace Module.Notification.Infrastructure.Services.Config
{
    /// <summary>
    /// Author: SonTH
    /// Description: Cấu hình tĩnh của phân hệ Notification, nạp từ Configuration/Notification.json.
    ///              Chu kỳ chạy job (cron) KHÔNG nằm đây — đọc từ bảng SysOpConfig qua
    ///              NotiConfigService theo đúng nếp Module.Mail (sửa cron không phải deploy lại).
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiSettings
    {
        public const string SectionName = "Notification";

        /// <summary>Số ngày giữ thông báo tính từ SendTime. Mặc định 90.</summary>
        public int RetentionDays { get; set; } = NotiConst.DefaultRetentionDays;

        /// <summary>Trần số người nhận của một lần gửi — chặn fan-out bất thường. Mặc định 20000.</summary>
        public int MaxRecipientsPerMessage { get; set; } = NotiConst.DefaultMaxRecipientsPerMessage;

        /// <summary>Bật/tắt broadcast realtime. Tắt thì FE tự poll unread-count.</summary>
        public bool EnableBroadcast { get; set; } = true;
    }
}
