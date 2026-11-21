using Shared.DTO.Dtos.Fms;

namespace Modules.VMS.Report.Controllers.EquipmentHistory.Dto
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: DTO thông tin EquipmentHistory cho report VMS/LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsEquipmentHistoryData : FmsEquipmentHistoryResponse
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
