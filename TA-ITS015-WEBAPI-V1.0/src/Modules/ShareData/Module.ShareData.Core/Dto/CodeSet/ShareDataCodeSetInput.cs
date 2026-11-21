using Module.ShareData.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.ShareData.Core.Dto.CodeSet
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPageCodeSetInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataCodeSetInput
    {
        public string? ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool? IncludeValues { get; set; } = false;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 bộ mã theo ID
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataIdCodeSetInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 bộ mã.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataAddCodeSetInput : ShareDataCodeSet
    {
        public List<ShareDataCodeSetValueDto>? Values { get; set; }

        /// <summary>Giá trị nội bộ mặc định — dùng khi giá trị nhận về không khớp dòng nào trong Values.</summary>
        public string? DefaultSourceValue { get; set; }

        /// <summary>Giá trị đối tác mặc định — dùng khi giá trị gửi đi không khớp dòng nào trong Values.</summary>
        public string? DefaultPartnerValue { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 bộ mã.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataUpdateCodeSetInput : ShareDataAddCodeSetInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 bộ mã (xóa mềm).
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataDeleteCodeSetInput : BaseIdInput { }
}
