using Modules.TMS.Controllers.Zone.Dto;

namespace Modules.TMS.Controllers.Zone;

/// <summary>
///  Tms Zone Controller
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsZoneController : BaseController
{
    /// <summary>Danh sách cấu hình tốc độ vùng, camera VDS và mặc định theo hướng (chỉ đọc).</summary>
    [ApiDescriptionSettings(Name = "SpeedConfigList"), HttpGet]
    [DisplayName("Get zone speed configuration")]
    public async Task<ZoneSpeedConfigListOutput> SpeedConfigList()
        => await MessBus.InvokeAsync<ZoneSpeedConfigListOutput>(new SpeedConfigListInput());

    /// <summary>Ghi đè min/max của các vùng được chọn; null xoá ngưỡng riêng của vùng.</summary>
    // Cùng CommonBaseController và cơ chế quyền API với UpdateZone; không mở anonymous.
    [ApiDescriptionSettings(Name = "BatchUpdateSpeed"), HttpPost]
    [DisplayName("Batch update zone speed")]
    public async Task<BatchUpdateZoneSpeedOutput> BatchUpdateSpeed(BatchUpdateZoneSpeedInput input)
        => await MessBus.InvokeAsync<BatchUpdateZoneSpeedOutput>(input);


    ///// <summary>
    ///// Get Tms Zone pagination list
    ///// </summary>
    ///// <param name="input"></param>
    ///// <returns></returns>
    //[DisplayName("Get  pagination list")]
    //public async Task<SqlSugarPagedList<PageZoneOutput>> Page(PageZoneInput input)
    //{
    //    return await MessBus.InvokeAsync<SqlSugarPagedList<PageZoneOutput>>(input);
    //}

    /// <summary>
    /// Get Tms Zone list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get list")]
    public async Task<List<ZoneOutput>> GetList([FromQuery] ZoneInput input)
    {
        return await MessBus.InvokeAsync<List<ZoneOutput>>(input);
    }

    /// <summary>
    /// Add Tms Zone
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add zone")]
    public async Task AddZone(AddZoneInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update Tms Zone 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update zone")]
    public async Task UpdateZone(UpdateZoneInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete Tms Zone 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete")]
    public async Task DeleteZone(DeleteZoneInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete Tms Zones 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete")]
    public async Task BatchDeleteZone(List<DeleteZoneInput> input)
    {
        await MessBus.InvokeAsync(input);
    }


    /// <summary>
    /// Get TmsZone tree structure
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetTree")]
    [DisplayName("Get List Tree")]
    public async Task<ActionResult<List<ZoneOutput>>> GetZoneTree([FromQuery] TreeZoneInput input)
    {
        return await MessBus.InvokeAsync<List<ZoneOutput>>(input);
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: Hàm list danh sách zone theo vị trí
    /// Created date: 25/12/2024
    /// </summary>
    [HttpPost("ListZoneByPosition")]
    [DisplayName("List zone based on position")]
    public async Task<List<ZoneOutput>> ListZoneByPosition(TmsZonePositionInput input)
    {
        return await this.MessBus.InvokeAsync<List<ZoneOutput>>(input);
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: Hàm list danh sách zone theo danh sách zoneId
    /// Created date: 25/12/2024
    /// </summary>
    [HttpGet("ListZoneByIds")]
    [DisplayName("List zone based on the list of zoneId")]
    public async Task<List<ZoneOutput>> ListZoneByIds([FromQuery] TmsZoneIdListInput input)
    {
        return await this.MessBus.InvokeAsync<List<ZoneOutput>>(input);
    }

    /// <summary>
    /// Get Tms Zone list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get list")]
    [HttpGet("GetListStatusInfo")]
    public async Task<List<ZoneOutput>> GetListStatusInfo([FromQuery] ZoneStatusInfoInput input)
    {
        return await MessBus.InvokeAsync<List<ZoneOutput>>(input);
    }
    /// <summary>
    /// Get TmsZone tree structure
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetTreeWithChild")]
    [DisplayName("Get List Tree With Child")]
    public async Task<ActionResult<List<ZoneOutput>>> GetZoneTreeChild([FromQuery] ZoneWithChildInput input)
    {
        return await MessBus.InvokeAsync<List<ZoneOutput>>(input);
    }
}
