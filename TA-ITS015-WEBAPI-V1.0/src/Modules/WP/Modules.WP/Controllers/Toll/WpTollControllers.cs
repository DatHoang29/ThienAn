using System.ComponentModel;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers;
using Modules.WP.Controllers.Toll.Dto;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Modules.WP.Controllers.Toll;

/// <summary>
/// Author: HieuNV
/// Description: WP Toll - quản lý trạm thu phí hệ thống WP
/// Created date: 03/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName,Order = 460)]
public class WpTollController : BaseController
{

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Toll pagination list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<WpPageTollOutput>> Page(WpPageTollInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<WpPageTollOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Toll list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<WpTollOutput>> GetList([FromQuery] WpTollInput input)
    {
        return await MessBus.InvokeAsync<List<WpTollOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Add Toll
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add Toll ")]
    public async Task AddToll(WpAddTollInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Update Toll
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update Toll")]
    public async Task UpdateToll(WpUpdateTollInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Delete Toll
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete Toll")]
    public async Task DeleteToll(WpDeleteTollInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Batch delete Toll
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete Toll")]
    public async Task BatchDeleteToll(List<WpDeleteTollInput> input)
    {
        await MessBus.InvokeAsync(input);
    }


    //[HttpGet("demo")]
    //[DisplayName("Demo")]
    //public string Demo()
    //{
    //    //hạn chế gọi thẳng từ controller >> có thể chuyển qua query/commmand để gọi rồi dùng Invoke qua
    //    return _categoryService.Calculate();
    //}

    //[HttpGet("chart")]
    //[DisplayName("Generate Chart")]
    //public async Task<CategoryChartOutput> GenerateChart([FromQuery] CategoryChartInput input)
    //{
    //    return await MessBus.InvokeAsync<CategoryChartOutput>(input);
    //}
}
