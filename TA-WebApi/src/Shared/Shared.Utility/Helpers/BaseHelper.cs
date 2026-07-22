namespace Shared.Utility.Helpers
{
    /// <summary>
    /// Base class for all common helper function
    /// Author: SonTH
    /// </summary>
    public class BaseHelper
    {
        /// <summary>
        /// File Helper
        /// </summary>
        public class FileHp : FileHelper
        {

        }

        /// <summary>
        /// EChart Helper
        /// </summary>
        public class EChartHp : EChartHelper
        {
            public static List<KeyValuePair<string, DateTime>> GetListHour(DateTime startTime, DateTime endTime, string formatKey)
            {
                var hourList = new List<KeyValuePair<string, DateTime>>();

                DateTime current = startTime;
                while (current <= endTime)
                {
                    hourList.Add(new KeyValuePair<string, DateTime>(current.ToString(formatKey), current));
                    current = current.AddHours(1);
                }

                return hourList;
            }

            public static List<KeyValuePair<string, DateTime>> GetListDay(DateTime startTime, DateTime endTime, string formatKey)
            {
                var dayList = new List<KeyValuePair<string, DateTime>>();

                DateTime current = startTime;
                while (current <= endTime)
                {
                    dayList.Add(new KeyValuePair<string, DateTime>(current.ToString(formatKey), current));
                    current = current.AddDays(1);
                }

                return dayList;
            }
        }


        /// <summary>
        /// Network helper
        /// </summary>
        public class NetworkHp : NetworkHelper
        {

        }


        /// <summary>
        /// Retry Helper
        /// </summary>
        public class RetryHp : RetryHelper
        {

        }

        public class ImageHp : ImageHelper
        {

        }

        public class ListHp : ListHelper
        {

        }
       
    }
}
