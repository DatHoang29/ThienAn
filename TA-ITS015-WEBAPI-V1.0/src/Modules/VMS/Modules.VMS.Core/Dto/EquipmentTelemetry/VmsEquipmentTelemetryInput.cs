using System;
using System.Collections.Generic;
using Shared.Core.Utilities;

namespace Modules.VMS.Controllers.EquipmentTelemetry.Dto
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Yêu cầu đọc ảnh hiện đang hiển thị trên thiết bị (thủ công, để đối chiếu với DB).
    /// Created date: 2026-08-20
    /// </summary>
    public class ReadDeviceImageInput
    {
        /// <summary> ID thiết bị (TmsEquipment.ID) </summary>
        public string EquipmentId { get; set; }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Yêu cầu đọc trạng thái bên trong thiết bị (thủ công).
    /// Created date: 2026-08-20
    /// </summary>
    public class GetDeviceStatInput
    {
        /// <summary> ID thiết bị (TmsEquipment.ID) </summary>
        public string EquipmentId { get; set; }

        /// <summary>
        /// Danh sách thông số cần lấy (IntgVmsStatType). Bỏ trống -> lấy bộ mặc định.
        /// </summary>
        public string[]? StatTypes { get; set; }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Phân trang lịch sử telemetry thiết bị (dùng cho màn tra cứu lịch sử).
    /// Created date: 2026-08-20
    /// </summary>
    public class PageVmsEquipmentTelemetryInput : BasePageInput
    {
        public string? EquipmentId { get; set; }
        public string? EquipmentCode { get; set; }
        public string? EquipmentTypeId { get; set; }
        public string? Source { get; set; }
        public bool? Succeeded { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Xóa một bản ghi lịch sử telemetry thiết bị.
    /// Created date: 2026-08-20
    /// </summary>
    public class DeleteVmsEquipmentTelemetryInput : BaseIdInput
    {
    }

    // StartTelemetryScheduleInput / StopTelemetryScheduleInput đã BỎ ngày 23/09/2026.
    // Lập lịch thu thập telemetry chuyển sang dịch vụ ITS.VMS.Worker (Telemetry:CollectIntervalMinutes),
    // BE không còn giữ nhịp nên không có lệnh bật/tắt.
}
