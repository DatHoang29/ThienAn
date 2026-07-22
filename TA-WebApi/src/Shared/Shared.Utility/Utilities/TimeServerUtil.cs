namespace Shared.Utility.Utilities
{
    /// <summary>
    /// Time Server Utility
    /// </summary>
    public class TimeServerUtil
    {
        public static DateTime GetTimeServer()
        {
            return DateTime.Now;
        }

        /// <summary>
        /// Convert Datetime UTC +0 to local system timezone
        /// </summary>
        /// <param name="utcTime"></param>
        /// <returns></returns>
        public static DateTime? GetTimeServer(DateTime? utcTime)
        {
            if (utcTime == null)
            {
                return null;
            }

            //return datetime utc to correct local timezone
            var time = TimeZoneInfo.ConvertTimeFromUtc(utcTime.Value, TimeZoneInfo.Local);
            return time;
        }
    }
}
