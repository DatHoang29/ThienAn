using Modules.Notification.Controllers.Notification.Commands;
using Modules.Notification.Controllers.Notification.Dto;
using Modules.Notification.Core.Messages;
using Modules.Notification.Infrastructure.Services;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Notification.Controllers.Notification
{
    /// <summary>
    /// Author: SonTH
    /// Description: API thông báo cho frontend: danh sách, badge unread, mark-read, xoá, chi tiết
    ///              và lệnh gửi (dùng cho màn admin phát thông báo; module khác gọi trực tiếp
    ///              qua IMessageBus — xem SendNotificationCommand).
    ///
    ///              Mọi endpoint đều scope theo user hiện tại từ claims, không nhận userId từ FE.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiController : BaseController
    {
        /// <summary>
        /// Author: SonTH
        /// Description: Danh sách thông báo của user hiện tại theo phân trang
        /// Created date: 17/09/2026
        /// </summary>
        [ApiDescriptionSettings(Name = "Page"), HttpPost]
        [DisplayName("Get notification page list")]
        public async Task<SqlSugarPagedList<NotiListOutput>> Page(NotiPageInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<NotiListOutput>>(input);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Số thông báo chưa đọc của user hiện tại (badge)
        /// Created date: 17/09/2026
        /// </summary>
        [ApiDescriptionSettings(Name = "UnreadCount"), HttpPost]
        [DisplayName("Get unread notification count")]
        public async Task<NotiUnreadCountOutput> UnreadCount(NotiUnreadCountInput input)
        {
            return await MessBus.InvokeAsync<NotiUnreadCountOutput>(input);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Chi tiết một thông báo theo Id dòng người nhận
        /// Created date: 17/09/2026
        /// </summary>
        [ApiDescriptionSettings(Name = "Detail"), HttpPost]
        [DisplayName("Get notification detail")]
        public async Task<NotiDetailOutput> GetDetail(NotiIdInput input)
        {
            return await MessBus.InvokeAsync<NotiDetailOutput>(input);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Đánh dấu đã đọc — theo Ids hoặc tất cả (All = true)
        /// Created date: 17/09/2026
        /// </summary>
        [ApiDescriptionSettings(Name = "MarkRead"), HttpPost]
        [DisplayName("Mark notifications as read")]
        public async Task<bool> MarkRead(NotiMarkReadInput input)
        {
            return await MessBus.InvokeAsync<bool>(input);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Xoá các thông báo của chính mình (soft-delete theo dòng người nhận)
        /// Created date: 17/09/2026
        /// </summary>
        [ApiDescriptionSettings(Name = "Delete"), HttpPost]
        [DisplayName("Delete my notifications")]
        public async Task<bool> Delete(NotiDeleteInput input)
        {
            return await MessBus.InvokeAsync<bool>(input);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gửi một thông báo (màn admin phát thông báo; module khác gọi qua
        ///              IMessageBus/Publisher chứ không qua HTTP này).
        /// Created date: 17/09/2026
        /// </summary>
        [ApiDescriptionSettings(Name = "Send"), HttpPost]
        [DisplayName("Send a notification")]
        public async Task<SendNotificationResult> Send(SendNotificationCommand input)
        {
            return await MessBus.InvokeAsync<SendNotificationResult>(input);
        }
    }
}
