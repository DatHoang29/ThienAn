using Module.ShareData.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.ShareData.Core.Dto.Partner
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách đối tác chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPagePartnerInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
        public BaseEnums.SessionState? SessionState { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách đối tác chia sẻ (không phân trang)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPartnerInput
    {
        public string? ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
        public BaseEnums.SessionState? SessionState { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 đối tác theo ID
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataIdPartnerInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddPartnerInput : ShareDataPartner
    {
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdatePartnerInput : ShareDataAddPartnerInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeletePartnerInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO mở phiên kết nối tới đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataConnectPartnerInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO ngắt phiên kết nối với đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDisconnectPartnerInput : BaseIdInput
    {
        /// <summary>Lý do ngắt (logout reason-cd). Mặc định 'clientRequested'.</summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thử kết nối tới đối tác (Initiate + Login + Logout)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataTestConnectionPartnerInput : BaseIdInput { }
}
