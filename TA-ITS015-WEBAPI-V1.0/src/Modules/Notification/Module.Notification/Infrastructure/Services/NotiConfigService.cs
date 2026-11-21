using Microsoft.Extensions.Options;
using Module.Notification.Infrastructure.Services.Config;
using Modules.CfgSystem.Core.Dto.OpConfig;
using Modules.Notification.Core.Constants;
using Modules.Notification.Core.Entities;
using Modules.Notification.Core.Messages;
using Wolverine;

namespace Modules.Notification.Infrastructure.Services
{
    /// <summary>
    /// Author: SonTH
    /// Description: Cấu hình vận hành của phân hệ Notification.
    ///
    ///              RetentionDays/MaxRecipients đọc từ Notification.json (IOptions),
    ///              cron của job dọn dẹp đọc từ bảng SysOpConfig qua MessBus — làm theo đúng
    ///              nếp Module.Mail/MailConfigService: hỏi qua bus chứ không tự mở repository
    ///              sang bảng của phân hệ khác. MỌI HÀM ĐỀU CÓ GIÁ TRỊ MẶC ĐỊNH, không ném lỗi:
    ///              chặn job vì thiếu một bản ghi cấu hình là hỏng to hơn.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiConfigService : IScoped
    {
        private readonly IMessageBus _bus;
        private readonly NotiSettings _settings;

        public NotiConfigService(IMessageBus bus, IOptions<NotiSettings> settings)
        {
            _bus = bus;
            _settings = settings.Value;
        }

        /// <summary>Trần số người nhận của một lần gửi (Notification.json).</summary>
        public int MaxRecipientsPerMessage => _settings.MaxRecipientsPerMessage;

        /// <summary>Bật/tắt broadcast realtime (Notification.json).</summary>
        public bool EnableBroadcast => _settings.EnableBroadcast;

        /// <summary>Số ngày giữ thông báo mặc định (Notification.json).</summary>
        public int RetentionDays => _settings.RetentionDays;

        /// <summary>
        /// Author: SonTH
        /// Description: Lấy toàn bộ cấu hình nhóm noti_config dạng Dictionary (Key = Code).
        ///              Một lần gọi bus cho mọi key, không gọi lẻ từng cái.
        /// Created date: 17/09/2026
        /// </summary>
        public async Task<Dictionary<string, string>> GetGroupAsync()
        {
            var result = await _bus.InvokeAsync<List<SysOpConfigOutput>>(
                new SysOpConfigInput { GroupCode = NotiConst.ConfigGroupCode });

            // Bản ghi thiếu Code là dữ liệu rác, bỏ luôn — để lọt vào đây thì ToDictionary sẽ ném lỗi
            return result?
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code!)
                .ToDictionary(g => g.Key, g => g.Last().Value ?? string.Empty)
                ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Biểu thức cron của job dọn dẹp.
        ///
        ///              LƯU Ý VẬN HÀNH: giá trị chỉ được đọc MỘT LẦN lúc khởi động API, trong
        ///              Startup.Configure. Sửa key trong SysOpConfig xong phải khởi động lại API
        ///              mới ăn — job rpt-email-scan cũng đúng như vậy.
        /// Created date: 17/09/2026
        /// </summary>
        public async Task<string> GetRetentionCronAsync()
        {
            var configs = await GetGroupAsync();

            return configs.TryGetValue(NotiConst.ConfigKeyRetentionCron, out var value)
                   && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : NotiConst.RetentionCronFallback;
        }
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Nhóm dữ liệu người nhận sau khi resolve — bản nội bộ của handler gửi.
    ///              Chỉ cần UserId: bảng NotiRecipient không lưu snapshot user/org.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiUserSeed
    {
        public string UserId { get; set; } = null!;
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Resolve danh sách người nhận cho một SendNotificationCommand.
    /// Created date: 17/09/2026
    /// </summary>
    public interface INotiUserResolver
    {
        Task<List<NotiUserSeed>> ResolveAsync(SendNotificationCommand command);
    }
}
