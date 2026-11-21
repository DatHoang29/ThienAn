using Module.ShareData.Core.Entities;
using Shared.DTO.Enums;

namespace Module.ShareData.Core.Dto.Subscription
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataSubscriptionOutput : ShareDataSubscription
    {
        public string? PartnerName { get; set; }
        public string? PartnerCode { get; set; }
        public ShareDataScheduleDto? Schedule { get; set; }

        /// <summary>
        /// Tình trạng hồ sơ ánh xạ của đăng ký này — SUY SỐNG lúc truy vấn theo
        /// </summary>
        public BaseEnums.SubMappingState MappingState { get; set; } = BaseEnums.SubMappingState.NoMapping;

        /// <summary>
        /// Mã hồ sơ ánh xạ ứng với MappingState.
        /// </summary>
        public string? MappingCode { get; set; }

        /// <summary>Tên hồ sơ ánh xạ — suy sống cùng lúc với MappingState.</summary>
        public string? MappingName { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 đăng ký chia sẻ (phân trang)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPageSubscriptionOutput : ShareDataSubscriptionOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 đăng ký
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddSubscriptionOutput : ShareDataSubscriptionOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 đăng ký
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdateSubscriptionOutput : ShareDataSubscriptionOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 đăng ký
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeleteSubscriptionOutput : ShareDataSubscriptionOutput { }
}
