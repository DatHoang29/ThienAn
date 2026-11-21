using Modules.TMS.Controllers.TrafficInfo.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure.Services.TrafficInfo;

namespace Modules.TMS.Controllers.TrafficInfo
{
    /// <summary>
    /// System TrafficInfo
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 460)]
    public class TmsTrafficInfoController : BaseController
    {
        private readonly TrafficInfoService _trafficInfoService;

        public TmsTrafficInfoController(TrafficInfoService trafficInfoService)
        {
            _trafficInfoService = trafficInfoService;
        }

        /// <summary>
        /// Kiểm tra sự tồn tại của Incident
        /// </summary>
        /// <returns></returns>
        [DisplayName("Kiểm tra sự tồn tại của Incident")]
        public async Task<List<TmsIncident>> CheckIncident()
        {
            // Gọi phương thức GetIncident() trả về danh sách TmsIncident
            var incidents = await _trafficInfoService.Incident();

            // Trả về danh sách incidents nếu có, ngược lại trả về một danh sách rỗng
            if (incidents != null && incidents.Any())
            {
                return incidents; // OK - Trả về danh sách incidents thỏa mãn
            }

            return new List<TmsIncident>(); // NotFound - Trả về danh sách rỗng nếu không có incident nào thỏa mãn
        }

        /// <summary>
        /// Endppoint xử lý lấy thông tin các sự cố xảy ra trên zone
        /// Author: TuanHA
        /// DateCreated: 2026-04-09
        /// </summary>
        /// <returns></returns>
        [DisplayName("Kiểm tra sự tồn tại của Incident")]
        public async Task<List<ListZoneIncidentOutput>> CheckIncidentExists()
        {

            return await MessBus.InvokeAsync<List<ListZoneIncidentOutput>>(new ListZoneIncidentInput());
        }
        [HttpGet("GetEquipmentTypeByEquipmentId/{equipmentId}")]
        [DisplayName("Lấy thông tin loại thiết bị theo EquipmentId")]
        public async Task<IActionResult> GetEquipmentTypeByEquipmentId(string equipmentId)
        {
            try
            {
                var (equipmentType, equipmentState, moduleName) = await _trafficInfoService.GetEquipmentTypeByEquipmentId(equipmentId);

                if (equipmentType != null) // Kiểm tra nếu equipmentType không null
                {
                    return new JsonResult(new { equipmentType, equipmentState, moduleName }); // Trả về thông tin loại thiết bị và trạng thái
                }
                else
                {
                    // Trả về một đối tượng rỗng khi không tìm thấy thiết bị
                    return new JsonResult(new { }); // Trả về thông tin loại thiết bị và trạng thái
                }
                // Gọi hàm GetEquipmentTypeByEquipmentId trong service

            }
            catch (Exception ex)
            {
                // Trả về phản hồi lỗi tùy chỉnh
                return new JsonResult(new { message = ex.Message }) { StatusCode = 500 };
            }
        }
    }
}
