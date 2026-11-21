using Modules.Report.Base.Core.Dto;

namespace Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoQuy
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tham số render báo cáo doanh thu theo quý (DTTQ)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTQRenderInput : BaseReportInput
    {
        public int Quy { get; set; }
        public int Nam { get; set; }
        public string? Tram { get; set; }
        public string? Huong { get; set; }   // 1 = vào, 2 = ra
    }

    public class DTTQExportInput : DTTQRenderInput
    {
        public string? Format { get; set; }
    }
}
