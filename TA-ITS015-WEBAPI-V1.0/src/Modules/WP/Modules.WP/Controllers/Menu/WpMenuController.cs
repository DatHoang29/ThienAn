using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers.Menu.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Modules.WP.Controllers.Menu;

/// <summary>
/// Author: HieuNV
/// Description: WP Menu - quản lý menu hệ thống WP
/// Created date: 25/06/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 450)]
public class WpMenuController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Get Menu list (dạng cây)
    /// Created date: 14/08/2026
    /// </summary>
    [DisplayName("Get Menu list")]
    public async Task<List<WpMenuOutput>> GetList([FromQuery] WpMenuInput input)
    {
        return await MessBus.InvokeAsync<List<WpMenuOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy cây menu để chọn menu cha (tree-select).
    /// Khi sửa, truyền ExcludeId để loại node hiện tại và con-cháu khỏi danh sách chọn.
    /// Created date: 14/08/2026
    /// </summary>
    [DisplayName("Get Menu tree select")]
    public async Task<List<WpMenuTreeSelectOutput>> TreeSelect([FromQuery] WpMenuTreeSelectInput input)
    {
        return await MessBus.InvokeAsync<List<WpMenuTreeSelectOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Add Menu
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add Menu")]
    public async Task AddMenu(WpAddMenuInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Update Menu
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update Menu")]
    public async Task UpdateMenu(WpUpdateMenuInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Delete Menu
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete Menu")]
    public async Task DeleteMenu(WpDeleteMenuInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Batch delete Menu
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete Menu")]
    public async Task BatchDeleteMenu(List<WpDeleteMenuInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
}
