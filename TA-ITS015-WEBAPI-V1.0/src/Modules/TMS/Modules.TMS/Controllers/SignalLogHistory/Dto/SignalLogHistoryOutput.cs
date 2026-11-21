using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.SignalLogHistory.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin SignalLog
    /// Created date: 01/02/2025
    /// </summary>
    public class SignalLogHistoryOutput : TmsSignalLog { }


    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin SignalLog
    /// Created date: 01/02/2025
    /// </summary>
    public class PageSignalLogHistoryOutput : TmsSignalLog
    {
        public string? EquipmentTypeId { get; set; }
        public string? EquipmentTypeCode { get; set; }
        public string? EquipmentTypeName { get; set; }

        public string? EquipmentName { get; set; }
    }
}