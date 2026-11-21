using System.ComponentModel;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers;
using Modules.WP.Controllers.PriceList.Dto;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Modules.WP.Controllers.PriceList;

/// <summary>
/// Author: HieuNV
/// Description: WP Price List - quản lý bảng giá hệ thống WP
/// Created date: 03/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName,Order = 460)]
public class WpPriceListController : BaseController
{

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Price List pagination list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<WpPagePriceListOutput>> Page(WpPagePriceListInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<WpPagePriceListOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Category list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<WpPriceListOutput>> GetList([FromQuery] WpPriceListInput input)
    {
        return await MessBus.InvokeAsync<List<WpPriceListOutput>>(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy details
    /// Created date: 14/08/2026
    /// </summary>

    [DisplayName("Get  Details")]
    public async Task<WpPriceListViewDetailOutput> GetDetails([FromQuery] WpPriceListViewDetailInput input)
    {
        return await MessBus.InvokeAsync<WpPriceListViewDetailOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Add PriceList
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add PriceList ")]
    public async Task AddPriceList(WpAddPriceListInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Update PriceList
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update PriceList")]
    public async Task UpdatePriceList(WpUpdatePriceListInput input)
    {
        await MessBus.InvokeAsync(input);

    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật price list status
    /// Created date: 14/08/2026
    /// </summary>

    [ApiDescriptionSettings(Name = "UpdateStatus"), HttpPost]
    [DisplayName("Update PriceList Status")]
    public async Task UpdatePriceListStatus(WpUpdateStatusPriceListInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Delete PriceList
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete PriceList")]
    public async Task DeletePriceList(WpDeletePriceListInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Batch delete PriceList
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete PriceList")]
    public async Task BatchDeletePriceList(List<WpDeletePriceListInput> input)
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
