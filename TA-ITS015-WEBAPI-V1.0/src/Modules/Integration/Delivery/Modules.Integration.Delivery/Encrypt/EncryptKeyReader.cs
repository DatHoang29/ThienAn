using Microsoft.Extensions.Configuration;

namespace Modules.Integration.Delivery.Encrypt
{
    /// <summary>
    /// Description: Đọc khóa mã hóa từ IConfiguration theo cấu hình ShareDataSecurity:Keys:{keyRef}, giá trị chuỗi Base64.
    /// Created date: 14/10/2026
    /// Modified date: 19/10/2026 (Gỡ IEncryptKeyReader)
    /// </summary>
    public class EncryptKeyReader(IConfiguration configuration)
    {
        /// <summary>
        /// Lấy khóa mã hóa (dạng byte) theo tên tham chiếu; không có thì trả null.
        /// </summary>
        public byte[]? GetKey(string keyRef)
        {
            var keyBase64 = configuration["ShareDataSecurity:Keys:" + keyRef];
            if (string.IsNullOrWhiteSpace(keyBase64))
            {
                return null;
            }

            return Convert.FromBase64String(keyBase64.Trim());
        }
    }
}
