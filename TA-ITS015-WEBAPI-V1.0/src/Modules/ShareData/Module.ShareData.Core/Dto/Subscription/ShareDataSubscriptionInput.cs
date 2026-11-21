using Module.ShareData.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.ShareData.Core.Dto.Subscription
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPageSubscriptionInput : BasePageInput
    {
        public string? PartnerId { get; set; }
        public BaseEnums.Direction? Direction { get; set; }
        public BaseEnums.SubSubscriptionState? State { get; set; }
        public string? DatatypeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataSubscriptionInput
    {
        public string? ID { get; set; }
        public string? PartnerId { get; set; }
        public BaseEnums.Direction? Direction { get; set; }
        public BaseEnums.SubSubscriptionState? State { get; set; }
        public string? DatatypeId { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 đăng ký theo ID
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataIdSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 đăng ký chia sẻ.
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddSubscriptionInput : ShareDataSubscription
    {
        /// <summary>Lịch gửi dạng đối tượng — backend serialize vào ScheduleJson.</summary>
        public ShareDataScheduleDto? Schedule { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 đăng ký chia sẻ.
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdateSubscriptionInput : ShareDataAddSubscriptionInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 đăng ký
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeleteSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tắt tạm 1 đăng ký (ACTIVE → PAUSED)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPauseSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO bật lại 1 đăng ký (PAUSED → ACTIVE)
    /// Created date: 2026-08-04
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class ShareDataResumeSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO hủy 1 đăng ký (kèm cancel reason-cd)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataCancelSubscriptionInput : BaseIdInput
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO duyệt 1 đăng ký INBOUND (PENDING → ACTIVE)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataApproveSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO từ chối 1 đăng ký INBOUND (kèm reject reason-cd)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataRejectSubscriptionInput : BaseIdInput
    {
        public string? Reason { get; set; }
    }
}
