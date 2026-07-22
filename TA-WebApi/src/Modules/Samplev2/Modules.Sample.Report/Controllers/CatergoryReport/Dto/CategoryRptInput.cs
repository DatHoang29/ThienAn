using Modules.Report.Base.Core.Dto;
using Shared.DTO.Dtos.Sample;

namespace Modules.Sample.Report.Controllers.CatergoryReport.Dto;

public class CategoryRenderInput :SampCategoryDto, IBaseReportInput
{
    //TODO điều kiện tìm kiếm
    public string Name { get; set; }
    public string Description { get; set; }
    public string ReportDataId { get; set; }
}


public class CategoryExportInput: CategoryRenderInput
{
    //TODO điều kiện tìm kiếm
    public string Format { get; set; }
}
