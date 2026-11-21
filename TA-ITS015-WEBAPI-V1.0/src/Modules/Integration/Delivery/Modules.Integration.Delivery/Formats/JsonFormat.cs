using System.Text;
using Modules.Integration.Delivery.Base.Models.Formats;

namespace Modules.Integration.Delivery.Formats
{
    /// <summary>
    /// Description: Định dạng JSON chuẩn hóa hai chiều, mã hóa UTF-8 không BOM.
    ///              Thuần BCL.
    /// Created date: 14/10/2026
    /// </summary>
    public class JsonFormat
    {
        public string Name => "Json";

        /// <summary>
        /// Author: DatHQ
        /// Description: Mã hóa JSON sang UTF-8 để truyền, kèm header content-type.
        /// Created date: 14/10/2026
        /// </summary>
        public TransportPayload ToTransport(string json, FormatContext ctx)
        {
            var bytes = string.IsNullOrEmpty(json)
                ? []
                : Encoding.UTF8.GetBytes(json);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["content-type"] = "application/json; charset=utf-8"
            };

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
