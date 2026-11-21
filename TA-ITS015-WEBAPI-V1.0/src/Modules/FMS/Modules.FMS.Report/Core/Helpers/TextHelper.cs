namespace Modules.FMS.Report.Core.Helpers
{
    internal class TextHelper
    {
        /// <summary>
        /// Tạo ngày ký
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static string SignatureDate(DateTime date)
        {
            return $"Ngày {date.ToString("dd")} Tháng {date.ToString("MM")} Năm {date.ToString("yyyy")}";
        }


        /// <summary>
        /// Hỗ trợ tạo text để tránh bị merge cột
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string PreventMergeText(int pos, string value)
        {
            if (pos % 2 == 0)
                return value;
            else return value + "  ";
        }


    }
}
