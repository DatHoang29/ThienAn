
namespace Modules.Integration.Delivery.Base.Constants
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Tên các thuật toán / bộ mã hóa payload chuẩn dùng chung.
    /// Created date: 16/10/2026
    /// Modified date: 18/10/2026 (Đổi tên thành EncryptNames)
    /// </summary>
    public static class EncryptNames
    {
        public const string None = "None";
        public const string AesGcm = "AesGcm";
        public static readonly string[] SupportedValues = [None, AesGcm];

        /// <summary>
        /// Author: DatHQ
        /// Description: Tên thuật toán có thuộc danh sách mã hóa được hỗ trợ không; trống trả false.
        /// Created date: 16/10/2026
        /// </summary>
        public static bool IsSupported(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            foreach (var s in SupportedValues)
            {
                if (string.Equals(s, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
