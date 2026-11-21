namespace Modules.FMS.Controllers.EquipmentHistory.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số lọc EquipmentHistory
    /// Created date: 22/01/2025
    /// </summary>
    public class EquipmentHistoryInput
    {
        public string? EquipmentId { get; set; }
        public string? NewState { get; set; }
        public string? Type { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}