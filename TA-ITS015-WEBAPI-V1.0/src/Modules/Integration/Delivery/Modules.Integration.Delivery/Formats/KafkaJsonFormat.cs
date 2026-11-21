using System.Text;
using Modules.Integration.Delivery.Base.Models.Formats;

namespace Modules.Integration.Delivery.Formats
{
    /// <summary>
    /// Description: Định dạng JSON truyền qua Kafka Broker.
    ///              Payload là mảng byte UTF-8 của JSON, gắn kèm 4 headers chuẩn hóa:
    ///              content-type, PartnerCode, PacketCode, SerialNbr (đối xứng với REST).
    ///              Thuần BCL.
    /// Created date: 14/10/2026
    /// </summary>
    public class KafkaJsonFormat
    {
        public string Name => "KafkaJson";

        /// <summary>
        /// Author: DatHQ
        /// Description: Mã hóa JSON sang UTF-8 để truyền, kèm content-type và các header định danh đối tác, gói tin.
        /// Created date: 14/10/2026
        /// </summary>
        public TransportPayload ToTransport(string json, FormatContext ctx)
        {
            var bytes = string.IsNullOrEmpty(json)
                ? []
                : Encoding.UTF8.GetBytes(json);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["content-type"] = "application/json",
                ["PartnerCode"] = ctx.PartnerCode ?? string.Empty,
                ["PacketCode"] = ctx.PacketCode ?? string.Empty,
                ["SerialNbr"] = ctx.SerialNbr?.ToString() ?? "0"
            };

            if (ctx.ProcessedAt.HasValue)
            {
                headers["ProcessedAt"] = ctx.ProcessedAt.Value.ToString("O");
            }

            return new TransportPayload(bytes, "application/json", ".json", headers);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Giải mã UTF-8 về chuỗi JSON; không có dữ liệu thì trả chuỗi rỗng.
        /// Created date: 14/10/2026
        /// </summary>
        public string FromTransport(byte[] bytes, FormatContext ctx)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(bytes);
        }
    }
}
