
using Modules.WP.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;
using Shared.Utility.Helpers;

namespace Modules.WP.Controllers.TrafficLaw.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Output DTO cho tính năng quy định giao thông hệ thống WP (TrafficLaw)
/// Created date: 03/07/2026
/// </summary>
public class WpPageTrafficLawOutput : WpTrafficLaw
{

}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Traffic Law
/// Created date: 14/08/2026
/// </summary>

public class WpTrafficLawOutput : WpTrafficLaw
{
    public string? PdfName { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Upload Pdf
/// Created date: 14/08/2026
/// </summary>

public class WpUploadPdfOutput
{
    public string? FileName { get; set; }
    public string? Url { get; set; }

}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Traffic Law Detail
/// Created date: 14/08/2026
/// </summary>
public class WpTrafficLawDetailOutput : WpTrafficLaw
{
    public string? PdfStreamUrl { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Change Stattus Traffic Law
/// Created date: 14/08/2026
/// </summary>

public class WpChangeStattusTrafficLawOutput: BaseIdInput
{
    public BaseEnums.StatusEnum? IsPublish { get; set; }
}