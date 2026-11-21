using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Infrastructure.Services.TmsEquipments;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.Equipment;

/// <summary>
/// System Tms Equipment 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsEquipmentController : BaseController
{
    private readonly TmsEquipmentService _equipmentService;

    public TmsEquipmentController(TmsEquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    /// <summary>
    /// Get Tms Equipment pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageEquipmentOutput>> Page(PageEquipmentInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageEquipmentOutput>>(input);
    }

    /// <summary>
    /// Get Tms Equipment list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<EquipmentOutput>> GetList([FromQuery] EquipmentInput input)
    {
        return await MessBus.InvokeAsync<List<EquipmentOutput>>(input);
    }

    /// <summary>
    /// Add Tms Equipment
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsEquipment(AddEquipmentInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update Tms Equipment 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsEquipment(UpdateEquipmentInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    [ApiDescriptionSettings(Name = "BatchUpdate Visible"), HttpPost]
    [DisplayName("Update Visible")]
    public async Task UpdateTmsEquipmentVisible(List<UpdateEquipmentVisibleInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    [ApiDescriptionSettings(Name = "BatchUpdate Invisible"), HttpPost]
    [DisplayName("Update Invisible")]
    public async Task UpdateTmsEquipmentInvisible(List<UpdateEquipmentInVisibleInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Delete Tms Equipment 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsEquipment(DeleteEquipmentInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete Tms Equipments 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsEquipment(List<DeleteEquipmentInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: Lấy thông tin theo id hoặc code
    /// Created date: 09/08/2024
    /// </summary>
    [HttpPost("Single")]
    [DisplayName("Get single based on id or code")]
    public async Task<TmsEquipmentDataOutput> Single(TmsEquipmentSingleInput input)
    {
        return await this.MessBus.InvokeAsync<TmsEquipmentDataOutput>(input);
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: List theo danh sách id
    /// Created date: 19/08/2024
    /// </summary>
    [HttpGet("ListByIds")]
    [DisplayName("List equipment based on the list of id")]
    public async Task<List<TmsEquipmentDataOutput>> ListTmsEquipmentByIds([FromQuery] TmsEquipmentIdListInput input)
    {
        return await this.MessBus.InvokeAsync<List<TmsEquipmentDataOutput>>(input);
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: List theo vị trí và hướng
    /// Created date: 19/08/2024
    /// </summary>
    [HttpPost("ListByPosition")]
    [DisplayName("List equipment based on position")]
    public async Task<List<TmsEquipmentDataOutput>> ListTmsEquipmentByPosition(TmsEquipmentPositionInput input)
    {
        return await this.MessBus.InvokeAsync<List<TmsEquipmentDataOutput>>(input);
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: List theo danh sách zone và thiết bị
    /// Created date: 19/08/2024
    /// </summary>
    [HttpPost("ListByEquipmentZoneList")]
    [DisplayName("List equipment based on equipment and zone list")]
    public async Task<List<TmsEquipmentDataOutput>> ListTmsEquipmentByPosition(TmsEquipmentZoneListInput input)
    {
        return await this.MessBus.InvokeAsync<List<TmsEquipmentDataOutput>>(input);
    }

    /// <summary>
    /// Description: Lấy danh sách thiết bị theo danh sách ZoneId thông qua bảng TmsZoneEquipment
    /// </summary>
    [HttpPost("ListByZoneList")]
    [DisplayName("List equipment by zone IDs via zone-equipment mapping")]
    public async Task<List<TmsEquipmentDataOutput>> ListTmsEquipmentByZoneList(TmsEquipmentByZoneListInput input)
    {
        return await this.MessBus.InvokeAsync<List<TmsEquipmentDataOutput>>(input);
    }

    /// <summary>
    /// Description: ĐIỂM VÀO DUY NHẤT của nghiệp vụ gợi ý thiết bị VMS/LCS theo vị trí sự cố.
    ///              - Ứng viên (hiển thị): small, mở rộng theo large khi IsExpand, cộng toàn tuyến khi IsWholeRoute.
    ///              - Tích sẵn (IsRecommended): trong small + đúng hướng + thuộc tuyến + đúng Before/After
    ///                theo TmsEquipmentEventRule.
    /// Note: endpoint cũ ListByPositionAndDirection đã bỏ (trùng nghiệp vụ, không có nơi gọi).
    /// </summary>
    [HttpPost("ListEquipmentSuggestion")]
    [DisplayName("List suggested equipment (expand + whole route)")]
    public async Task<List<TmsEquipmentDataOutput>> ListTmsEquipmentSuggestion(TmsEquipmentSuggestionInput input)
    {
        return await this.MessBus.InvokeAsync<List<TmsEquipmentDataOutput>>(input);
    }

    /// <summary>
    /// Author: KhoaND
    /// Description: Hàm list danh sách equipment theo name module
    /// Created date: 19/08/2024
    /// </summary>
    /// <param name="input">Đối tượng TmsIncidentPositionForEquipment</param>
    [HttpGet("GetEquipmentTypeByEquipmentId/{vmstype}")]
    [DisplayName("Lấy thông tin loại thiết bị theo Equipment Type")]
    public async Task<IActionResult> GetEquipmentTypeByEquipmentId(string vmstype)
    {
        try
        {
            // Gọi hàm GetEquipmentTypeByEquipmentId trong service
            var equipmentList = await _equipmentService.GetEquipmentTypeByEquipmentId(vmstype);

            // Nếu danh sách thiết bị không null hoặc rỗng, trả về kết quả
            if (equipmentList != null && equipmentList.Count > 0)
            {
                return new JsonResult(equipmentList); // Trả về thông tin loại thiết bị
            }
            else
            {
                return new JsonResult(new { message = "No equipment found." }) { StatusCode = 404 };
            }
        }
        catch (Exception ex)
        {
            // Trả về phản hồi lỗi tùy chỉnh
            return new JsonResult(new { message = ex.Message }) { StatusCode = 500 };
        }
    }
}
