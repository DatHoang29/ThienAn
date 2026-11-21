using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.EventRule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageEventRuleInput : BasePageInput
    {
        public string? EventTypeId { get; set; }
        public string? TargetSceneId { get; set; }
        public string? EventSource { get; set; }
        public string? Priority { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách VwEventRule (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwEventRuleInput
    {
        public string? ID { get; set; }
        public string? EventTypeId { get; set; }
        public string? TargetSceneId { get; set; }
        public string? EventSource { get; set; }
        public string? Priority { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 VwEventRule theo ID
    /// Created date: 2026-08-01
    /// </summary>
    public class VwIdEventRuleInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddEventRuleInput : VwEventRule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateEventRuleInput : VwAddEventRuleInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteEventRuleInput : BaseIdInput { }
}
