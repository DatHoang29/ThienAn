using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.Render;



namespace Modules.Mail.Core.Helper
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Tính MỐC CHẠY của lịch gửi báo cáo (khi nào gửi).
    ///              Mỗi lịch một recurring job Hangfire — cron của job sinh từ ToCron; job tới giờ vẫn
    ///              hỏi lại GetLastDueTime / LastRunKey nên cron chỉ cần bắn ĐỦ (bắn thừa thì job tự bỏ qua).
    /// Created date: 17/08/2026
    /// </summary>
    public static class RptScheduleTime
    {
        /// <summary>Số ngày dò tối đa khi tìm mốc — lớn hơn một năm nhuận để lịch năm luôn tìm được.</summary>
        private const int MaxScanDays = 400;

        /// <summary>Tên thứ theo ISO, dùng cho Describe.</summary>
        private static readonly string[] WeekdayNames =
            { "", "thứ Hai", "thứ Ba", "thứ Tư", "thứ Năm", "thứ Sáu", "thứ Bảy", "Chủ nhật" };

        private static readonly string[] WeekdayShort = { "", "T2", "T3", "T4", "T5", "T6", "T7", "CN" };

        /// <summary>
        /// Author: HieuNV
        /// Description: Mốc đến hạn gần nhất TẠI HOẶC TRƯỚC thời điểm now.
        ///              Trả null nếu tần suất không hỗ trợ.
        /// Created date: 17/08/2026
        /// </summary>
        public static DateTime? GetLastDueTime(MailRptScheduleRule rule, DateTime now)
        {
            if (!IsSupported(rule.Frequency)) return null;

            for (var i = 0; i <= MaxScanDays; i++)
            {
                var date = now.Date.AddDays(-i);
                if (!OccursOn(rule, date)) continue;

                var candidate = date.AddHours(rule.Hour).AddMinutes(rule.Minute);
                if (candidate <= now) return candidate;
            }

            return null;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Mốc đến hạn kế tiếp SAU thời điểm now. Chỉ dùng để hiển thị cột
        ///              "Lần chạy kế tiếp" và tính kỳ cho bản xem trước; job không đọc giá trị này.
        /// Created date: 17/08/2026
        /// </summary>
        public static DateTime? GetNextDueTime(MailRptScheduleRule rule, DateTime now)
        {
            if (!IsSupported(rule.Frequency)) return null;

            for (var i = 0; i <= MaxScanDays; i++)
            {
                var date = now.Date.AddDays(i);
                if (!OccursOn(rule, date)) continue;

                var candidate = date.AddHours(rule.Hour).AddMinutes(rule.Minute);
                if (candidate > now) return candidate;
            }

            return null;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ngày date có phải ngày chạy của lịch không (chưa xét giờ).
        ///
        ///              Đây là chỗ DUY NHẤT định nghĩa luật của từng tần suất:
        ///                - day    : mọi ngày, hoặc chỉ các thứ trong DaysOfWeek
        ///                - week   : đúng thứ DaysOfWeek[0] (mặc định thứ Hai)
        ///                - month  : ngày DayOfMonth, dồn về ngày cuối nếu tháng ngắn hơn
        ///                - quarter: như month, trong tháng thứ MonthInPeriod của quý
        ///                - year   : như month, trong tháng MonthInPeriod
        /// Created date: 30/09/2026
        /// </summary>
        public static bool OccursOn(MailRptScheduleRule rule, DateTime date)
        {
            switch (rule.Frequency)
            {
                // Rỗng chỉ còn ở lịch tạo trước 01/10/2026 (khi đó rỗng = mọi ngày) — giữ nguyên để lịch cũ
                // không tự dưng ngừng gửi. Lịch lưu mới bắt buộc có ít nhất một thứ (NormalizeRunRule).
                case MailRptPeriodType.Day:
                    return rule.DaysOfWeek.Count == 0 || rule.DaysOfWeek.Contains(IsoDayOfWeek(date));

                case MailRptPeriodType.Week:
                    return IsoDayOfWeek(date) == (rule.DaysOfWeek.Count > 0 ? rule.DaysOfWeek[0] : 1);

                case MailRptPeriodType.Month:
                    return IsRunDayOfMonth(rule, date);

                case MailRptPeriodType.Quarter:
                    return date.Month == FirstMonthOfQuarter(date.Month) + rule.MonthInPeriod - 1
                        && IsRunDayOfMonth(rule, date);

                case MailRptPeriodType.Year:
                    return date.Month == rule.MonthInPeriod && IsRunDayOfMonth(rule, date);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Biểu thức cron (5 trường, giờ địa phương) cho recurring job của lịch. Null nếu tần suất lạ.
        ///
        ///              Thứ trong cron: 0 = Chủ nhật, 1 = thứ Hai … 6 = thứ Bảy (ISO 7 → 0).
        ///              Ngày trong tháng — cron không diễn đạt được "ngày D, dồn về cuối tháng nếu tháng ngắn hơn":
        ///                - D ≤ 28 : đúng ngày D
        ///                - D = 31 : L (ngày cuối tháng)
        ///                - D = 29/30: 28-31 — bắn 4 ngày cuối tháng, job lọc lại bằng OccursOn (IsRunDayOfMonth)
        ///              Bắn thừa không sao: RptEmailSendService.RunScheduleAsync chỉ gửi khi đúng mốc đến hạn chưa gửi.
        /// Created date: 02/10/2026
        /// </summary>
        public static string? ToCron(MailRptScheduleRule rule)
        {
            var time = $"{rule.Minute} {rule.Hour}";
            var day = rule.DayOfMonth <= 28 ? rule.DayOfMonth.ToString()
                : rule.DayOfMonth >= MailConst.MaxDayOfMonth ? "L"
                : "28-31";

            static int CronDow(int isoDay) => isoDay % 7;

            switch (rule.Frequency)
            {
                case MailRptPeriodType.Day:
                    return rule.DaysOfWeek.Count is 0 or 7
                        ? $"{time} * * *"
                        : $"{time} * * {string.Join(",", rule.DaysOfWeek.Select(CronDow).OrderBy(x => x))}";

                case MailRptPeriodType.Week:
                    return $"{time} * * {CronDow(rule.DaysOfWeek.Count > 0 ? rule.DaysOfWeek[0] : 1)}";

                case MailRptPeriodType.Month:
                    return $"{time} {day} * *";

                case MailRptPeriodType.Quarter:
                    {
                        var months = Enumerable.Range(0, 4).Select(q => rule.MonthInPeriod + q * MailConst.MonthsPerQuarter);
                        return $"{time} {day} {string.Join(",", months)} *";
                    }

                case MailRptPeriodType.Year:
                    return $"{time} {day} {rule.MonthInPeriod} *";

                default:
                    return null;
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Khoá chống gửi trùng của một mốc đến hạn.
        ///
        ///              Chỉ tới phút, không tới giây: mốc đến hạn luôn tròn phút nên thêm giây
        ///              chỉ làm khoá dài ra vô ích.
        /// Created date: 17/08/2026
        /// </summary>
        public static string? GetRunKey(DateTime? dueTime)
            => dueTime?.ToString("yyyyMMddHHmm");

        /// <summary>
        /// Author: HieuNV
        /// Description: Diễn giải lịch chạy thành câu tiếng Việt cho lưới và tooltip.
        /// Created date: 14/08/2026
        /// </summary>
        public static string Describe(MailRptScheduleRule rule)
        {
            var time = $"{rule.Hour:00}:{rule.Minute:00}";

            // Khai 31 nghĩa là ngày cuối tháng, phải nói rõ ra chứ không ghi "ngày 31"
            var day = rule.DayOfMonth >= MailConst.MaxDayOfMonth ? "ngày cuối" : $"ngày {rule.DayOfMonth:00}";

            switch (rule.Frequency)
            {
                case MailRptPeriodType.Day:
                    return rule.DaysOfWeek.Count is 0 or 7
                        ? $"{time} hàng ngày"
                        : $"{time} hàng ngày ({string.Join(", ", rule.DaysOfWeek.Select(d => WeekdayShort[d]))})";

                case MailRptPeriodType.Week:
                    return $"{time} {WeekdayNames[rule.DaysOfWeek.Count > 0 ? rule.DaysOfWeek[0] : 1]} hàng tuần";

                case MailRptPeriodType.Month:
                    return $"{time} {day} hàng tháng";

                case MailRptPeriodType.Quarter:
                    {
                        var month = rule.MonthInPeriod switch
                        {
                            1 => "tháng đầu",
                            MailConst.MonthsPerQuarter => "tháng cuối",
                            _ => $"tháng thứ {rule.MonthInPeriod}",
                        };
                        return $"{time} {day} {month} mỗi quý";
                    }

                case MailRptPeriodType.Year:
                    return $"{time} {day} tháng {rule.MonthInPeriod:00} hàng năm";

                default:
                    return time;
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đọc chuỗi thứ "1,2,5" thành danh sách thứ ISO đã sắp xếp, bỏ trùng, bỏ giá trị lạ.
        /// Created date: 30/09/2026
        /// </summary>
        public static IReadOnlyList<int> ParseDaysOfWeek(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Array.Empty<int>();

            return value
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var n) ? n : 0)
                .Where(n => n >= 1 && n <= 7)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kiểm chuỗi thứ có hợp lệ không (mọi phần tử là số 1-7). Chuỗi rỗng coi là hợp lệ.
        /// Created date: 30/09/2026
        /// </summary>
        public static bool IsValidDaysOfWeek(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;

            return value
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .All(x => int.TryParse(x, out var n) && n >= 1 && n <= 7);
        }

        /// <summary>Ghi danh sách thứ thành chuỗi lưu DB. Rỗng trả null.</summary>
        public static string? FormatDaysOfWeek(IEnumerable<int> days)
        {
            var list = days.Where(n => n >= 1 && n <= 7).Distinct().OrderBy(n => n).ToList();
            return list.Count == 0 ? null : string.Join(",", list);
        }

        /// <summary>Thứ theo chuẩn ISO: 1 = thứ Hai … 7 = Chủ nhật (.NET để Chủ nhật = 0).</summary>
        public static int IsoDayOfWeek(DateTime date)
            => date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;

        private static bool IsSupported(string? frequency)
            => frequency != null && MailRptPeriodType.All.Contains(frequency);

        /// <summary>
        /// Author: HieuNV
        /// Description: Ngày date có trùng ngày chạy trong tháng không, DỒN ngày về ngày cuối tháng nếu
        ///              tháng đó ngắn hơn. Đây là chỗ duy nhất xử lý chuyện 29-31.
        /// Created date: 17/08/2026
        /// </summary>
        private static bool IsRunDayOfMonth(MailRptScheduleRule rule, DateTime date)
            => date.Day == Math.Min(rule.DayOfMonth, DateTime.DaysInMonth(date.Year, date.Month));

        /// <summary>
        /// Author: HieuNV
        /// Description: Tháng đầu của quý chứa tháng đã cho: 1, 4, 7 hoặc 10.
        /// Created date: 17/08/2026
        /// </summary>
        internal static int FirstMonthOfQuarter(int month) => (month - 1) / 3 * 3 + 1;
    }
}
