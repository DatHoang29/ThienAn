using Module.ShareData.Core.Entities;

namespace Module.ShareData.Core.Dto.Mapping
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 phễu lọc 
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataMappingOutput : ShareDataMapping
    {
        public string? PartnerName { get; set; }
        public string? PartnerCode { get; set; }
        public string? PacketName { get; set; }
        public int MappedFieldCount { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 phễu lọc (phân trang).
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPageMappingOutput : ShareDataMappingOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataAddMappingOutput : ShareDataMappingOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataUpdateMappingOutput : ShareDataMappingOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataDeleteMappingOutput : ShareDataMappingOutput { }
}
