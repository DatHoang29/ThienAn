using Module.ShareData.Core.Entities;

namespace Module.ShareData.Core.Dto.Subscription
{
    /// <summary>
    /// Description: DTO trả về 1 đăng ký chia sẻ (dùng chung cho danh sách / chi tiết / sau khi ghi)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataSubscriptionOutput : ShareDataSubscription
    {
        /// <summary>Tên đối tác — trường dẫn xuất, không lưu DB.</summary>
        public string? PartnerName { get; set; }

        /// <summary>Mã đối tác — trường dẫn xuất, không lưu DB.</summary>
        public string? PartnerCode { get; set; }

        /// <summary>Lịch gửi đã giải mã từ ScheduleJson — trường dẫn xuất, không lưu DB.</summary>
        public ShareDataScheduleDto? Schedule { get; set; }
    }

    /// <summary>
    /// Description: DTO trả về 1 đăng ký chia sẻ (phân trang)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPageSubscriptionOutput : ShareDataSubscriptionOutput { }

    /// <summary>
    /// Description: DTO trả về sau khi thêm mới 1 đăng ký
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddSubscriptionOutput : ShareDataSubscriptionOutput { }

    /// <summary>
    /// Description: DTO trả về sau khi cập nhật 1 đăng ký
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdateSubscriptionOutput : ShareDataSubscriptionOutput { }

    /// <summary>
    /// Description: DTO trả về sau khi xóa 1 đăng ký
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeleteSubscriptionOutput : ShareDataSubscriptionOutput { }
}
