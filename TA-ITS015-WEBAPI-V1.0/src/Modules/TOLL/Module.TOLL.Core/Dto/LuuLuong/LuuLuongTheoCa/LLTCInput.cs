using Modules.Report.Base.Core.Dto;

namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoCa
{
    public class LLTCRenderInput : BaseReportInput
    {
        public DateTime Ngay { get; set; }
        public string? Ca { get; set; }
        public string Tram { get; set; }
        public string Huong { get; set; }
    }
    public class LLTCExportInput : LLTCRenderInput
    {
        public string Format { get; set; }
    }
}
