using System;
using Shared.Core.Utilities;

namespace Modules.TMS.Report.Controllers.EventData.Dto
{
    /// <summary>
    /// Điều kiện tra cứu dữ liệu sự kiện VDS.
    /// </summary>
    public class PageEventDataInput : BasePageInput
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Direction { get; set; }
        public string? LicensePlate { get; set; }
        public string? VehicleType { get; set; }
        public string? EquipmentId { get; set; }
        public string? EventTypeId { get; set; }
        public byte? Severity { get; set; }
        public string? State { get; set; }
    }

    public class EventDataInput : PageEventDataInput
    {
    }
}
