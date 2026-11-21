namespace Modules.Mail.Core.Dto.Report
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Một báo cáo ĐÃ CÓ NHÁNH KẾT XUẤT trong code — một dòng của {Phân hệ}Reports.List (ghép vào RptReportCode.Catalog).
    ///              Tên, nhóm hiển thị, bật / tắt nằm ở bảng RptEmailReport; ở đây chỉ còn phần code quyết định.
    /// Created date: 01/10/2026
    /// </summary>
    public sealed class MailRptReportInfo
    {
        /// <summary>Phân hệ sở hữu báo cáo (RptModule) — phải trùng RptEmailReport.PluginCode của dòng danh mục.</summary>
        public string Module { get; init; } = null!;

        /// <summary>Mã báo cáo, trùng mã mẫu báo cáo (frx) và RptEmailReport.Code.</summary>
        public string Code { get; init; } = null!;

        /// <summary>
        /// Kỳ dữ liệu CỐ ĐỊNH (RptPeriodType) của báo cáo — ràng buộc bởi DTO export của phân hệ báo cáo
        /// (báo cáo tháng chỉ nhận Thang/Nam...), nên khai trong code chứ không cấu hình.
        ///
        /// null = báo cáo nhận KHOẢNG NGÀY, chạy được mọi kỳ → kỳ lấy THEO TẦN SUẤT của lịch
        /// (gửi hàng tuần = số liệu tuần, hàng tháng = số liệu tháng...).
        /// Báo cáo có kỳ cố định mà lệch tần suất KHÔNG bị chặn — vẫn chạy kỳ của nó, form soạn lịch chỉ cảnh báo.
        /// </summary>
        public string? PeriodType { get; init; }

        /// <summary>Kỳ theo tần suất của lịch (không có kỳ cố định).</summary>
        public bool FollowsFrequency => PeriodType == null;
    }
}
