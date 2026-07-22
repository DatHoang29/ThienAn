using Modules.Report.Base.Core.Dto;

namespace Modules.Report.Demo.Controllers.CarboneDemo.Dto;

public class CarboneDemoRenderInput : BaseReportInput
{
    //TODO điều kiện tìm kiếm
    public string Name { get; set; }
}


public class CarboneDemoExportInput : CarboneDemoRenderInput
{
    //TODO điều kiện tìm kiếm
    public string Format { get; set; }
}
