using Modules.Report.Base.Core.Dto;

namespace Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoThoiGian
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tham số render báo cáo doanh thu theo thời gian (DTTTG)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTTGRenderInput : BaseReportInput
    {
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string? Tram { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tham số export báo cáo doanh thu theo thời gian (DTTTG)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTTGExportInput : DTTTGRenderInput
    {
        public string? Format { get; set; }
    }
}
