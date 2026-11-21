using Modules.Report.Base.Core.Dto;

namespace Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoNam
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tham số render báo cáo doanh thu theo năm (DTTNam)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTNamRenderInput : BaseReportInput
    {
        public int Nam { get; set; }
        public string? Tram { get; set; }
        public string? Huong { get; set; }   // 1 = vào, 2 = ra
    }

    public class DTTNamExportInput : DTTNamRenderInput
    {
        public string? Format { get; set; }
    }
}
