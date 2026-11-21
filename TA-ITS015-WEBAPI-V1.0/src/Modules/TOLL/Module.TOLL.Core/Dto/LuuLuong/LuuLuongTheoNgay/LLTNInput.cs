using Modules.Report.Base.Core.Dto;

namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay
{
    public class LLTNRenderInput : BaseReportInput
    {
        public DateTime Ngay { get; set; }
        public string Tram { get; set; }
        public string Huong { get; set; }
    }

    public class LLTNExportInput : LLTNRenderInput
    {
        public string Format { get; set; }
    }
}
