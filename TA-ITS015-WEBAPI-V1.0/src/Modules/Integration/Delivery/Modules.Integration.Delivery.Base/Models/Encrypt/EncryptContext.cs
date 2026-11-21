
namespace Modules.Integration.Delivery.Base.Models.Encrypt
{
    /// <summary>
    /// Description: Ngữ cảnh cung cấp khóa và metadata cho bộ mã hoá payload hai chiều.
    /// Created date: 14/10/2026
    /// Modified date: 18/10/2026 (Chuyển namespace sang Encrypt)
    /// </summary>
    public sealed record EncryptContext(
        string? KeyRef,
        byte[]? KeyBytes,
        IDictionary<string, string>? Metadata = null);
}
