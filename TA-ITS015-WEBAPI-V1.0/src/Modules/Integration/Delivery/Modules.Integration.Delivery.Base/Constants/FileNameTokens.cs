namespace Modules.Integration.Delivery.Base.Constants
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Danh mục các tham số biến đổi (token) hỗ trợ trong cấu hình mẫu tên tệp kết xuất (FileNamePattern).
    /// Created date: 16/10/2026
    /// </summary>
    public static class FileNameTokens
    {
        public const string Serial = "{serial}";
        public const string Timestamp = "{timestamp}";
        public const string Packet = "{packet}";
        public const string Partner = "{partner}";

        public static readonly string[] RequiredPatternTokens = [Serial, Timestamp];

        /// <summary>
        /// Author: DatHQ
        /// Description: Mẫu tên tệp có chứa ít nhất một trong hai biến số thứ tự, thời gian không; trống trả false.
        /// Created date: 16/10/2026
        /// </summary>
        public static bool HasRequiredToken(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            return pattern.Contains(Serial, StringComparison.OrdinalIgnoreCase) ||
                   pattern.Contains(Timestamp, StringComparison.OrdinalIgnoreCase);
        }
    }
}
