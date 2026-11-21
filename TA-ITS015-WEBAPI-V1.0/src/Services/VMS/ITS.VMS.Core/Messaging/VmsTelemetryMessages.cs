using System;
using System.Collections.Generic;

namespace ITS.VMS.Core.Messaging
{
    /// <summary>
    /// Phân loại gói tin trên kênh dữ liệu VMS. Cả 3 loại đi CHUNG một subject
    /// (<see cref="VmsTelemetrySubject.Data"/>), phân biệt bằng trường "Type" để không phải
    /// mở thêm nhiều subject.
    ///
    /// LƯU Ý: subject này còn được BE dùng để bắn trạng thái gửi ảnh cho FE (vms-success / vms-fail).
    /// Vì vậy mọi bên nhận PHẢI lọc theo Type và bỏ qua gói không thuộc 3 loại dưới đây.
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

    /// <summary>Subject dùng cho luồng dữ liệu telemetry.</summary>
    public static class VmsTelemetrySubject
    {
        /// <summary>
        /// Kênh dữ liệu VMS dùng chung cho cả 3 loại gói (hỏi thiết bị / trả thiết bị / đẩy telemetry).
        /// Trùng với ItsDataTransporterChannel.VmsUpdateImage bên BE.
        /// </summary>
        public const string Data = "ta.its.data.vms";
    }

    /// <summary>
    /// Gói tin của luồng THU THẬP TELEMETRY do dịch vụ tự giữ nhịp.
    ///
    /// Khác luồng điều khiển (ta.its.control.vms) ở chiều khởi phát:
    ///   - Điều khiển : BE ra lệnh -> dịch vụ thực thi.
    ///   - Telemetry  : dịch vụ tự chạy -> hỏi BE danh sách thiết bị, rồi đẩy số liệu về BE lưu DB.
    /// </summary>
    public sealed class VmsEquipmentSyncRequest
    {
        public string Type { get; set; } = VmsDataType.EquipmentRequest;

        /// <summary>Mã truy vết để đối chiếu với gói trả về.</summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Danh sách mã loại thiết bị cần lấy ("vms", "lcs"). Để trống thì BE trả theo mặc định của nó.
        /// </summary>
        public List<string>? TypeCodes { get; set; }

        public DateTime RequestTime { get; set; } = DateTime.Now;
    }

    /// <summary>BE trả danh sách thiết bị kèm đủ thông tin kết nối để dịch vụ đọc được thiết bị.</summary>
    public sealed class VmsEquipmentSyncResult
    {
        public string Type { get; set; } = VmsDataType.EquipmentResult;

        public string? RequestId { get; set; }

        public bool Success { get; set; }

        public string? Message { get; set; }

        /// <summary>Thời điểm BE lấy dữ liệu, dùng để biết file cache cũ bao lâu.</summary>
        public DateTime SyncedAt { get; set; } = DateTime.Now;

        public List<VmsEquipmentInfo> Equipments { get; set; } = new();
    }

    /// <summary>
    /// Thông tin một thiết bị dịch vụ cần để đọc telemetry. Chỉ lấy đúng những trường cần dùng,
    /// KHÔNG mang toàn bộ entity TmsEquipment sang để gói tin gọn và không lệ thuộc schema DB.
    /// </summary>
    public sealed class VmsEquipmentInfo
    {
        public string EquipmentId { get; set; } = string.Empty;

        public string? Code { get; set; }

        public string? Name { get; set; }

        /// <summary>ID loại thiết bị, BE cần lại khi dựng bản ghi lịch sử.</summary>
        public string? EquipmentTypeId { get; set; }

        /// <summary>Mã loại thiết bị ("vms" / "lcs") — để form hiển thị và lọc.</summary>
        public string? EquipmentTypeCode { get; set; }

        /// <summary>"chainzone" | "dysten".</summary>
        public string? Vendor { get; set; }

        public string? Ip { get; set; }

        /// <summary>Null/0 -> dịch vụ dùng 80.</summary>
        public int? Port { get; set; }

        /// <summary>Cấu hình thiết bị dạng JSON (Width/Height/PortApi...).</summary>
        public string? Config { get; set; }

        /// <summary>Đã đủ thông tin để gọi ra thiết bị hay chưa.</summary>
        public bool IsReady() => !string.IsNullOrWhiteSpace(Vendor) && !string.IsNullOrWhiteSpace(Ip);
    }

    /// <summary>
    /// Dịch vụ đẩy một lượt đọc telemetry về BE. Một gói mang nhiều thiết bị của cùng một lượt quét.
    /// </summary>
    public sealed class VmsTelemetryPush
    {
        public string Type { get; set; } = VmsDataType.Telemetry;

        /// <summary>Nguồn lấy số liệu: "auto" (vòng lặp) hoặc "manual" (bấm quét ngay trên form).</summary>
        public string Source { get; set; } = "auto";

        /// <summary>Thời điểm dịch vụ hoàn tất lượt quét (giờ local của máy chạy dịch vụ).</summary>
        public DateTime CollectedAt { get; set; } = DateTime.Now;

        public List<VmsTelemetryItem> Items { get; set; } = new();
    }

    /// <summary>Kết quả đọc thông số của một thiết bị trong lượt quét.</summary>
    public sealed class VmsTelemetryItem
    {
        public string EquipmentId { get; set; } = string.Empty;

        /// <summary>Mã thiết bị — chỉ để đọc log/hiển thị cho dễ, BE vẫn khớp theo EquipmentId.</summary>
        public string? EquipmentCode { get; set; }

        public bool Success { get; set; }

        public string? Message { get; set; }

        /// <summary>
        /// Thông số đọc được, khóa theo IntgVmsStatType ("temperatureIn", "powerStatus"...).
        /// BE gom nhóm lại thành resource/environment/diagnostic khi lưu.
        /// </summary>
        public Dictionary<string, object>? Stats { get; set; }
    }
}
