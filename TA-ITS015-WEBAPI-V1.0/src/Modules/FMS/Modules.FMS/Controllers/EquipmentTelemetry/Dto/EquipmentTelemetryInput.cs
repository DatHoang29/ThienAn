using System;
using Shared.Core.Utilities;

namespace Modules.FMS.Controllers.EquipmentTelemetry.Dto
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Phân trang lịch sử telemetry thiết bị FMS (dùng cho màn tra cứu lịch sử).
    /// Created date: 2026-08-27
    /// </summary>
    public class PageEquipmentTelemetryInput : BasePageInput
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
    /// Description: Xóa một bản ghi lịch sử telemetry thiết bị FMS.
    /// Created date: 2026-08-27
    /// </summary>
    public class DeleteEquipmentTelemetryInput : BaseIdInput
    {
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Phân trang DANH SÁCH THIẾT BỊ (TmsEquipment) kèm telemetry MỚI NHẤT của mỗi thiết bị
    ///              (dùng cho màn giám sát chính: mỗi dòng 1 thiết bị + thông số mới nhất dịch vụ lấy được).
    /// Created date: 2026-09-10
    /// </summary>
    public class PageEquipmentLatestInput : BasePageInput
    {
        public string? EquipmentCode { get; set; }
        public string? EquipmentTypeId { get; set; }
    }
}
