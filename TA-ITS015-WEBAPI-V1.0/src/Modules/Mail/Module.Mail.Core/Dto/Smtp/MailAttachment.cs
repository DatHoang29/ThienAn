namespace Modules.Mail.Core.Dto.Smtp
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Một tệp đính kèm trong thư.
    /// Created date: 17/08/2026
    /// </summary>
    public class MailAttachment
    {
        /// <summary>Tên tệp người nhận nhìn thấy, VD "DoanhThuThang08_2026.pdf".</summary>
        public string FileName { get; set; } = null!;

        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Có giá trị = ảnh NỘI TUYẾN hiện trong thân thư (HTML trỏ tới bằng src="cid:{ContentId}"),
        /// không hiện thành tệp đính kèm. Rỗng = tệp đính kèm thường.
        /// </summary>
        public string? ContentId { get; set; }

        /// <summary>Kiểu MIME, VD "image/png". Rỗng thì MimeKit tự đoán theo đuôi tên tệp.</summary>
        public string? ContentType { get; set; }

        public MailAttachment() { }

        public MailAttachment(string fileName, byte[] content)
        {
            FileName = fileName;
            Content = content;
        }
    }
}
