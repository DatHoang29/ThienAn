using Modules.Report.Base.Core.Dto;

namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoLoTrinh
{
    public class LLTLTRenderInput : BaseReportInput
    {
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string Tram { get; set; }
        public string Huong { get; set; }
    }
    public class LLTLTExportInput : LLTLTRenderInput
    {
        public string Format { get; set; }
    }
}
