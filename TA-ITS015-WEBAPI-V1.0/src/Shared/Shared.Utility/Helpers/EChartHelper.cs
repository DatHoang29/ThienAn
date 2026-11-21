using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Utility.Helpers
{
    public class EChartHelper
    {

        /// <summary>
        /// Dữ liệu EChart trả về dùng chung
        /// </summary>
        public class EChartResponse
        {
            public List<EChartSeries> series { get; set; }
            public EChartAxis xAxis { get; set; }
            public EChartAxis yAxis { get; set; }
            public EChartLegend legend { get; set; }
            public EChartTitle title { get; set; }

        }


        #region TreeMap
        /// <summary>
        /// Dữ liệu EChart TreeMap trả về
        /// </summary>
        public class EChartTreeMap
        {
            public string value { get; set; }
            public string name { get; set; }
            public string path { get; set; }
            public List<EChartTreeMap> children { get; set; } = new List<EChartTreeMap>();
        }
        #endregion



        #region Component

        /// <summary>
        /// Danh sách dữ liệu
        /// </summary>
        public class EChartSeries
        {
            public string name { get; set; }
            public string type { get; set; }
            public string stack { get; set; }
            public List<string> data { get; set; } = new List<string>();
            public bool smooth { get; set; } = true;
            public int barWidth { get; set; } = 20;
            public EChartSeriesLabel label { get; set; }

            public EChartItemStyle itemStyle { get; set; }
        }

        /// <summary>
        /// Trục toạ độ
        /// </summary>
        public class  EChartAxis
        {
            public string type { get; set; }
            public string[] data { get; set; }
            public bool boundaryGap { get; set; } = false;
            
            public string name { get; set; }
        }

        /// <summary>
        /// Chú giải / Tên các series dữ liệu
        /// </summary>
        public class  EChartLegend
        {
            public string[] data { get; set; }
            public string top { get; set; } = "0%";
        }

        /// <summary>
        /// Nhãn series dữ liệu trong chart
        /// </summary>
        public class EChartSeriesLabel
        {
            public bool show { get; set; } = false;
        }

        /// <summary>
        /// Tiêu đề chart
        /// </summary>
        public class EChartTitle
        {
            public string text { get; set; }
            public string subtext { get; set; }
            public string left { get; set; } = "center";
            public string top { get; set; } = "90%";
            public EChartTextStyle textStyle { get; set; } = new EChartTextStyle(){color = "black",fontFamily = "Arial"};
        }


        /// <summary>
        /// Kiểu chữ
        /// </summary>
        public class EChartTextStyle
        {
            public string color { get; set; }
            public string fontFamily { get; set; }

        }

        public class EChartItemStyle
        {
            public string color { get; set; }
        }

        #endregion

        /// <summary>
        /// Hàm lấy danh sách ngày từ ngày đến ngày
        /// Dữ liệu: yyyy-MM-dd (key)
        /// Hiển thị: dd/MM/yyyy (value)
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, string>> GetListDay(DateTime from, DateTime to)
        {
            var list = Enumerable.Range(0, 1 + to.Subtract(from).Days)
                .Select(offset => new KeyValuePair<string, string>(from.AddDays(offset).ToString("yyyy-MM-dd"),
                    from.AddDays(offset).ToString("dd/MM/yyyy")))
                .ToList();
            return list;
        }

    }
}
