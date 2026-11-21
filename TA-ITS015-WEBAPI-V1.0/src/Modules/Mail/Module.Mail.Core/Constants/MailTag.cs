using System.Text.RegularExpressions;
using Modules.Mail.Core.Dto.Template;

namespace Modules.Mail.Core.Constants
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Danh mục tag hệ thống của email (nhãn, nhóm, mẫu, mô tả). Giao diện lấy qua API
    ///              RptEmailSchedule/Tags để liệt kê cho người dùng kéo thả vào tiêu đề và nội dung thư (wangEditor),
    ///              không tự khai lại danh sách.
    ///              Mã tag: MailConst.Placeholder. Cách tính giá trị: MailBodyRenderer.SystemTagValues.
    ///              Thêm tag: thêm hằng ở Placeholder + một dòng ở All + một dòng ở SystemTagValues.
    /// Created date: 29/09/2026
    /// </summary>
    public static class MailTag
    {
        public const string GroupCommon = "Chung";
        public const string GroupReport = "Báo cáo";

        /// <summary>Nhận diện mọi chuỗi dạng {TenTag}.</summary>
        public static readonly Regex Pattern = new(@"\{[A-Za-z]+\}", RegexOptions.Compiled);

        public static readonly IReadOnlyList<MailTagInfo> All = new List<MailTagInfo>
        {
            Tag(MailConst.Placeholder.TenLich, "Tên lịch", GroupCommon, "Báo cáo tháng gửi BGĐ"),
            Tag(MailConst.Placeholder.SoBaoCao, "Số báo cáo", GroupCommon, "2",
                "Số tệp đính kèm thực tế trong thư."),
            Tag(MailConst.Placeholder.NgayGui, "Ngày gửi", GroupCommon, "01/09/2026"),
            Tag(MailConst.Placeholder.GioGui, "Giờ gửi", GroupCommon, "08:00"),

            // Tính trên TOÀN BỘ báo cáo trong thư — không có tag theo một báo cáo vì thư có thể gồm nhiều báo cáo.
            Tag(MailConst.Placeholder.KyBaoCao, "Kỳ báo cáo", GroupReport, "Tháng 08/2026",
                "Các kỳ dữ liệu trong thư, trùng thì in một lần. VD \"Tháng 08/2026\" hoặc \"Ngày 31/08/2026, Tháng 08/2026\"."),
            Tag(MailConst.Placeholder.DanhSachBaoCao, "Danh sách báo cáo", GroupReport, "Lưu lượng đầu vào theo tháng, Doanh thu theo tháng",
                "Tên các báo cáo đính kèm, ngăn bằng dấu phẩy."),
            Tag(MailConst.Placeholder.NhomBaoCao, "Nhóm báo cáo", GroupReport, "Tích hợp TOLL",
                "Tên các nhóm (hệ) có báo cáo trong thư, VD dùng cho tiêu đề \"[{NhomBaoCao}] Báo cáo định kỳ\"."),
        };

        public static readonly HashSet<string> Codes = All.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);

        public static MailTagInfo? Find(string code) => All.FirstOrDefault(x => x.Code == code);

        /// <summary>Tag lạ (không có trong danh mục) xuất hiện trong chuỗi — chỉ để cảnh báo ở bản xem trước.</summary>
        public static List<string> FindUnknown(string? text)
            => string.IsNullOrEmpty(text)
                ? new List<string>()
                : Pattern.Matches(text).Select(m => m.Value).Distinct(StringComparer.Ordinal).Where(x => !Codes.Contains(x)).ToList();

        private static MailTagInfo Tag(string code, string label, string group, string sample, string? desc = null)
            => new() { Code = code, Label = label, Group = group, Sample = sample, Description = desc };
    }
}
