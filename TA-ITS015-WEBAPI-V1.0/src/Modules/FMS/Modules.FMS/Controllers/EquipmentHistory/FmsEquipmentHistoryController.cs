using Modules.FMS.Controllers.EquipmentHistory.Dto;
using System.ComponentModel;

namespace Modules.FMS.Controllers.EquipmentHistory
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 22/01/2025
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class FmsEquipmentHistoryController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tham số
        /// Created date: 22/01/2025
        /// </summary>
        [HttpPost("List")]
        [DisplayName("List based on parameters")]
        public async Task<List<EquipmentHistoryOutput>> GetList(EquipmentHistoryInput input)
        {
            return await this.MessBus.InvokeAsync<List<EquipmentHistoryOutput>>(input);
        }
    }
}
