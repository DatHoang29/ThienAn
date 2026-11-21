namespace Modules.Integration.Delivery.Base.Constants
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Danh mục các phương thức xác thực (AuthType) được hỗ trợ cho kênh kết nối đối tác.
    /// Created date: 16/10/2026
    /// </summary>
    public static class AuthTypes
    {
        public const string None = "None";
        public const string Bearer = "Bearer";
        public const string ApiKey = "ApiKey";
        public const string Basic = "Basic";

        public static readonly string[] SupportedValues = [None, Bearer, ApiKey, Basic];

        /// <summary>
        /// Author: DatHQ
        /// Description: Giá trị có thuộc danh sách kiểu xác thực được hỗ trợ không; trống trả false.
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
