using Module.ShareData.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.ShareData.Core.Dto.Mapping
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPageMappingInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? PartnerId { get; set; }
        public string? DatatypeId { get; set; }
        public BaseEnums.Direction? Direction { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách phễu lọc (không phân trang — dùng cho dropdown, xuất báo cáo)
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataMappingInput
    {
        public string? ID { get; set; }
        public string? Code { get; set; }
        public string? PartnerId { get; set; }
        public string? DatatypeId { get; set; }
        public BaseEnums.Direction? Direction { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 phễu lọc theo ID
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataIdMappingInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 phễu lọc.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataAddMappingInput : ShareDataMapping
    {
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 phễu lọc.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataUpdateMappingInput : ShareDataAddMappingInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 phễu lọc (xóa mềm)
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataDeleteMappingInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO bật / tắt 1 phễu lọc.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataSetActiveMappingInput : BaseIdInput
    {
        public bool IsActive { get; set; }
    }
}
