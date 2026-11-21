using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Entities;
using Modules.Mail.Core.Helper;



namespace Modules.Mail.Core.Dto.Render
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Quy tắc lịch chạy đã chuẩn hoá
    /// Created date: 30/09/2026
    /// </summary>
    public sealed class MailRptScheduleRule
    {
        public string? Frequency { get; init; }
        public int DayOfMonth { get; init; } = 1;
        public int MonthInPeriod { get; init; } = 1;
        public IReadOnlyList<int> DaysOfWeek { get; init; } = Array.Empty<int>();
        public int Hour { get; init; }
        public int Minute { get; init; }

        public static MailRptScheduleRule Of(
            string? frequency, int? dayOfMonth, int? monthInPeriod, string? daysOfWeek, int hour, int minute)
        {
            var freq = frequency?.Trim().ToLowerInvariant();
            var maxMonth = freq == MailRptPeriodType.Quarter ? MailConst.MonthsPerQuarter : MailConst.MonthsPerYear;

            return new MailRptScheduleRule
            {
                Frequency = freq,
                DayOfMonth = Math.Clamp(dayOfMonth ?? 1, 1, MailConst.MaxDayOfMonth),
                MonthInPeriod = Math.Clamp(monthInPeriod ?? 1, 1, maxMonth),
                DaysOfWeek = RptScheduleTime.ParseDaysOfWeek(daysOfWeek),
                Hour = Math.Clamp(hour, 0, 23),
                Minute = Math.Clamp(minute, 0, 59),
            };
        }

        public static MailRptScheduleRule From(MailSchedule s)
            => Of(s.Frequency, s.DayOfMonth, s.MonthInPeriod, s.DaysOfWeek, s.HourOfDay, s.MinuteOfHour);
    }
}
