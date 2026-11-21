using Modules.Report.Base.Core.Dto;

namespace Modules.Sample.Report.Controllers.FastDemo.Dto;

public class FastDemoRenderInput:BaseReportInput
{
    //TODO điều kiện tìm kiếm
    public string Name { get; set; }
    public string Code { get; set; }
}


public class FastDemoExportInput:FastDemoRenderInput
{
    //TODO điều kiện tìm kiếm
    public string Format { get; set; }
}
