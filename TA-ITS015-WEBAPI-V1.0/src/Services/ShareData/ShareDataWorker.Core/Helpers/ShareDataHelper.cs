using Module.ShareData.Core.Entities;
using ShareDataWorker.Core.Enums;

namespace ShareDataWorker.Core.Helpers
{
    /// <summary>
    /// Tiện ích hỗ trợ xử lý thông tin Subscription.
    /// </summary>
    public static class ShareDataHelper
    {
        /// <summary>
        /// Description: Format chuỗi mô tả ngắn gọn cho Subscription để log.
        /// Created date: 07/08/2026
        /// </summary>
        public static string DescribeSubscription(ShareDataSubscription sub, ShareDataPartner? partner)
        {
            var packet = string.IsNullOrWhiteSpace(sub.DatatypeId) ? ShareDataEnum.SystemConstants.Unknown : sub.DatatypeId.Trim();
            var subCode = string.IsNullOrWhiteSpace(sub.Code) ? sub.ID : sub.Code.Trim();
            var partnerLabel = partner?.Code ?? partner?.Name ?? sub.PartnerId ?? ShareDataEnum.SystemConstants.Unknown;

            return $"gói {packet} | đăng ký {subCode} | đối tác {partnerLabel} | ID {sub.ID}";
        }

        /// <summary>
        /// Description: Nhãn dễ hiểu cho thông báo hiển thị trên giao diện: chỉ tên đối tác, không đưa ID nội bộ của gói tin, đăng ký hay đối tác.
        ///              Nhãn đầy đủ (DescribeSubscription) chỉ dành cho nhật ký máy chủ.
        /// Created date: 22/10/2026
        /// </summary>
        public static string DescribeSubscriptionForUser(ShareDataSubscription sub, ShareDataPartner? partner)
        {
            var partnerLabel = partner?.Name ?? partner?.Code ?? ShareDataEnum.SystemConstants.Unknown;
            return $"đối tác {partnerLabel}";
        }

        /// <summary>
        /// Description: Cắt bỏ phần mili-giây của DateTime để làm tròn chẵn giây,
        ///              đảm bảo khớp chính xác khi so khớp kiểu datetime trong CSDL.
        /// Created date: 28/09/2026
        /// </summary>
        public static DateTime TruncateToSeconds(this DateTime dt) => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
    }
}
