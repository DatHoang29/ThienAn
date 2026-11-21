namespace Modules.Integration.Delivery.Base.Models
{
    /// <summary>
    /// Description: Kết quả thực thi gửi qua kênh kết nối tương ứng (API / Kafka / File / FTP)
    /// Created date: 13/10/2026
    /// Modified date: 13/11/2026
    /// </summary>
    public sealed class DeliveryResult
    {
        public bool Success { get; init; }
        public string Channel { get; init; } = string.Empty;
        public int? StatusCode { get; init; }
        public int SentByteSize { get; init; }
        public int ByteSize => SentByteSize;
        public string? TargetAddressOrPath { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public string? DetailJson { get; init; }
        /// <summary>Gói đã rời bên gửi mà không nhận được trả lời (đứt kết nối giữa chừng, hết giờ chờ): đối tác có thể đã nhận gói.</summary>
        public bool IsPossiblyDelivered { get; init; }
        public List<string> Warnings { get; set; } = [];
    }
}
