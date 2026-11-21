using Modules.TMS.Controllers.EquipmentTypeChart.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.EquipmentTypeChart
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsEquipmentTypeChartController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("ValidEquipmentRateList")]
        [DisplayName("Valid equipment rate list")]
        public async Task<SqlSugarPagedList<PageValidEquipmentRateOutput>> Page(PageValidEquipmentRateInput input)
        {
            return await this.MessBus.InvokeAsync<SqlSugarPagedList<PageValidEquipmentRateOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("EquipmentStateChart")]
        [DisplayName("Equipment state chart")]
        public async Task<EquipmentStateChartOutput> EquipmentStateChart(EquipmentStateChartInput input)
        {
            return await this.MessBus.InvokeAsync<EquipmentStateChartOutput>(input);
        }
    }
}
