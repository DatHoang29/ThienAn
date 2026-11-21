using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.Source
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwPageSourceInput : BasePageInput
    {
        public string? Name { get; set; }
        public string? ControllerId { get; set; }
        public string? SourceType { get; set; }
        public string? SignalType { get; set; }
        public int? InputPort { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
        public string? SignalStatus { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách VwSource (không phân trang)
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSourceInput
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? ControllerId { get; set; }
        public string? SourceType { get; set; }
        public string? SignalType { get; set; }
        public int? InputPort { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
        public string? SignalStatus { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 VwSource theo ID
    /// Created date: 2026-08-01
    /// </summary>
    public class VwIdSourceInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddSourceInput : VwSource { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateSourceInput : VwAddSourceInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteSourceInput : BaseIdInput { }
}
