using Module.ShareData.Core.Entities;
using Shared.Core.Utilities;

namespace Module.ShareData.Core.Dto.Packet
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách gói tin.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPagePacketInput : BasePageInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }

        /// <summary>
        /// Lọc theo đối tác: chỉ trả các gói đối tác đang đăng ký gửi hoặc đã có SQL riêng, kèm cờ nguồn SQL (chỉ SuperAdmin).
        /// </summary>
        public string? PartnerId { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách gói tin (không phân trang — dùng cho dropdown chọn gói tin)
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPacketInput
    {
        public string? ID { get; set; }
        public string? Code { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 gói tin theo ID
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataIdPacketInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 gói tin.
    /// Created date: 2026-09-17
    /// </summary>
    public class ShareDataAddPacketInput : ShareDataPacket { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 gói tin
    /// Created date: 2026-09-17
    /// </summary>
    public class ShareDataUpdatePacketInput : ShareDataAddPacketInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 gói tin (xóa mềm)
    /// Created date: 2026-09-17
    /// </summary>
    public class ShareDataDeletePacketInput : BaseIdInput { }
}
