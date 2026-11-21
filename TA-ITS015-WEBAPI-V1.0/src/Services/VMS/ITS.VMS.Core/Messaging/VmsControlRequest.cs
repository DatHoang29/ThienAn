using System;
using System.Collections.Generic;

namespace ITS.VMS.Core.Messaging
{
    /// <summary>
    /// Danh sách lệnh (Action) hỗ trợ trên kênh ta.its.control.vms.
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

        /// <summary>Kiểm tra kết nối tới thiết bị (ping + mở port).</summary>
        public const string CheckConnect = "check-connect";
    }

    /// <summary>
    /// Loại gói tin trên kênh dùng chung 2 chiều.
    /// </summary>
    public static class VmsControlType
    {
        /// <summary>BE gửi -> dịch vụ nhận và xử lý.</summary>
        public const string Request = "request";

        /// <summary>Dịch vụ gửi -> BE nhận, đối chiếu theo RequestId để bắn Event cập nhật DB/log/socket.</summary>
        public const string Result = "result";
    }

    /// <summary>
    /// Gói tin YÊU CẦU trên kênh ta.its.control.vms (Type = "request").
    /// 1 gói mang danh sách nhiều thiết bị, có thể nhiều hãng khác nhau; dịch vụ xử lý song song.
    /// Dịch vụ STATELESS: mọi thông tin kết nối thiết bị do BE đóng gói sẵn trong <see cref="Targets"/>.
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public class VmsControlRequest
    {
        /// <summary>Loại gói tin: xem <see cref="VmsControlType"/>. Dịch vụ BỎ QUA mọi gói khác "request".</summary>
        public string? Type { get; set; }

        /// <summary>Mã truy vết để BE đối chiếu request/result (Guid 32 ký tự).</summary>
        public string? RequestId { get; set; }

        /// <summary>Lệnh cần thực hiện: xem <see cref="VmsControlAction"/>.</summary>
        public string? Action { get; set; }

        /// <summary>true = dịch vụ trả thành công giả lập, không gọi thiết bị thật (test xuyên suốt luồng NATS).</summary>
        public bool Simulate { get; set; }
        public string? ImageBase64 { get; set; }
        public string? ImageFormat { get; set; }

        /// <summary>
        /// Danh sách thông số cần đọc cho lệnh get-stat, dùng chung cả mẻ.
        /// Theo hằng IntgVmsStatType ("temperatureIn", "powerStatus", "brightness"...).
        /// Để trống thì dịch vụ đọc tất cả ("all").
        /// </summary>
        public string[]? StatTypes { get; set; }
        public List<VmsControlTarget> Targets { get; set; } = new();
        public DateTime RequestTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Một thiết bị đích kèm đầy đủ thông tin kết nối do BE cung cấp.
    /// </summary>
    public class VmsControlTarget
    {
        /// <summary>ID thiết bị (khóa đối chiếu kết quả về đúng bản ghi bên BE).</summary>
        public string? EquipmentId { get; set; }

        /// <summary>Hãng thiết bị: "chainzone" | "dysten" (xem IntgVmsVendor).</summary>
        public string? Vendor { get; set; }

        public string? Ip { get; set; }

        /// <summary>Port SDK. Null/0 -> dịch vụ dùng mặc định 80.</summary>
        public int? Port { get; set; }

        /// <summary>Cấu hình thiết bị dạng JSON (Width/Height/PortApi...), đổ vào Metadata của driver.</summary>
        public string? Config { get; set; }

        /// <summary>Đường dẫn ảnh cho send-content. Hỗ trợ đường dẫn tương đối hoặc HTTP URL.</summary>
        public string? Url { get; set; }

        /// <summary>Độ sáng: 0 = tự động, 1-100 = mức sáng.</summary>
        public int Brightness { get; set; }
    }

    /// <summary>
    /// Gói tin KẾT QUẢ dịch vụ phát lại (Type = "result"), giữ nguyên RequestId/Action của request.
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public sealed class VmsControlResult
    {
        public string Type { get; set; } = VmsControlType.Result;

        public string? RequestId { get; set; }

        public string? Action { get; set; }

        /// <summary>true khi TẤT CẢ thiết bị trong gói đều thành công.</summary>
        public bool Success { get; set; }

        /// <summary>Thông báo tổng hợp (đọc được cho người dùng).</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Thời điểm hoàn tất xử lý (giờ local của máy chạy dịch vụ).</summary>
        public DateTime FinishedAt { get; set; } = DateTime.Now;

        /// <summary>Chi tiết kết quả từng thiết bị.</summary>
        public List<VmsTargetResult> Results { get; set; } = new();
    }

    /// <summary>Kết quả cho một thiết bị cụ thể.</summary>
    public sealed class VmsTargetResult
    {
        public string EquipmentId { get; set; } = string.Empty;

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        /// <summary>Riêng send-content: kết quả bước gửi ảnh.</summary>
        public bool SendImageOk { get; set; }

        /// <summary>Riêng send-content / set-brightness: kết quả bước đặt độ sáng.</summary>
        public bool SetBrightnessOk { get; set; }

        /// <summary>
        /// Dữ liệu trả kèm theo Action:
        /// read-image -> chuỗi Base64; get-stat -> Dictionary thông số; check-connect -> true/false; send-content -> null.
        /// </summary>
        public object? Data { get; set; }
    }
}
