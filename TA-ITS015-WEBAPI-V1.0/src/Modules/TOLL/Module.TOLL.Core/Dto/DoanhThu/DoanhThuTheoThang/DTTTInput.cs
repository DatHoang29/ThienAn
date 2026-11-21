using Modules.Report.Base.Core.Dto;

namespace Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoThang
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tham số render báo cáo doanh thu theo tháng (DTTT)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTTRenderInput : BaseReportInput
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public string? Tram { get; set; }
        public string? Huong { get; set; }   // 1 = vào, 2 = ra
    }

    public class DTTTExportInput : DTTTRenderInput
    {
        public string? Format { get; set; }
    }
}
