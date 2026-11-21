using Microsoft.Extensions.Logging;
using Modules.Notification.Core.Constants;
using Modules.Notification.Core.Entities;
using Modules.DataTransporter.ITS.Infrastructure.Services;

namespace Module.Notification.Infrastructure.Services.Config
{
    /// <summary>
    /// Author: SonTH
    /// Description: Phát tín hiệu realtime về thông báo mới qua kênh Notification của
    ///              ItsDataTransporter (SocketCluster), theo đúng nếp IncidentBroadcaster.
    ///
    ///              NGUYÊN TẮC AN TOÀN: kênh là kênh shared — mọi client đều nhận được,
    ///              nên payload CHỈ chứa tín hiệu (MessageId + danh sách UserId khi nhỏ),
    ///              KHÔNG chứa Title/Content. Client nào thấy UserId của mình thì mới gọi
    ///              REST lấy nội dung (có kiểm tra quyền). Vượt ngưỡng MaxBroadcastUserIds
    ///              thì gửi không kèm danh sách — mọi client tự làm mới badge (target All).
    ///
    ///              Lỗi broadcast KHÔNG bao giờ làm fail thao tác gửi: danh sách thông báo
    ///              vẫn đúng khi FE poll/refresh. broadcastCode đổi mỗi lần phát để FE
    ///              nhận biết là biến động mới.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiBroadcaster : IScoped
    {
        private readonly ILogger<NotiBroadcaster> _logger;

        public NotiBroadcaster(ILogger<NotiBroadcaster> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Báo FE có thông báo mới cho một nhóm người nhận.
        /// Created date: 17/09/2026
        /// </summary>
        public void NotifyCreated(List<NotiRecipient> recipients, string severity)
        {
            if (recipients == null || recipients.Count == 0) return;

            try
            {
                var userIds = recipients.Select(u => u.UserId).Distinct().ToList();

                ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.Notification, new
                {
                    broadcastCode = $"{recipients[0].MessageId}-{DateTime.Now.Ticks}",
                    broadcastObject = nameof(NotiRecipient),
                    userIds = userIds.Count <= NotiConst.MaxBroadcastUserIds ? userIds : null,
                    severity,
                    broadcastDateTime = DateTime.Now.Ticks,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Broadcast notification failed — FE sẽ tự poll unread-count. MessageId={MessageId}",
                    recipients[0].MessageId);
            }
        }
    }
}
