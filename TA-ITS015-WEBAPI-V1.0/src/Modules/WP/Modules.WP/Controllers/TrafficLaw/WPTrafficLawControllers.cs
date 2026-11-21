using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers;
using Modules.WP.Controllers.Post.Dto;
using Modules.WP.Controllers.TrafficLaw.Dto;
using Shared.DTO.Enums;
using Shared.DTO.Upload;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using System.ComponentModel;
using System.IO;
using Wolverine;

namespace Modules.WP.Controllers.TrafficLaw;

/// <summary>
/// Author: HieuNV
/// Description: WP Traffic Law - quản lý quy định giao thông hệ thống WP
/// Created date: 03/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName,Order = 460)]
public class WpTrafficLawController : BaseController
{

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Traffic Law pagination list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<WpPageTrafficLawOutput>> Page(WpPageTrafficLawInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<WpPageTrafficLawOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Traffic Law list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<WpTrafficLawOutput>> GetList([FromQuery] WpTrafficLawInput input)
    {
        return await MessBus.InvokeAsync<List<WpTrafficLawOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Add Traffic Law
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add TrafficLaw")]
    public async Task AddTrafficLaw( WpAddTrafficLawInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Update Traffic Law
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update TrafficLaw")]  
    public async Task UpdateTrafficLaw(WpUpdateTrafficLawInput input)
    {
        await MessBus.InvokeAsync(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Change Status
    /// Created date: 14/08/2026
    /// </summary>

    [ApiDescriptionSettings(Name = "WpChangeStatusTrafficLaw")]
    [HttpPost]
    [DisplayName("Change Publish Status")]
    public async Task<WpChangeStattusTrafficLawOutput> ChangeStatus(WpChangeStattusTrafficLawInput input)
    {
        return await MessBus.InvokeAsync<WpChangeStattusTrafficLawOutput>(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Delete Traffic Law
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete TrafficLaw")]
    public async Task DeleteTrafficLaw(WpDeleteTrafficLawInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Batch delete Traffic Law
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete TrafficLaw ")]
    public async Task BatchDeleteTrafficLaw(List<WpDeleteTrafficLawInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Stream Pdf
    /// Created date: 14/08/2026
    /// </summary>

    [NonUnify]
    [ApiDescriptionSettings(Name = "StreamPdf"), HttpGet]
    [DisplayName("Stream TrafficLaw Pdf")]
    public async Task<FileStreamResult> StreamPdf([FromQuery] WpTrafficLawPdfStreamInput input)
    {
        return await MessBus.InvokeAsync<FileStreamResult>(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Upload Image
    /// Created date: 14/08/2026
    /// </summary>

    [ApiDescriptionSettings(Name = "WPUploadPdf")]
    [HttpPost]
    [DisplayName("Upload TrafficLaw Pdf")]
    public async Task<WpUploadPdfOutput> UploadImage(
   [FromForm] WpUploadPdfInput input)
    {
        return await MessBus.InvokeAsync<WpUploadPdfOutput>(input);
    }

}
