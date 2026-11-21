using Modules.Integration.Delivery.Base.Enums;

namespace Modules.Integration.Delivery.Base.Models
{
    /// <summary>
    /// Description: Hợp đồng dữ liệu 3 đầu vào chuẩn cho tầng gửi dữ liệu tới đối tác (HandleConnection):
    ///              JsonPayload, SecurityInfo, ConnectionConfig.
    /// Created date: 13/10/2026
    /// Modified date: 15/10/2026
    /// </summary>
    public sealed class DeliveryRequest
    {
        /// <summary>1. Cục dữ liệu sau khi đã xử lý (thống nhất đầu vào là chuỗi JSON)</summary>
        public string JsonPayload { get; init; } = string.Empty;

        /// <summary>Mảng byte dữ liệu thực gửi sau format (chưa mã hóa theo hiện trạng)</summary>
        public byte[]? PayloadBytes { get; init; }

        /// <summary>MIME Content-Type của dữ liệu (application/json, text/csv...)</summary>
        public string ContentType { get; init; } = "application/json";

        /// <summary>Phần mở rộng tệp (.json, .csv...)</summary>
        public string FileExtension { get; init; } = ".json";

        /// <summary>Header truyền vận đi kèm</summary>
        public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>2. Thông tin bảo mật / xác thực của đối tác (Token, API Key, Username/Password...)</summary>
        public IDictionary<string, string> SecurityInfo { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>3. Cấu hình kết nối động dạng Key-Value (IP, Port, EndPointApiUrl, Topic/Channel, HttpTimeoutSec...)</summary>
        public IDictionary<string, string> ConnectConfig { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Loại kết nối của đối tác nhận (API, Kafka, File...)</summary>
        public ConnectionType ConnectType { get; init; } = ConnectionType.Api;
    }
}
