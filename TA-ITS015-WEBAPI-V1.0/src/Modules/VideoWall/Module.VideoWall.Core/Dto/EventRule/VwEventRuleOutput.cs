using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Dto.EventRule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwEventRule (phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageEventRuleOutput : VwEventRule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 VwEventRule (danh sách / chi tiết)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwEventRuleOutput : VwEventRule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddEventRuleOutput : VwEventRule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateEventRuleOutput : VwEventRule { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteEventRuleOutput : VwEventRule { }
}
