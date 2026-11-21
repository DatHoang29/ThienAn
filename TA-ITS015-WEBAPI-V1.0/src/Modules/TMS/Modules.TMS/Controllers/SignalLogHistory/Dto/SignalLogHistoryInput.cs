using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.SignalLogHistory.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số phân trang
    /// Created date: 01/02/2025
    /// </summary>
    public class PageSignalLogHistoryInput : BasePageInput
    {
        public string? ModuleKey { get; set; }
        public string? EquipmentTypeId { get; set; }
        public string? EquipmentId { get; set; }
        public string? State { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ExecuteBy { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số lọc tìm kiếm
    /// Created date: 01/02/2025
    /// </summary>
    public class SignalLogHistoryInput
    {
        public string ModuleKey { get; set; }
        public string? EquipmentTypeId { get; set; }
        public string? EquipmentId { get; set; }
        public string? State { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ExecuteBy { get; set; }
    }
}