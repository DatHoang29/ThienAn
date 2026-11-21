using System.ComponentModel;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers;
using Modules.WP.Controllers.PriceListDetail.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.WP.Controllers.PriceListDetail;

/// <summary>
/// Author: HieuNV
/// Description: WP Price List Detail - quản lý chi tiết bảng giá hệ thống WP
/// Created date: 03/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class WpPriceListDetailController : BaseController
{

    /// <summary>
    /// Get Price List Detail pagination list
    /// </summary>
    /// <param name="input"></param>
    //<returns></returns>
    /// <summary>
    /// Author: HieuNV
    /// Description: Page
    /// Created date: 14/08/2026
    /// </summary>
    [DisplayName("Get  pagination list Price List")]
    public async Task<SqlSugarPagedList<WpPagePriceListDetailOutput>> Page(WpPagePriceListDetailInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<WpPagePriceListDetailOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Price List Detail list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list Price List")]
    public async Task<List<WpPriceListDetailOutput>> GetList([FromQuery] WpPriceListDetailInput input)
    {
        return await MessBus.InvokeAsync<List<WpPriceListDetailOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Add Price List Detail
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "AddPriceListDetail"), HttpPost]
    [DisplayName("Add Price List Detail")]
    public async Task AddPriceListDetail(WpAddPriceListDetailInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Update Price List Detail
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "UpdatePriceListDetail"), HttpPost]
    [DisplayName("Update Price List Detail")]
    public async Task UpdatePriceListDetail(WpUpdatePriceListDetailInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Delete Price List Detail
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "DeletePriceListDetail"), HttpPost]
    [DisplayName("Delete Price List Detail")]
    public async Task DeletePriceListDetail(WpDeletePriceListDetailInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Batch delete Price List Detail
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDeletePriceListDetail"), HttpPost]
    [DisplayName("Batch delete Price List Detail")]
    public async Task BatchDeletePriceListDetail(List<WpDeletePriceListDetailInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
}
