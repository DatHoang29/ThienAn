namespace Modules.Mail.Core.Dto.Template
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Thông tin một tag hệ thống, trả cho giao diện dựng danh sách tag kéo thả.
    /// Created date: 29/09/2026
    /// </summary>
    public class MailTagInfo
    {
        /// <summary>Tag đầy đủ kèm ngoặc, VD "{KyBaoCao}". Giao diện chèn nguyên chuỗi này.</summary>
        public string Code { get; set; } = null!;

        /// <summary>Tên tag bỏ ngoặc, VD "KyBaoCao".</summary>
        public string Key => Code.Trim('{', '}');

        /// <summary>Nhãn hiển thị.</summary>
        public string Label { get; set; } = null!;

        /// <summary>Nhóm: Chung, Báo cáo.</summary>
        public string Group { get; set; } = null!;

        /// <summary>Giá trị mẫu, dùng cho bản xem trước khi chưa có dữ liệu thật.</summary>
        public string? Sample { get; set; }

        /// <summary>Giải thích ngắn hiện ở tooltip.</summary>
        public string? Description { get; set; }
    }
}
