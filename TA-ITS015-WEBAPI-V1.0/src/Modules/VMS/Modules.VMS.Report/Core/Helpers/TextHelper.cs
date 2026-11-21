namespace Modules.VMS.Report.Core.Helpers
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Helper định dạng văn bản cho báo cáo
    /// Created date: 07/10/2026
    /// </summary>
    internal class TextHelper
    {
        /// <summary>
        /// Author: ChuongLP
        /// Description: Tạo chuỗi ngày ký dạng "Ngày dd Tháng MM Năm yyyy"
        /// Created date: 07/10/2026
        /// </summary>
        internal static string SignatureDate(DateTime date)
        {
            return $"Ngày {date.ToString("dd")} Tháng {date.ToString("MM")} Năm {date.ToString("yyyy")}";
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Hỗ trợ tạo text tránh bị tự động merge cột
        /// Created date: 07/10/2026
        /// </summary>
        internal static string PreventMergeText(int pos, string value)
        {
            if (pos % 2 == 0)
                return value;
            else return value + "  ";
        }
    }
}
