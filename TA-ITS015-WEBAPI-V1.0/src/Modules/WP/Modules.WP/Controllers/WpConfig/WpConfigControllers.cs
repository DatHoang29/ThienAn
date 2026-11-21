using Mapster;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers;
using Modules.WP.Controllers.Config.Dto;

namespace Modules.WP.Controllers.Config;      

/// <summary>
/// Author: HieuNV
/// Description: WP Config - quản lý cấu hình hệ thống WP (thông tin website, logo, ảnh banner...)
/// Created date: 03/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName,Order = 460)]
public class WpConfigController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy details
    /// Created date: 14/08/2026
    /// </summary>
    [DisplayName("Get Wp Config")]
    public async Task<WpConfigInfoOutput> GetDetails([FromQuery] WpConfigInfoInput input)
    {
        return await MessBus.InvokeAsync<WpConfigInfoOutput>(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Update Wp Config
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>

    [DisplayName("Update Wp Config")]
    [HttpPost]
    public async Task Update(
    [FromBody] WpConfigUpdateInput input)
    {
        await MessBus.InvokeAsync(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Upload Image
    /// Created date: 14/08/2026
    /// </summary>

    [ApiDescriptionSettings(Name = "UploadImg")]
    [HttpPost]
    [DisplayName("Upload Img")]
    public async Task<WPConfigUploadImgOutput> UploadImage([FromForm] WPConfigUploadImgInput input)
    {
        return await MessBus.InvokeAsync<WPConfigUploadImgOutput>(input);
    }
}
