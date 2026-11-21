namespace Module.ShareData.Core.Dto.Inbound
{
    /// <summary>
    /// Author: HieuNV
    /// Description: 3 header định danh gói tin luồng NHẬN — đúng tên với DataOutboundRestSender.AddIdentityHeaders
    ///              bên gửi (PartnerCode, PacketCode, SerialNbr). Tên property trùng tên header nên
    ///              ASP.NET Core tự bind khi gắn [FromHeader] lên tham số kiểu này, không cần khai Name riêng lẻ.
    /// Created date: 20/09/2026
    /// </summary>
    public class ShareDataInboundIdentityHeader
    {
        public string? PartnerCode { get; set; }
        public string? PacketCode { get; set; }
        public int? SerialNbr { get; set; }
    }
}
