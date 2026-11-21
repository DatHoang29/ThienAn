using Mapster;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers;
using Modules.WP.Controllers.Category.Dto;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using System.ComponentModel;
using Wolverine;

namespace Modules.WP.Controllers.Category;

/// <summary>
/// Author: HieuNV
/// Description: WP Category - quản lý danh mục hệ thống WP
/// Created date: 03/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName,Order = 460)]
public class WpCategoryController : BaseController
{

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Category pagination list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<WpPageCategoryOutput>> Page(WpPageCategoryInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<WpPageCategoryOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Get Category list
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<WpCategoryOutput>> GetList([FromQuery] WpCategoryInput input)
    {
        return await MessBus.InvokeAsync<List<WpCategoryOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Add Category
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add Category ")]
    public async Task AddCategory(WpAddCategoryInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Update Category
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update Category")]
    public async Task UpdateCategory(WpUpdateCategoryInput input)
    {
        await MessBus.InvokeAsync(input);

    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Change Status
    /// Created date: 14/08/2026
    /// </summary>

    [ApiDescriptionSettings(Name = "WpChangeStatusCategory")]
    [HttpPost]
    [DisplayName("Change Publish Status")]
    public async Task<WpChangeStatusCategoryOutput> ChangeStatus(WpChangeStatusCategoryInput input)
    {
        return await MessBus.InvokeAsync<WpChangeStatusCategoryOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Delete Category
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete Category")]
    public async Task DeleteCategory(WpDeleteCategoryInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Batch delete Category
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete Category")]
    public async Task BatchDeleteCategory(List<WpDeleteCategoryInput> input)
    {
        await MessBus.InvokeAsync(input);
    }


}
