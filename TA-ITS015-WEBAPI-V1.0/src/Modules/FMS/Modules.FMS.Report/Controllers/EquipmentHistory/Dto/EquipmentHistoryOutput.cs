using Shared.DTO.Dtos.Fms;

namespace Modules.FMS.Report.Controllers.EquipmentHistory.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin EquipmentHistory cho report
    /// Created date: 09/08/2024
    /// </summary>
    public class EquipmentHistoryData: FmsEquipmentHistoryResponse
    {
        public string? Idx { get; set; }
        public string? EquipmentName { get; set; }
        public string? EquipmentType { get; set; }
        public string? OldStateName { get; set; }
        public string? NewStateName { get; set; }
        public string? StartDate { get; set; }
        public string? UpdateDate { get; set; }
        public string? TypeName { get; set; }
    }
}
