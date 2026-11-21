namespace Modules.Report.Core.Extensions
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Helper đọc giá trị cấu hình thông tin báo cáo đã gom nhóm (RptInfo).
    /// Created date: 27/07/2026
    /// </summary>
    public static class RptInfoConfigExtensions
    {
        /// <summary>
        /// Lấy giá trị cấu hình theo nhóm + key. Không có cấu hình thì trả về fallback.
        /// </summary>
        /// <param name="info">Cấu hình đã gom nhóm: group -> key -> value</param>
        /// <param name="groupCode">Mã nhóm (header/footer/signature/company)</param>
        /// <param name="code">Mã key (logo/title/left_name...)</param>
        /// <param name="fallback">Giá trị dùng khi chưa cấu hình</param>
        public static string? GetInfo(this Dictionary<string, Dictionary<string, string?>>? info, string groupCode, string code, string? fallback = null)
        {
            if (info == null || string.IsNullOrWhiteSpace(groupCode) || string.IsNullOrWhiteSpace(code))
                return fallback;

            if (!info.TryGetValue(groupCode, out var group) || group == null)
                return fallback;

            if (!group.TryGetValue(code, out var value) || string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Trim();
        }
    }
}
