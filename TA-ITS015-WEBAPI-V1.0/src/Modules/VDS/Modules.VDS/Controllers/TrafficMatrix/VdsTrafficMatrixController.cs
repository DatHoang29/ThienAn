using Modules.VDS.Controllers.TrafficMatrix.Dto;
using Shared.DTO.Dtos.Vds;

namespace Modules.VDS.Controllers.TrafficMatrix
{
    /// <summary>
    /// Author: KienLT
    /// Description: API ma trận thô VDS (QuestDB). Trả số theo <c>(zone, direction, hour)</c>; TMS gom cây
    /// zone + thang màu (quyết định 3b). Route <c>api/vds/vdstrafficmatrix/hourlyraw</c>, GroupName VDS.
    /// Created date: 03/09/2026
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName, Order = 588)]
    public class VdsTrafficMatrixController : BaseController
    {
        /// <summary>
        /// Get VDS raw hourly traffic matrix
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc vùng/hướng.</param>
        /// <returns>Các dòng số theo (zone, direction, hour); TMS tự gom cây zone và thang màu.</returns>
        [HttpPost("HourlyRaw")]
        [DisplayName("Traffic matrix raw hourly")]
        public async Task<List<VdsTrafficMatrixRow>> HourlyRaw(TrafficMatrixRawInput input)
        {
            return await MessBus.InvokeAsync<List<VdsTrafficMatrixRow>>(input);
        }
    }
}
