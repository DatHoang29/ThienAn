using Module.ShareData.Core.Entities;

namespace Module.ShareData.Core.Dto.AlertLog
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 cảnh báo (danh sách / chi tiết).
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataAlertLogOutput : ShareDataAlertLog
    {
        public string? PartnerName { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 cảnh báo (phân trang).
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPageAlertLogOutput : ShareDataAlertLogOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: Một dòng thống kê cảnh báo (theo mức độ hoặc theo nguồn phát)
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataAlertCountOutput
    {
        public string? Code { get; set; }
        /// <summary>Số bản ghi.</summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thống kê nhanh phục vụ các thẻ số liệu phía trên lưới cảnh báo
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataSummaryAlertLogOutput
    {
        public int Total { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public int UnackedCount { get; set; }
        public List<ShareDataAlertCountOutput> BySource { get; set; } = [];
    }
}
