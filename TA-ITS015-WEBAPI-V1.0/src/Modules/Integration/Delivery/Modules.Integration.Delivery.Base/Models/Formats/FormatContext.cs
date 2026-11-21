namespace Modules.Integration.Delivery.Base.Models.Formats
{
    /// <summary>
    /// Description: Ngữ cảnh cung cấp siêu dữ liệu cho bộ chuyển đổi định dạng payload hai chiều.
    ///              Thuần BCL.
    /// Created date: 14/10/2026
    /// </summary>
    public sealed record FormatContext(
        string? PartnerCode = null,
        string? PacketCode = null,
        long? SerialNbr = null,
        DateTime? ProcessedAt = null,
        IDictionary<string, string>? Metadata = null);
}
