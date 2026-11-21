using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Shared.Core.Utilities;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.MaintenanceSchedule;

/// <summary>
/// Author: System
/// Description: Tms Maintenance Guide Controller - Quản lý hướng dẫn bảo trì
/// Created date: 10/12/2025
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 470)]
public class TmsMaintenanceGuideController : BaseController
{
    /// <summary>
    /// Lấy danh sách hướng dẫn bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "GetList"), HttpPost]
    [DisplayName("Get maintenance guide list")]
    public async Task<List<MaintenanceGuideOutput>> GetList(MaintenanceGuideInput input)
    {
        return await MessBus.InvokeAsync<List<MaintenanceGuideOutput>>(input);
    }

    /// <summary>
    /// Lấy danh sách hướng dẫn bảo trì có phân trang
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "GetPage"), HttpPost]
    [DisplayName("Get maintenance guide list with pagination")]
    public async Task<SqlSugarPagedList<MaintenanceGuideOutput>> GetPage(PageMaintenanceGuideInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<MaintenanceGuideOutput>>(input);
    }

    /// <summary>
    /// Lấy chi tiết hướng dẫn bảo trì
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("GetById/{id}")]
    [DisplayName("Get maintenance guide by id")]
    public async Task<MaintenanceGuideOutput?> GetById(string id)
    {
        return await MessBus.InvokeAsync<MaintenanceGuideOutput?>(new BaseIdInput { ID = id });
    }

    /// <summary>
    /// Thêm hướng dẫn bảo trì mới
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add maintenance guide")]
    public async Task Add(AddMaintenanceGuideInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Cập nhật hướng dẫn bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update maintenance guide")]
    public async Task Update(UpdateMaintenanceGuideInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Xóa hướng dẫn bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete maintenance guide")]
    public async Task Delete(DeleteMaintenanceGuideInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Lấy hướng dẫn bảo trì phù hợp cho thiết bị (theo type + manufacturer)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("GetByEquipment")]
    [DisplayName("Get guides for equipment")]
    public async Task<List<MaintenanceGuideOutput>> GetByEquipment(GetGuideByEquipmentInput input)
    {
        return await MessBus.InvokeAsync<List<MaintenanceGuideOutput>>(input);
    }

    /// <summary>
    /// Upload tài liệu đính kèm cho hướng dẫn bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("UploadDocument")]
    [DisplayName("Upload maintenance guide document")]
    public async Task<GuideDocumentInfo> UploadDocument([FromForm] UploadMaintenanceGuideDocumentInput input)
    {
        return await MessBus.InvokeAsync<GuideDocumentInfo>(input);
    }
}
