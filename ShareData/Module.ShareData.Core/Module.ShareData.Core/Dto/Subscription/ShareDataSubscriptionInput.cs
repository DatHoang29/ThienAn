using Module.ShareData.Core.Entities;
using Shared.Core.Utilities;

namespace Module.ShareData.Core.Dto.Subscription
{
    /// <summary>
    /// Description: DTO phân trang danh sách đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPageSubscriptionInput : BasePageInput
    {
        public string? PartnerId { get; set; }
        public string? Direction { get; set; }
        public string? State { get; set; }
        public string? Mode { get; set; }
        public int? DatatypeId { get; set; }

        /// <summary>Lọc theo khoảng thời gian yêu cầu đăng ký.</summary>
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Description: DTO tìm kiếm danh sách đăng ký chia sẻ (không phân trang)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataSubscriptionInput
    {
        public string? ID { get; set; }
        public string? PartnerId { get; set; }
        public string? Direction { get; set; }
        public string? State { get; set; }
        public string? Mode { get; set; }
        public int? DatatypeId { get; set; }
    }

    /// <summary>
    /// Description: DTO lấy chi tiết 1 đăng ký theo ID
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataIdSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO thêm mới 1 đăng ký chia sẻ.
    ///              SerialNbr / State / RequestedAt do backend sinh, client gửi lên sẽ bị bỏ qua.
    ///              MappingProfileId / DataSourceId backend tự suy lại, không tin giá trị client gửi.
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddSubscriptionInput : ShareDataSubscription
    {
        /// <summary>Lịch gửi dạng đối tượng — backend serialize vào ScheduleJson.</summary>
        public ShareDataScheduleDto? Schedule { get; set; }
    }

    /// <summary>
    /// Description: DTO cập nhật 1 đăng ký chia sẻ.
    ///              Không cho đổi PartnerId / DatatypeId / Direction / SerialNbr / State.
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdateSubscriptionInput : ShareDataAddSubscriptionInput { }

    /// <summary>
    /// Description: DTO xóa 1 đăng ký (xóa mềm)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeleteSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO tắt tạm 1 đăng ký (ACTIVE → PAUSED)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPauseSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO bật lại 1 đăng ký (PAUSED → ACTIVE)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataResumeSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO hủy 1 đăng ký (kèm cancel reason-cd)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataCancelSubscriptionInput : BaseIdInput
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Description: DTO duyệt 1 đăng ký INBOUND (PENDING → ACTIVE)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataApproveSubscriptionInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO từ chối 1 đăng ký INBOUND (kèm reject reason-cd)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataRejectSubscriptionInput : BaseIdInput
    {
        public string? Reason { get; set; }
    }
}
