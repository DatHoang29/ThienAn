using Modules.Report.Base.Core.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoThang
{
    public class LLTTRenderInput : BaseReportInput
    {
        public int Nam { get; set; }
        public int Thang { get; set; }
        public string Tram { get; set; }
        public string Huong { get; set; }
    }
    public class LLTTExportInput : LLTTRenderInput
    {
        public string Format { get; set; }
    }
}
