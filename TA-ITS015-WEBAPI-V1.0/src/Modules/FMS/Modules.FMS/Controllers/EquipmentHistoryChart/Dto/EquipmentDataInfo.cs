using Shared.DTO.Dtos.Tms;

namespace Modules.FMS.Controllers.EquipmentHistoryChart.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 22/01/2025
    /// </summary>
    public class EquipmentDataInfo: TmsEquipmentResponse
    {
        public int InvalidStateCount { get; set; } = 0;
    }
}
