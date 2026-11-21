

using Microsoft.AspNetCore.Http;
using Modules.WP.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.WP.Controllers.TrafficLaw.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Input DTO cho tính năng quy định giao thông hệ thống WP (TrafficLaw)
/// Created date: 03/07/2026
/// </summary>
public class WpPageTrafficLawInput : BasePageInput
{

    public string? Title { get; set; }
    
    public string? Author { get; set; }

    public BaseEnums.StatusEnum? IsPublish { get; set; }

    public string? Code { get; set; }
    public DateTime? PublishTime { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Traffic Law
/// Created date: 14/08/2026
/// </summary>

public class WpTrafficLawInput
{
    public string? Title { get; set; }

    public string? Author { get; set; }

    public BaseEnums.StatusEnum? IsPublish { get; set; }

    public string? Code { get; set; }
    public DateTime? PublishTime { get; set; }

}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Traffic Law Detail
/// Created date: 14/08/2026
/// </summary>

public class WpTrafficLawDetailInput : BaseIdInput
{
    public BaseEnums.StatusEnum? IsPublish { get; set; }

}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Add Traffic Law
/// Created date: 14/08/2026
/// </summary>

public class WpAddTrafficLawInput :WpTrafficLaw
{
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Update Traffic Law
/// Created date: 14/08/2026
/// </summary>




public class WpUpdateTrafficLawInput : WpAddTrafficLawInput
{
    
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Delete Traffic Law
/// Created date: 14/08/2026
/// </summary>

public class WpDeleteTrafficLawInput : BaseIdInput
{
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Upload Pdf
/// Created date: 14/08/2026
/// </summary>

public class WpUploadPdfInput 
{
    public IFormFile PdfFile { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Change Stattus Traffic Law
/// Created date: 14/08/2026
/// </summary>

public class WpChangeStattusTrafficLawInput : BaseIdInput
{
    public BaseEnums.StatusEnum? IsPublish { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Traffic Law Pdf Stream
/// Created date: 14/08/2026
/// </summary>

public class WpTrafficLawPdfStreamInput : BaseIdInput
{
}
