using System;
using System.Collections.Generic;

namespace Modules.VMS.Controllers.EquipmentTelemetry.Dto
{
    /// <summary>
    /// Phân loại gói tin trên kênh dữ liệu VMS (ItsDataTransporterChannel.VmsData = "ta.its.data.vms").
    ///
    /// Kênh này chở NHIỀU loại gói nên bên nhận BẮT BUỘC lọc theo trường phân loại:
    ///   - { ActionType = "vms-success" | "vms-fail" } : BE -> FE, kết quả gửi ảnh (gói CŨ, không có Type).
    ///   - { Type = "equipment-request" }              : dịch vụ VMS hỏi danh sách thiết bị.
    ///   - { Type = "equipment-result" }               : BE trả danh sách thiết bị.
    ///   - { Type = "telemetry" }                      : dịch vụ VMS đẩy thông số đọc được về BE.
    ///
    /// CẢNH BÁO: các lớp trong file này phải TRÙNG TÊN THUỘC TÍNH với DTO cùng tên ở
    /// ITS.VMS.Core/Messaging/VmsTelemetryMessages.cs. Cố tình trùng lặp thay vì tách project dùng chung,
    /// vì dịch vụ chạy độc lập không được kéo theo cây phụ thuộc Furion của BE.
    ///
    /// Author: ChuongLP
    /// Created date: 23/09/2026
    /// </summary>
    public static class VmsDataType
    {
        /// <summary>Dịch vụ hỏi BE danh sách thiết bị.</summary>
        public const string EquipmentRequest = "equipment-request";

        /// <summary>BE trả danh sách thiết bị cho dịch vụ.</summary>
        public const string EquipmentResult = "equipment-result";

        /// <summary>Dịch vụ đẩy số liệu telemetry về BE.</summary>
        public const string Telemetry = "telemetry";
    }

    /// <summary>Dịch vụ hỏi BE danh sách thiết bị VMS/LCS cần đọc telemetry.</summary>
    public class VmsEquipmentSyncRequest
    {
        public string? Type { get; set; }

        /// <summary>Mã truy vết, BE phải trả lại đúng giá trị này để dịch vụ đối chiếu.</summary>
        public string? RequestId { get; set; }

        /// <summary>Mã loại thiết bị cần lấy ("vms", "lcs"). Để trống thì BE dùng mặc định của mình.</summary>
        public List<string>? TypeCodes { get; set; }

        public DateTime RequestTime { get; set; }
    }

    /// <summary>BE trả danh sách thiết bị kèm đủ thông tin kết nối để dịch vụ đọc được thiết bị.</summary>
    public class VmsEquipmentSyncResult
    {
        public string Type { get; set; } = VmsDataType.EquipmentResult;

        public string? RequestId { get; set; }

        public bool Success { get; set; }

        public string? Message { get; set; }

        /// <summary>Thời điểm BE lấy dữ liệu, dịch vụ dùng để biết bản sao cũ bao lâu.</summary>
        public DateTime SyncedAt { get; set; } = DateTime.Now;

        public List<VmsEquipmentInfo> Equipments { get; set; } = new List<VmsEquipmentInfo>();
    }

    /// <summary>
    /// Thông tin một thiết bị dịch vụ cần để đọc telemetry. Chỉ lấy đúng trường cần dùng,
    /// KHÔNG mang toàn bộ entity TmsEquipment sang để gói tin gọn và không lệ thuộc schema DB.
    /// </summary>
    public class VmsEquipmentInfo
    {
        public string? EquipmentId { get; set; }

        public string? Code { get; set; }

        public string? Name { get; set; }

        /// <summary>ID loại thiết bị, BE cần lại khi dựng bản ghi lịch sử.</summary>
        public string? EquipmentTypeId { get; set; }

        /// <summary>Mã loại thiết bị ("vms" / "lcs") — để dịch vụ hiển thị và lọc.</summary>
        public string? EquipmentTypeCode { get; set; }

        /// <summary>"chainzone" | "dysten".</summary>
        public string? Vendor { get; set; }

        public string? Ip { get; set; }

        /// <summary>Null/0 -> dịch vụ dùng 80.</summary>
        public int? Port { get; set; }

        /// <summary>Cấu hình thiết bị dạng JSON (Width/Height/PortApi...).</summary>
        public string? Config { get; set; }
    }

    /// <summary>
    /// Dịch vụ đẩy một lượt đọc telemetry về BE. Một gói mang nhiều thiết bị của cùng một lượt quét.
    /// Gói này KHÔNG có RequestId do BE tạo nên BE xử lý như dữ liệu đến tự do, không qua VmsControlBridge.
    /// </summary>
    public class VmsTelemetryPush
    {
        public string? Type { get; set; }

        /// <summary>Nguồn số liệu: "auto" (vòng lặp) hoặc "manual" (bấm quét tay) — khớp VmsStatConst.Source.</summary>
        public string? Source { get; set; }

        /// <summary>Thời điểm dịch vụ hoàn tất lượt quét.</summary>
        public DateTime CollectedAt { get; set; }

        public List<VmsTelemetryItem> Items { get; set; } = new List<VmsTelemetryItem>();
    }

    /// <summary>Kết quả đọc thông số của một thiết bị trong lượt quét.</summary>
    public class VmsTelemetryItem
    {
        public string? EquipmentId { get; set; }

        /// <summary>Mã thiết bị — chỉ để đọc log cho dễ, BE vẫn khớp theo EquipmentId.</summary>
        public string? EquipmentCode { get; set; }

        public bool Success { get; set; }

        public string? Message { get; set; }

        /// <summary>
        /// Thông số đọc được, khóa theo IntgVmsStatType ("temperatureIn", "powerStatus"...).
        /// BE gom nhóm thành resource/environment/diagnostic khi lưu.
        /// </summary>
        public Dictionary<string, object>? Stats { get; set; }
    }
}
