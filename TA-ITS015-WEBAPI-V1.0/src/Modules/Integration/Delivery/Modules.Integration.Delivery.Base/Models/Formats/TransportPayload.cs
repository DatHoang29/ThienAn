namespace Modules.Integration.Delivery.Base.Models.Formats
{
    /// <summary>
    /// Description: Dữ liệu vận chuyển sau khi chuyển đổi định dạng (Bytes, ContentType, Headers...).
    ///              Thuần BCL.
    /// Created date: 14/10/2026
    /// </summary>
    public sealed class TransportPayload
    {
        public byte[] Bytes { get; set; } = [];
        public string ContentType { get; set; } = "application/json";
        public string FileExtension { get; set; } = ".json";
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public TransportPayload() { }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo dữ liệu truyền; headers được chép sang dictionary không phân biệt hoa thường.
        /// Created date: 14/10/2026
        /// </summary>
        public TransportPayload(byte[] bytes, string contentType, string fileExtension, IDictionary<string, string>? headers = null)
        {
            Bytes = bytes ?? [];
            ContentType = contentType;
            FileExtension = fileExtension;
            if (headers != null)
            {
                Headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
