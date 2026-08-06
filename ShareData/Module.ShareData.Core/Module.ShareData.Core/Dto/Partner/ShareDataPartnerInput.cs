using Module.ShareData.Core.Entities;
using Shared.Core.Utilities;

namespace Module.ShareData.Core.Dto.Partner
{
    /// <summary>
    /// Description: DTO phân trang danh sách đối tác chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPagePartnerInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? ProtocolProfile { get; set; }
        public string? InitiatorMode { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// Description: DTO tìm kiếm danh sách đối tác chia sẻ (không phân trang)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPartnerInput
    {
        public string? ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? ProtocolProfile { get; set; }
        public string? InitiatorMode { get; set; }
        public string? Status { get; set; }

        /// <summary>Bỏ qua đối tác đã ngừng sử dụng (DISABLED). Mặc định true — khớp hành vi màn hình vận hành.</summary>
        public bool? ExcludeDisabled { get; set; } = true;
    }

    /// <summary>
    /// Description: DTO lấy chi tiết 1 đối tác theo ID
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataIdPartnerInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO thêm mới 1 đối tác. Password chỉ gửi lên, không bao giờ trả về.
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddPartnerInput : ShareDataPartner
    {
        /// <summary>Mật khẩu dạng rõ — backend băm trước khi lưu vào PasswordHash.</summary>
        public string? Password { get; set; }
    }

    /// <summary>
    /// Description: DTO cập nhật 1 đối tác. Password để trống nghĩa là giữ nguyên mật khẩu cũ.
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdatePartnerInput : ShareDataAddPartnerInput { }

    /// <summary>
    /// Description: DTO xóa 1 đối tác (xóa mềm)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeletePartnerInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO mở phiên kết nối tới đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataConnectPartnerInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO ngắt phiên kết nối với đối tác
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDisconnectPartnerInput : BaseIdInput
    {
        /// <summary>Lý do ngắt (logout reason-cd). Mặc định 'clientRequested'.</summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Description: DTO thử kết nối tới đối tác (Initiate + Login + Logout)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataTestConnectionPartnerInput : BaseIdInput { }
}
