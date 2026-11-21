using Modules.Integration.Delivery.Base.Enums;
using Modules.Integration.Delivery.Base.Models.Formats;

namespace Modules.Integration.Delivery.Formats
{
    /// <summary>
    /// Description: Sổ đăng ký các định dạng dữ liệu payload hai chiều (Format Registry).
    ///              Tra định dạng theo tên định dạng hoặc theo loại kênh kết nối.
    ///              Thuần BCL.
    /// Created date: 14/10/2026
    /// </summary>
    public static class PayloadFormatRegistry
    {
        private static readonly JsonFormat Json = new JsonFormat();
        private static readonly CsvFormat Csv = new CsvFormat();
        private static readonly KafkaJsonFormat KafkaJson = new KafkaJsonFormat();

        /// <summary>
        /// Chiều gửi: chọn định dạng theo tên định dạng (từ key "Format" trong cấu hình) và loại kênh kết nối,
        /// rồi chuyển chuỗi JSON nghiệp vụ sang dạng truyền của kênh.
        /// </summary>
        public static TransportPayload ToTransport(string? formatName, ConnectionType? connectionType, string json, FormatContext ctx)
        {
            if (connectionType == ConnectionType.Kafka)
            {
                return KafkaJson.ToTransport(json, ctx);
            }

            if (string.Equals(formatName, "Csv", StringComparison.OrdinalIgnoreCase))
            {
                return Csv.ToTransport(json, ctx);
            }

            if (string.Equals(formatName, "KafkaJson", StringComparison.OrdinalIgnoreCase))
            {
                return KafkaJson.ToTransport(json, ctx);
            }

            return Json.ToTransport(json, ctx);
        }

        /// <summary>
        /// Chiều nhận: chọn định dạng theo cùng quy tắc chiều gửi rồi chuyển mảng byte từ kênh truyền về chuỗi JSON chuẩn hóa.
        /// </summary>
        public static string FromTransport(string? formatName, ConnectionType? connectionType, byte[] bytes, FormatContext ctx)
        {
            if (connectionType == ConnectionType.Kafka)
            {
                return KafkaJson.FromTransport(bytes, ctx);
            }

            if (string.Equals(formatName, "Csv", StringComparison.OrdinalIgnoreCase))
            {
                return Csv.FromTransport(bytes, ctx);
            }

            if (string.Equals(formatName, "KafkaJson", StringComparison.OrdinalIgnoreCase))
            {
                return KafkaJson.FromTransport(bytes, ctx);
            }

            return Json.FromTransport(bytes, ctx);
        }
    }
}
