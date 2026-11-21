namespace Modules.Integration.Delivery.Base.Models
{
    /// <summary>
    /// Description: Ngữ cảnh giao vận gói tin thuần BCL, không phụ thuộc entity,
    ///              cung cấp thông tin đối tác/gói tin/đăng ký cho các lớp gửi (HttpDeliverySender, FileDeliverySender, FtpDeliverySender, KafkaDeliverySender).
    /// Created date: 15/10/2026
    /// </summary>
    public sealed record DeliveryContext(
        string? PartnerCode = null,
        string? PacketCode = null,
        string? SubscriptionId = null,
        long? SerialNbr = null,
        string? DatatypeId = null,
        DateTime ExportedAt = default,
        string? Address = null,
        int? Port = null,
        string? EndPointApiUrl = null);
}
