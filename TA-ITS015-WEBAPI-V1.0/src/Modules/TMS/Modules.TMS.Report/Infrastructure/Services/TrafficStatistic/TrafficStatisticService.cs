namespace Modules.TMS.Report.Infrastructure.Services.TrafficStatistic
{
    public class TrafficStatisticService
    {
        /// <summary>
        /// Author: ThangNN
        /// Created date: 15/02/2025
        /// </summary>
        public static List<(DateTime FromTime, DateTime ToTime, DateTime ValueTime)> GetDateList(DateTime fromDate, DateTime toDate)
        {
            var timeList = new List<(DateTime FromTime, DateTime ToTime, DateTime ValueTime)>();

            DateTime current = fromDate;
            while (current <= toDate)
            {
                // Ngày đầu tiên giữ nguyên thời điểm bắt đầu truyền vào
                if (current == fromDate)
                    timeList.Add((fromDate, fromDate.Date.AddDays(1).AddMicroseconds(-1), fromDate));
                else if(current == toDate) // Ngày cuối cùng giữ nguyên thời điểm kết thúc truyền vào
                    timeList.Add((toDate.Date, toDate, toDate));
                else // Các ngày còn lại đủ 24 giờ sẽ lấy từ đầu ngày đến cuối ngày
                    timeList.Add((current.Date, current.Date.AddDays(1).AddMicroseconds(-1), current));

                current = current.AddDays(1);
            }

            return timeList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 15/02/2025
        /// </summary>
        public static List<(DateTime FromTime, DateTime ToTime, DateTime ValueTime)> GetTimeList(DateTime fromDate, DateTime toDate, int intervalMinutes)
        {
            var timeList = new List<(DateTime FromTime, DateTime ToTime, DateTime ValueTime)>();

            if (intervalMinutes <= 0)
                intervalMinutes = 5;

            var current = new DateTime(
                fromDate.Year,
                fromDate.Month,
                fromDate.Day,
                fromDate.Hour, 0, 0);

            var additionalMinutes = (fromDate.Minute / intervalMinutes) * intervalMinutes;
            current = current.AddMinutes(additionalMinutes);

            while (current < toDate)
            {
                var next = current.AddMinutes(intervalMinutes);

                timeList.Add((current, next, current));

                current = next;
            }

            return timeList;
        }
    }
}
