using Modules.Report.Base.Core.Dto;

namespace Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoNgay
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tham số render báo cáo doanh thu theo ngày (DTTN)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTNRenderInput : BaseReportInput
    {
        public DateTime Ngay { get; set; }
        public string? Tram { get; set; }
        public string? Huong { get; set; }   // 1 = vào, 2 = ra
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tham số export báo cáo doanh thu theo ngày (DTTN)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTNExportInput : DTTNRenderInput
    {
        public string? Format { get; set; }
    }
}
