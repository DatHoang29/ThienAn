using Modules.TMS.Controllers.ZoneEquipment.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.ZoneEquipment;

[ApiDescriptionSettings(GroupName, Order = 470)]
public class TmsZoneEquipmentController : BaseController
{
    /// <summary>
    /// Description: Lấy danh sách ZoneEquipment theo bộ lọc
    /// </summary>
    [DisplayName("Get ZoneEquipment list")]
    public async Task<List<ZoneEquipmentOutput>> GetList([FromQuery] ZoneEquipmentInput input)
    {
        return await MessBus.InvokeAsync<List<ZoneEquipmentOutput>>(input);
    }

    /// <summary>
    /// Description: Lấy danh sách phân trang ZoneEquipment
    /// </summary>
    [DisplayName("Get ZoneEquipment page")]
    public async Task<SqlSugarPagedList<ZoneEquipmentOutput>> Page([FromQuery] PageZoneEquipmentInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<ZoneEquipmentOutput>>(input);
    }

    /// <summary>
    /// Description: Thêm mới ZoneEquipment mapping
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ZoneEquipment")]
    public async Task AddZoneEquipment(AddZoneEquipmentInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Cập nhật ZoneEquipment mapping
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ZoneEquipment")]
    public async Task UpdateZoneEquipment(UpdateZoneEquipmentInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Xóa ZoneEquipment mapping
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ZoneEquipment")]
    public async Task DeleteZoneEquipment(DeleteZoneEquipmentInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Xóa nhiều ZoneEquipment mapping
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ZoneEquipment")]
    public async Task BatchDeleteZoneEquipment(List<DeleteZoneEquipmentInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Auto-map equipment cho một zone (trả về preview)
    /// </summary>
    [ApiDescriptionSettings(Name = "AutoMap"), HttpPost]
    [DisplayName("Auto-map zone equipment")]
    public async Task<List<AutoMapPreviewOutput>> AutoMap(AutoMapZoneEquipmentInput input)
    {
        return await MessBus.InvokeAsync<List<AutoMapPreviewOutput>>(input);
    }

    /// <summary>
    /// Description: Auto-map equipment cho tất cả zones (trả về preview)
    /// </summary>
    [ApiDescriptionSettings(Name = "AutoMapAll"), HttpPost]
    [DisplayName("Auto-map all zone equipment")]
    public async Task<List<AutoMapPreviewOutput>> AutoMapAll(AutoMapAllZoneEquipmentInput input)
    {
        return await MessBus.InvokeAsync<List<AutoMapPreviewOutput>>(input);
    }

    /// <summary>
    /// Description: Xác nhận và lưu danh sách mapping
    /// </summary>
    [ApiDescriptionSettings(Name = "ConfirmMapping"), HttpPost]
    [DisplayName("Confirm zone equipment mapping")]
    public async Task ConfirmMapping(ConfirmZoneEquipmentMappingInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
