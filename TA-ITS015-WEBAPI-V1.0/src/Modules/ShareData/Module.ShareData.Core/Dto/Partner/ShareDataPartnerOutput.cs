using Module.ShareData.Core.Entities;

namespace Module.ShareData.Core.Dto.Partner
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 đối tác (phân trang)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPagePartnerOutput : ShareDataPartner { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPartnerOutput : ShareDataPartner { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi thêm mới 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddPartnerOutput : ShareDataPartnerOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi cập nhật 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdatePartnerOutput : ShareDataPartnerOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về sau khi xóa 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeletePartnerOutput : ShareDataPartnerOutput { }
}
