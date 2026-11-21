namespace Modules.Mail.Core.Constants
{
    /// <summary>
    /// Description: Kỳ dữ liệu của báo cáo
    /// Created date: 2026-08-14
    /// </summary>
    public class MailRptPeriodType
    {
        public const string Year = "year";

        public const string Quarter = "quarter";

        public const string Month = "month";
        public const string Week = "week";

        public const string Day = "day";

        public static readonly string[] All = { Year, Quarter, Month, Week, Day };

        public static readonly Dictionary<string, int> Rank = new()
        {
            [Day] = 1,
            [Week] = 2,
            [Month] = 3,
            [Quarter] = 4,
            [Year] = 5,
        };
    }
}
