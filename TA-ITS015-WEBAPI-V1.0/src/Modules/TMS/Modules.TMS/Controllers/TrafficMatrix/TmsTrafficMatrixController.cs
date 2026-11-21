using Modules.TMS.Controllers.TrafficMatrix.Dto;

namespace Modules.TMS.Controllers.TrafficMatrix
{
    /// <summary>
    /// Author: KienLT
    /// Description: Bảng ma trận vận tốc / lưu lượng theo giờ (hàng = đoạn tuyến, cột = 24 giờ).
    /// Created date: 11/08/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 455)]
    public class TmsTrafficMatrixController : BaseController
    {
        /// <summary>
        /// Ma trận theo giờ của một ngày: mỗi hướng tuyến một nhóm, mỗi đoạn tuyến một hàng, mỗi giờ một ô.
        /// Trả về cả vận tốc và lưu lượng để đổi chế độ xem không phải gọi lại API.
        /// </summary>
        [HttpPost("HourlyMatrix")]
        [DisplayName("Hourly speed/volume matrix by section")]
        public async Task<TrafficMatrixOutput> HourlyMatrix(TrafficMatrixInput input)
        {
            return await MessBus.InvokeAsync<TrafficMatrixOutput>(input);
        }
    }
}
