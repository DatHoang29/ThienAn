using Module.ShareData.Core.Entities;

namespace Module.ShareData.Core.Dto.Session
{
    /// <summary>
    /// Description: DTO trả về 1 phiên kết nối (kèm tên đối tác để tiện hiển thị)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataSessionOutput : ShareDataSession
    {
        /// <summary>Tên đối tác — trường dẫn xuất, không lưu DB.</summary>
        public string? PartnerName { get; set; }

        /// <summary>Mã đối tác — trường dẫn xuất, không lưu DB.</summary>
        public string? PartnerCode { get; set; }
    }
}
