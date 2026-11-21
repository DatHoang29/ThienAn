using System.Globalization;
using System.Text.RegularExpressions;

namespace Shared.Utility.Helpers
{
    /// <summary>
    /// Author: KienLT
    /// Description: Đọc cấu hình THỜI LƯỢNG dạng chuỗi có hậu tố đơn vị và trả về SỐ GIÂY.
    ///
    /// <para><b>Quy ước:</b> <c>s</c> = giây, <c>m</c> = phút, <c>h</c> = giờ, <c>d</c> = ngày. KHÔNG có hậu
    /// tố thì tính bằng PHÚT. Hoa/thường đều nhận, cho phép khoảng trắng hai đầu và giữa số với hậu tố
    /// (<c>" 10 s "</c>).</para>
    ///
    /// <para><b>Vì sao không dùng thẳng <c>int.Parse</c>:</b> bộ cấu hình cũ tính bằng GIÂY. Chép nguyên số
    /// sang bộ cấu hình mới thì <c>10</c> hoá thành 10 PHÚT — sai gấp 60 lần mà không có lỗi nào báo. Vì vậy
    /// hàm này luôn trả về giây để tầng trên chỉ làm việc với một đơn vị duy nhất.</para>
    ///
    /// <para><b>Fail-safe:</b> rỗng, sai định dạng, âm hoặc vượt <see cref="MaxSeconds"/> ⇒ trả <c>null</c>
    /// để bên gọi dùng giá trị mặc định và ghi log, KHÔNG ném lỗi — một dòng cấu hình sai trên DB không được
    /// phép hạ cả service. <c>0</c> là giá trị HỢP LỆ, nghĩa là TẮT.</para>
    ///
    /// <para><b>Vị trí.</b> Trước 23/09/2026 hàm này là <c>VdsConfigRules.DurationSeconds</c> trong
    /// <c>Modules.VDS.Core</c>, và đó là lý do duy nhất khiến <c>Modules.TMS</c> phải tham chiếu Core của
    /// module VDS (<c>EventDisplaySettings</c> đọc thời lượng hiển thị sự cố). Phép đổi chuỗi thành giây
    /// không có gì của VDS nên chuyển xuống đây, ngang <c>RetryHelper</c>.</para>
    /// Created date: 23/09/2026
    /// </summary>
    public class DurationHelper
    {
        /// <summary>Trần an toàn: 30 ngày. Lớn hơn mức này gần như chắc chắn là gõ sai, ví dụ thiếu hậu tố.</summary>
        public const int MaxSeconds = 30 * 24 * 60 * 60;

        /// <summary>Số nguyên không dấu, khoảng trắng tuỳ ý, rồi một hậu tố đơn vị tuỳ chọn.</summary>
        private static readonly Regex Pattern = new(@"^(\d+)\s*([smhd]?)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Đổi chuỗi cấu hình thành SỐ GIÂY; <c>null</c> nếu không đọc được.
        /// </summary>
        /// <param name="raw">Ví dụ: <c>10s</c>, <c>5m</c>, <c>2h</c>, <c>1d</c>, <c>3</c> (= 3 phút), <c>0</c> (tắt).</param>
        public static int? TryParseSeconds(string? raw)
        {
            var match = Pattern.Match(raw?.Trim() ?? "");
            if (!match.Success
                || !long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount))
            {
                return null;
            }

            var factor = match.Groups[2].Value.ToLowerInvariant() switch
            {
                "s" => 1,
                "h" => 3600,
                "d" => 86400,
                // Không hậu tố cũng rơi vào đây: đơn vị mặc định là PHÚT.
                _ => 60,
            };

            // So trước khi nhân để không tràn với đầu vào cỡ long.MaxValue.
            return amount <= MaxSeconds / factor ? (int)(amount * factor) : null;
        }

        /// <summary>Như <see cref="TryParseSeconds"/> nhưng luôn có kết quả: đọc không được thì trả mặc định.</summary>
        public static int ParseSecondsOrDefault(string? raw, int fallbackSeconds)
            => TryParseSeconds(raw) ?? fallbackSeconds;
    }
}
