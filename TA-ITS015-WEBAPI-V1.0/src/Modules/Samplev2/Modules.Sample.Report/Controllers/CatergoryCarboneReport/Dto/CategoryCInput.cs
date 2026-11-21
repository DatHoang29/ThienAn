using Modules.Report.Base.Core.Dto;
using Shared.DTO.Dtos.Sample;

namespace Modules.Sample.Report.Controllers.CatergoryCarboneReport.Dto;

public class CategoryCRenderInput :SampCategoryDto, IBaseReportInput
{
    //TODO điều kiện tìm kiếm
    public string Name { get; set; }
    public string Description { get; set; }
    public string ReportDataId { get; set; }
}


public class CategoryCExportInput: CategoryCRenderInput
{
    //TODO điều kiện tìm kiếm
    public string Format { get; set; }
}
