using Modules.Report.Base.Core.Dto;

namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNam
{
    public class LLTNamRenderInput : BaseReportInput
    {
        public int Nam { get; set; }
        public string Tram { get; set; }
        public string Huong { get; set; }
    }
    public class LLTNamExportInput : LLTNamRenderInput
    {
        public string Format { get; set; }
    }
}
