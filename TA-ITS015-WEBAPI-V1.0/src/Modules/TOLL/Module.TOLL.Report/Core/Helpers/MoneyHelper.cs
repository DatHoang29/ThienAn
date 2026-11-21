using System.Text;
using System.Text.RegularExpressions;

namespace Module.TOLL.Report.Core.Helpers
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Helper đọc số tiền thành chữ (tiếng Việt) cho dòng "Bằng chữ"
    /// Created date: 28/07/2026
    /// Lưu ý: bản cơ bản, xử lý đến hàng tỷ; nếu cần > tỷ tỷ thì mở rộng thêm.
    /// </summary>
    public static class MoneyHelper
    {
        private static readonly string[] _digits =
            { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

        // Đơn vị theo nhóm 3 chữ số
        private static readonly string[] _units = { "", "nghìn", "triệu", "tỷ" };

        /// <summary>
        /// Đọc số tiền (VND) thành chữ. Ví dụ 1.250.000 => "Một triệu hai trăm năm mươi nghìn đồng".
        /// </summary>
        public static string DocSoThanhChu(decimal value)
        {
            long number = (long)Math.Round(value, MidpointRounding.AwayFromZero);
            if (number == 0) return "Không đồng";

            bool negative = number < 0;
            number = Math.Abs(number);

            // Tách thành các nhóm 3 chữ số (từ thấp -> cao)
            var groups = new List<int>();
            while (number > 0)
            {
                groups.Add((int)(number % 1000));
                number /= 1000;
            }

            var sb = new StringBuilder();
            for (int i = groups.Count - 1; i >= 0; i--)
            {
                if (groups[i] == 0) continue;

                bool full = i != groups.Count - 1; // các nhóm phía sau luôn đọc đủ 3 chữ số
                sb.Append(ReadGroup(groups[i], full));

                if (i > 0)
                    sb.Append(_units[i % 4]).Append(' ');
            }

            var result = Regex.Replace(sb.ToString().Trim(), "\\s+", " ");
            if (result.Length > 0)
                result = char.ToUpper(result[0]) + result.Substring(1);

            return (negative ? "Âm " : string.Empty) + result + " đồng";
        }

        /// <summary>
        /// Đọc 1 nhóm 3 chữ số.
        /// </summary>
        private static string ReadGroup(int number, bool full)
        {
            int tram = number / 100;
            int chuc = (number % 100) / 10;
            int donvi = number % 10;
            var sb = new StringBuilder();

            if (tram > 0 || full)
                sb.Append(_digits[tram]).Append(" trăm ");

            if (chuc == 0)
            {
                if (donvi > 0)
                    sb.Append("lẻ ").Append(_digits[donvi]).Append(' ');
            }
            else if (chuc == 1)
            {
                sb.Append("mười ");
                if (donvi == 5) sb.Append("lăm ");
                else if (donvi > 0) sb.Append(_digits[donvi]).Append(' ');
            }
            else
            {
                sb.Append(_digits[chuc]).Append(" mươi ");
                if (donvi == 1) sb.Append("mốt ");
                else if (donvi == 5) sb.Append("lăm ");
                else if (donvi > 0) sb.Append(_digits[donvi]).Append(' ');
            }

            return sb.ToString();
        }
    }
}
