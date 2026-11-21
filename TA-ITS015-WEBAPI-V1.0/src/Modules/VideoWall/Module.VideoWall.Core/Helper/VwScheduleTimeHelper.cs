using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Entities;
using System.Globalization;

namespace Module.VideoWall.Core.Helper
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đọc các trường thời gian của VwSchedule và dựng biểu thức cron cho Hangfire.
    ///              Hàm thuần (không đọc DB) — dùng chung cho validator và job service.
    /// Created date: 06/10/2026
    /// </summary>
    public static class VwScheduleTimeHelper
    {
        private static readonly string[] OnceDateFormats =
        {
            "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss",
        };

        /// <summary>
        /// "HH:mm" → giờ, phút.
        /// </summary>
        public static bool TryParseTime(string? time, out int hour, out int minute)
        {
            hour = minute = 0;
            if (string.IsNullOrWhiteSpace(time)) return false;

            var parts = time.Trim().Split(':');
            return parts.Length >= 2
                && int.TryParse(parts[0], out hour) && hour is >= 0 and <= 23
                && int.TryParse(parts[1], out minute) && minute is >= 0 and <= 59;
        }

        /// <summary>
        /// CSV "1,2,3" → [1,2,3]. 0 = Chủ nhật (khớp Date.getDay() của FE và trường day-of-week của cron).
        /// </summary>
        public static List<int> ParseWeekdays(string? csv)
            => (csv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(d => int.TryParse(d, out var v) ? v : -1)
                .Where(d => d is >= 0 and <= 6)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

        /// <summary>
        /// "yyyy-MM-dd HH:mm" → DateTime (giờ server).
        /// </summary>
        public static bool TryParseOnceDate(string? onceDate, out DateTime value)
            => DateTime.TryParseExact(onceDate?.Trim(), OnceDateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out value);

        /// <summary>
        /// Dựng biểu thức cron 5 trường cho Hangfire. Trả null khi dữ liệu không đủ để chạy,
        /// hoặc lịch "Một lần" đã qua thời điểm chạy.
        ///
        /// Lịch "Một lần": cron không có trường năm nên "mm HH dd MM *" sẽ lặp lại hằng năm —
        /// job tự xoá recurring job sau khi chạy và tự chặn khi lệch năm (xem VwScheduleJobService.RunAsync).
        /// </summary>
        public static string? BuildCron(VwSchedule schedule, DateTime now)
        {
            switch (schedule.ScheduleType)
            {
                case VwScheduleConst.ScheduleType.Daily:
                    return TryParseTime(schedule.Time, out var dh, out var dm) ? $"{dm} {dh} * * *" : null;

                case VwScheduleConst.ScheduleType.Weekly:
                    {
                        if (!TryParseTime(schedule.Time, out var wh, out var wm)) return null;
                        var days = ParseWeekdays(schedule.Weekdays);
                        return days.Count == 0 ? null : $"{wm} {wh} * * {string.Join(",", days)}";
                    }

                case VwScheduleConst.ScheduleType.Once:
                    {
                        if (!TryParseOnceDate(schedule.OnceDate, out var at) || at <= now) return null;
                        return $"{at.Minute} {at.Hour} {at.Day} {at.Month} *";
                    }

                default:
                    return null;
            }
        }

        private const int MinutesPerDay = 24 * 60;
        private const int MinutesPerWeek = 7 * MinutesPerDay;
        private static readonly List<int> AllWeekdays = new() { 0, 1, 2, 3, 4, 5, 6 };

        /// <summary>
        /// Hai lịch có chạy cách nhau không quá <paramref name="bufferMinutes"/> phút hay không (0 = trùng đúng phút).
        /// So được mọi cặp loại lịch:
        ///   - Hằng ngày / Hằng tuần ↔ Hằng ngày / Hằng tuần: so trên vòng tuần (hằng ngày = cả 7 thứ);
        ///   - Một lần ↔ lặp lại: quy thời điểm chạy về thứ + giờ:phút trong tuần rồi so như trên;
        ///   - Một lần ↔ Một lần: so trực tiếp 2 thời điểm.
        /// So trên vòng tuần nên tự xử lý qua nửa đêm (23:58 ↔ 00:01 hôm sau) và qua tuần (T7 ↔ CN).
        /// Không xét trạng thái / hành động / kịch bản — phía gọi tự lọc.
        /// </summary>
        public static bool IsOverlap(VwSchedule a, VwSchedule b, int bufferMinutes = 0)
        {
            if (!TryGetSlot(a, out var slotA) || !TryGetSlot(b, out var slotB)) return false;
            bufferMinutes = Math.Max(0, bufferMinutes);

            if (slotA.OnceAt.HasValue && slotB.OnceAt.HasValue)
                return Math.Abs((slotA.OnceAt.Value - slotB.OnceAt.Value).TotalMinutes) <= bufferMinutes;

            var minutesB = MinutesOfWeek(slotB).ToList();
            return MinutesOfWeek(slotA).Any(x => minutesB.Any(y => WeekDistance(x, y) <= bufferMinutes));
        }

        /// <summary>
        /// Giờ:phút chạy + các thứ chạy (hằng ngày = cả 7 thứ, một lần = thứ của ngày chạy) + thời điểm chạy (lịch một lần).
        /// </summary>
        private static bool TryGetSlot(VwSchedule schedule, out (int Hour, int Minute, List<int> Weekdays, DateTime? OnceAt) slot)
        {
            slot = default;
            switch (schedule.ScheduleType)
            {
                case VwScheduleConst.ScheduleType.Daily:
                    {
                        if (!TryParseTime(schedule.Time, out var h, out var m)) return false;
                        slot = (h, m, AllWeekdays, null);
                        return true;
                    }

                case VwScheduleConst.ScheduleType.Weekly:
                    {
                        if (!TryParseTime(schedule.Time, out var h, out var m)) return false;
                        var days = ParseWeekdays(schedule.Weekdays);
                        if (days.Count == 0) return false;
                        slot = (h, m, days, null);
                        return true;
                    }

                case VwScheduleConst.ScheduleType.Once:
                    {
                        if (!TryParseOnceDate(schedule.OnceDate, out var at)) return false;
                        // cron chạy theo phút ⇒ bỏ phần giây
                        var onceAt = at.Date.AddHours(at.Hour).AddMinutes(at.Minute);
                        slot = (at.Hour, at.Minute, new List<int> { (int)at.DayOfWeek }, onceAt);
                        return true;
                    }

                default:
                    return false;
            }
        }

        /// <summary>
        /// Các thời điểm chạy tính bằng phút kể từ 00:00 Chủ nhật.
        /// </summary>
        private static IEnumerable<int> MinutesOfWeek((int Hour, int Minute, List<int> Weekdays, DateTime? OnceAt) slot)
            => slot.Weekdays.Select(d => d * MinutesPerDay + slot.Hour * 60 + slot.Minute);

        /// <summary>
        /// Khoảng cách ngắn nhất (phút) giữa 2 thời điểm trên vòng tuần.
        /// </summary>
        private static int WeekDistance(int x, int y)
        {
            var d = Math.Abs(x - y) % MinutesPerWeek;
            return Math.Min(d, MinutesPerWeek - d);
        }
    }
}
