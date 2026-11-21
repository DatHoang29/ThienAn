using Modules.FMS.Controllers.EquipmentHistoryChart.Dto;
using System.ComponentModel;

namespace Modules.FMS.Controllers.EquipmentHistoryChart
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 22/01/2025
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class FmsEquipmentHistoryChartController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Created date: 22/01/2025
        /// </summary>
        [HttpPost("InvalidEquipmentHistoryChart")]
        [DisplayName("Invalid equipment history chart")]
        public async Task<InvalidEquipmentHistoryChartOutput> InvalidEquipmentHistoryChart(InvalidEquipmentHistoryChartInput input)
        {
            return await this.MessBus.InvokeAsync<InvalidEquipmentHistoryChartOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 22/01/2025
        /// </summary>
        [HttpPost("InvalidEquipmentHistoryDetailChart")]
        [DisplayName("Invalid equipment history detail chart")]
        public async Task<InvalidEquipmentHistoryDetailChartOutput> InvalidEquipmentHistoryDetailChart(InvalidEquipmentHistoryDetailChartInput input)
        {
            return await this.MessBus.InvokeAsync<InvalidEquipmentHistoryDetailChartOutput>(input);
        }
    }
}
