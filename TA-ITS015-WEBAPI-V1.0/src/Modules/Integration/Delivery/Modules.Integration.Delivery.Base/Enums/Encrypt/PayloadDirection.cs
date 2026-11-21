namespace Modules.Integration.Delivery.Base.Enums.Encrypt
{
    /// <summary>
    /// Description: Chiều truyền nhận dữ liệu của kênh kết nối (Outbound: gửi đi, Inbound: nhận về).
    ///              Dùng phân định khóa và bộ mã hóa/giải mã encryptor tương ứng.
    /// Created date: 14/10/2026
    /// Modified date: 18/10/2026 (Chuyển namespace sang Encrypt)
    /// </summary>
    public enum PayloadDirection
    {
        Outbound = 1,
        Inbound = 2
    }
}
