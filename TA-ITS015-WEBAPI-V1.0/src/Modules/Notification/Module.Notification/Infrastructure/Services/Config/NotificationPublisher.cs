using Modules.Notification.Core.Enums;
using Modules.Notification.Core.Messages;
using Wolverine;

namespace Module.Notification.Infrastructure.Services.Config
{
    /// <summary>
    /// Author: SonTH
    /// Description: Facade gửi thông báo cho các module khác — bọc IMessageBus để caller
    ///              chỉ cần inject MỘT dependency và không phải dựng command tay.
    ///
    ///              Ưu tiên dùng class này thay vì gọi IMessageBus trực tiếp khi không cần
    ///              custom target phức tạp. Muốn kiểm soát toàn bộ tham số thì tự dựng
    ///              SendNotificationCommand rồi gọi PublishAsync.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotificationPublisher : IScoped
    {
        private readonly IMessageBus _bus;

        public NotificationPublisher(IMessageBus bus)
        {
            _bus = bus;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gửi với toàn bộ tham số tự dựng — lối thoát cho trường hợp đặc biệt.
        /// Created date: 17/09/2026
        /// </summary>
        public Task<SendNotificationResult> PublishAsync(SendNotificationCommand command)
        {
            return _bus.InvokeAsync<SendNotificationResult>(command);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gửi riêng cho từng người dùng.
        /// Created date: 17/09/2026
        /// </summary>
        public Task<SendNotificationResult> ToUsersAsync(
            string title, string content, string category, IEnumerable<string> userIds,
            string? sourceKey = null,
            string? severity = null, string? idempotencyKey = null)
        {
            return PublishAsync(new SendNotificationCommand
            {
                Title = title,
                Content = content,
                Category = category,
                Severity = severity ?? NotiEnums.SeverityEnum.Info.ToString(),
                TargetType = NotiEnums.TargetTypeEnum.User,
                UserIds = userIds?.ToList(),
                SourceKey = sourceKey,
                IdempotencyKey = idempotencyKey,
            });
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gửi cho org (mặc định gồm cây con).
        /// Created date: 17/09/2026
        /// </summary>
        public Task<SendNotificationResult> ToOrgAsync(
            string title, string content, string category, IEnumerable<string> orgIds,
            string? sourceKey = null,
            string? severity = null, string? idempotencyKey = null,
            bool includeChildren = true)
        {
            return PublishAsync(new SendNotificationCommand
            {
                Title = title,
                Content = content,
                Category = category,
                Severity = severity ?? NotiEnums.SeverityEnum.Info.ToString(),
                TargetType = NotiEnums.TargetTypeEnum.Org,
                OrgIds = orgIds?.ToList(),
                IncludeChildren = includeChildren,
                SourceKey = sourceKey,
                IdempotencyKey = idempotencyKey,
            });
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gửi toàn hệ thống — fan-out mọi user active tại thời điểm gửi.
        /// Created date: 17/09/2026
        /// </summary>
        public Task<SendNotificationResult> ToAllAsync(
            string title, string content, string category,
            string? sourceKey = null,
            string? severity = null, string? idempotencyKey = null)
        {
            return PublishAsync(new SendNotificationCommand
            {
                Title = title,
                Content = content,
                Category = category,
                Severity = severity ?? NotiEnums.SeverityEnum.Info.ToString(),
                TargetType = NotiEnums.TargetTypeEnum.All,
                SourceKey = sourceKey,
                IdempotencyKey = idempotencyKey,
            });
        }
    }
}
