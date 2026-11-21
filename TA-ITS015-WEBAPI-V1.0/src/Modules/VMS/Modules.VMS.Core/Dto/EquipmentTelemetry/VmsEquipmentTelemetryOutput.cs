using System.Collections.Generic;
using Modules.VMS.Core.Entities;

namespace Modules.VMS.Controllers.EquipmentTelemetry.Dto
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Kết quả đọc ảnh hiện tại của thiết bị (base64) để FE hiển thị đối chiếu.
    /// Created date: 2026-08-20
    /// </summary>
    public class ReadDeviceImageOutput
    {
        public bool Succeeded { get; set; }
        public string? Vendor { get; set; }
        public string? Base64 { get; set; }
        public string? ContentType { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Kết quả đọc trạng thái thiết bị, đã gom nhóm theo loại dữ liệu
    ///              (resource/environment/diagnostic) để FE render từng khối.
    /// Created date: 2026-08-20
    /// </summary>
    public class GetDeviceStatOutput
    {
        public bool Succeeded { get; set; }
        public string? Vendor { get; set; }
        public string? Message { get; set; }

        /// <summary> { "resource": {...}, "environment": { "temperatureIn": "..." }, "diagnostic": {...} } </summary>
        public Dictionary<string, Dictionary<string, object>> Groups { get; set; } = new();
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Dòng dữ liệu phân trang lịch sử telemetry thiết bị.
    /// Created date: 2026-08-20
    /// </summary>
    public class PageVmsEquipmentTelemetryOutput : VmsEquipmentTelemetry
    {
    }

    // TelemetryScheduleOutput đã BỎ ngày 23/09/2026 cùng 2 API StartSchedule/StopSchedule.
}
