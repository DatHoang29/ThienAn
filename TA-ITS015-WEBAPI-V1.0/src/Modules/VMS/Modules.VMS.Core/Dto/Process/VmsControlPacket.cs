using System;
using System.Collections.Generic;

namespace Modules.VMS.Controllers.Process.Dto
{
    /// <summary>
    /// Danh sách lệnh (Action) trên kênh ta.its.control.vms.
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public static class VmsControlAction
    {
        /// <summary>Gửi nội dung (hình ảnh) + độ sáng ra bảng.</summary>
        public const string SendContent = "send-content";

        /// <summary>Đọc ảnh thực tế đang hiển thị trên bảng (trả về Base64).</summary>
        public const string ReadImage = "read-image";

        /// <summary>Đọc thông số kỹ thuật (nhiệt độ, quạt, nguồn, lỗi LED...).</summary>
        public const string GetStat = "get-stat";

        /// <summary>Thiết lập độ sáng độc lập.</summary>
        public const string SetBrightness = "set-brightness";

        /// <summary>Kiểm tra kết nối tới thiết bị.</summary>
        public const string CheckConnect = "check-connect";
    }

    /// <summary>Loại gói tin trên kênh dùng chung 2 chiều.</summary>
    public static class VmsControlType
    {
        /// <summary>BE gửi -> dịch vụ ITS.VMS.Worker nhận và xử lý.</summary>
        public const string Request = "request";

        /// <summary>Dịch vụ gửi -> BE nhận, đối chiếu theo RequestId.</summary>
        public const string Result = "result";
    }

    /// <summary>
    /// Gói tin YÊU CẦU BE phát lên kênh ta.its.control.vms (Type = "request").
    /// CẢNH BÁO: các lớp trong file này phải TRÙNG TÊN THUỘC TÍNH với DTO cùng tên ở
    /// ITS.VMS.Core/Messaging/VmsControlRequest.cs. Cố tình trùng lặp thay vì tách project dùng chung,
    /// vì dịch vụ Worker chạy độc lập không được kéo theo cây phụ thuộc Furion của BE.
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public class VmsControlRequest
    {
        public string? Type { get; set; } = VmsControlType.Request;

        /// <summary>Mã truy vết đối chiếu request/result (Guid 32 ký tự).</summary>
        public string? RequestId { get; set; }

        /// <summary>Lệnh cần thực hiện: xem <see cref="VmsControlAction"/>.</summary>
        public string? Action { get; set; }

        /// <summary>true = dịch vụ trả thành công giả lập, không gọi thiết bị thật.</summary>
        public bool Simulate { get; set; }
        public string? ImageBase64 { get; set; }

        /// <summary>Đuôi file khi dịch vụ ghi ảnh ra đĩa, mặc định "bmp" (SDK hãng nhận đường dẫn file).</summary>
        public string? ImageFormat { get; set; }

        /// <summary>
        /// Danh sách thông số cần đọc cho lệnh get-stat, dùng chung cả mẻ.
        /// Theo hằng IntgVmsStatType; để trống thì dịch vụ đọc tất cả.
        /// </summary>
        public string[]? StatTypes { get; set; }

        /// <summary>Danh sách thiết bị đích kèm đầy đủ thông tin kết nối.</summary>
        public List<VmsControlTarget> Targets { get; set; } = new List<VmsControlTarget>();

        public DateTime RequestTime { get; set; } = DateTime.Now;
    }

    /// <summary>Một thiết bị đích của lệnh.</summary>
    public class VmsControlTarget
    {
        public string? EquipmentId { get; set; }

        /// <summary>Hãng thiết bị (IntgVmsVendor): "chainzone" | "dysten".</summary>
        public string? Vendor { get; set; }

        public string? Ip { get; set; }

        /// <summary>Port SDK. Null/0 -> dịch vụ dùng mặc định 80.</summary>
        public int? Port { get; set; }

        /// <summary>Cấu hình thiết bị dạng JSON (Width/Height/PortApi...).</summary>
        public string? Config { get; set; }

        /// <summary>Đường dẫn ảnh cho send-content (tương đối hoặc HTTP URL).</summary>
        public string? Url { get; set; }

        /// <summary>Độ sáng: 0 = tự động, 1-100 = mức sáng.</summary>
        public int Brightness { get; set; }
    }

    /// <summary>Gói tin KẾT QUẢ dịch vụ phát về (Type = "result").</summary>
    public class VmsControlResult
    {
        public string? Type { get; set; }

        public string? RequestId { get; set; }

        public string? Action { get; set; }

        /// <summary>true khi TẤT CẢ thiết bị trong gói đều thành công.</summary>
        public bool Success { get; set; }

        public string? Message { get; set; }

        public DateTime FinishedAt { get; set; }

        /// <summary>Chi tiết kết quả từng thiết bị.</summary>
        public List<VmsTargetResult> Results { get; set; } = new List<VmsTargetResult>();
    }

    /// <summary>Kết quả cho một thiết bị cụ thể.</summary>
    public class VmsTargetResult
    {
        public string? EquipmentId { get; set; }

        public bool Success { get; set; }

        public string? Message { get; set; }

        public bool SendImageOk { get; set; }

        public bool SetBrightnessOk { get; set; }

        /// <summary>
        /// Dữ liệu trả kèm theo Action: read-image -> Base64; get-stat -> object thông số;
        /// check-connect -> true/false; send-content -> null.
        /// </summary>
        public object? Data { get; set; }
    }
}
