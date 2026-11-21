using Module.ShareData.Core.Entities;

namespace Module.ShareData.Core.Dto.CodeSet
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 bộ mã (dùng chung cho danh sách / chi tiết / sau khi ghi).
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataCodeSetOutput : ShareDataCodeSet
    {
        public int ValueCount { get; set; }

        /// <summary>Bảng quy đổi đã giải mã từ ValuesJson.</summary>
        public List<ShareDataCodeSetValueDto>? Values { get; set; }

        /// <summary>Giá trị nội bộ mặc định — đã giải mã từ ValuesJson.</summary>
        public string? DefaultSourceValue { get; set; }

        /// <summary>Giá trị đối tác mặc định — đã giải mã từ ValuesJson.</summary>
        public string? DefaultPartnerValue { get; set; }

        /// <summary>
        /// Mã hồ sơ ánh xạ đang dùng (đang bật + có đăng ký Active) tham chiếu bộ mã 
        /// </summary>
        public List<string>? InUseMappings { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 bộ mã (phân trang).
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPageCodeSetOutput : ShareDataCodeSetOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataAddCodeSetOutput : ShareDataCodeSetOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataUpdateCodeSetOutput : ShareDataCodeSetOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataDeleteCodeSetOutput : ShareDataCodeSetOutput { }
}
