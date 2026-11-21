using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Exceptions;
using Shared.Utility.Helpers;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident
{
    public class IncidentChartService : IScoped
    {
        private readonly IStringLocalizer _localizer;

        public IncidentChartService(IStringLocalizer localizer)
        {
            this._localizer = localizer;
        }

        /// <summary>
        /// Một bản ghi đã CHUẨN HOÁ cho biểu đồ: tên loại sự kiện chắc chắn không null.
        ///
        /// <para>Dùng kiểu riêng thay vì vá tại chỗ <c>legend.data</c> vì tên loại sự kiện xuất hiện ở HAI
        /// nơi phải khớp nhau: <c>legend.data</c>/<c>series.name</c> và điều kiện đếm
        /// <c>EventTypeName == eventType</c>. Vá một chỗ thì đường biểu đồ lệch khỏi legend.</para>
        /// </summary>
        private sealed record ChartRow(DateTime? StartDate, string EventTypeName);

        /// <summary>
        /// Author: ThangNN
        /// Description: Build incident chart
        /// Created date: 15/02/2025
        /// </summary>
        public IncidentChartOutput BuildIncidentChart(List<PageTmsIncidentOutput>? incidentList, DateTime fromDate, DateTime toDate, string chartDataType)
        {
            try
            {
                var chart = new IncidentChartOutput();

                // Lấy danh sách thời gian để lọc incident
                var dateList = chartDataType == "day"
                    ? BaseHelper.EChartHp.GetListHour(fromDate, toDate, "HH")
                    : BaseHelper.EChartHp.GetListDay(fromDate, toDate, "dd/MM/yyyy");

                // Lấy danh sách giá trị cột x cho chart
                var xAxisData = dateList.Select(kv => kv.Key).ToArray();

                // Bản ghi thiếu loại sự kiện được gộp vào MỘT nhóm có tên hiển thị, KHÔNG để null lọt ra.
                //
                // Vì sao bắt buộc: EventTypeName đến từ LEFT JOIN TmsEventType trong IncidentQueryHandler nên
                // null khi sự cố có EventTypeId null hoặc trỏ tới loại đã bị xoá. Một phần tử null trong
                // legend.data làm ECharts 6 ném TypeError ngay trong LegendModel._updateData (null không phải
                // string/number nên không được bọc thành { name }, rồi dataItem.name nổ) — MẤT TRẮNG biểu đồ,
                // không chỉ mất một đường. Nhánh chartDataType = "day" ít gặp hơn vì nó tự ghi đè cửa sổ về
                // đúng hôm nay; nhánh "time" dùng filter của FE (mặc định 90 ngày) nên vớ được dữ liệu xấu.
                var unknownEventType = this._localizer[BaseMsg.TmsIncident.Label.UnknownEventType].Value;
                var rows = (incidentList ?? new List<PageTmsIncidentOutput>())
                    .Select(item => new ChartRow(
                        item.StartDate,
                        string.IsNullOrWhiteSpace(item.EventTypeName) ? unknownEventType : item.EventTypeName))
                    .ToList();

                // Danh sách eventype: fire, traffic-jam, accident...
                var legendInfo = rows.Select(row => row.EventTypeName).Distinct().ToArray();

                // Legend: loại sự kiện sự cố (đường biểu đồ)
                chart.legend = new BaseHelper.EChartHp.EChartLegend();
                chart.legend.top = "5%";
                chart.legend.data = legendInfo;

                // xAxis: cột thời gian (cột x)
                var xAxisInfo = new BaseHelper.EChartHp.EChartAxis();
                xAxisInfo.data = xAxisData; // Danh sách giờ: 0, 1, 2, 3... hoặc danh sách ngày 01/02/2025, 02/02/2025, 03/02/2025...
                xAxisInfo.type = "category";
                xAxisInfo.name = chartDataType == "day"
                    ? "lz.label.base.hour"
                    : "lz.label.base.day";
                xAxisInfo.boundaryGap = true;
                chart.xAxis = xAxisInfo;

                //Series: dữ liệu biểu đồ
                var seriesData = new List<BaseHelper.EChartHp.EChartSeries>();
                foreach (var eventType in legendInfo)
                {
                    var series = new BaseHelper.EChartHp.EChartSeries();
                    series.name = eventType;
                    series.type = "line";

                    // Count incident theo eventType và thời gian
                    foreach (var date in dateList)
                    {
                        if (chartDataType == "day")
                        {
                            var count = rows.Count(row => row.StartDate != null && row.StartDate.Value.Date == date.Value.Date && row.StartDate.Value.Hour == date.Value.Hour && row.EventTypeName == eventType);

                            series.data.Add(count.ToString());
                        }
                        else
                        {
                            var count = rows.Count(row => row.StartDate != null && row.StartDate.Value.Date == date.Value.Date && row.EventTypeName == eventType);

                            series.data.Add(count.ToString());
                        }
                    }

                    seriesData.Add(series);
                }
                chart.series = seriesData;

                var title = new BaseHelper.EChartHp.EChartTitle();
                title.text = chartDataType == "day"
                    ? "lz.label.tmsIncident.incidentDayChartTitle"
                    : "lz.label.tmsIncident.incidentChartTitle";

                title.subtext = chartDataType == "day"
                    ? toDate.ToString(DateTimeFormatEnum.DateDefault)
                    : $"{fromDate.ToString(DateTimeFormatEnum.DateDefault)} - {toDate.ToString(DateTimeFormatEnum.DateDefault)}";

                title.top = "0%";
                title.left = "left";
                chart.title = title;

                return chart;
            }
            catch
            {
                return new IncidentChartOutput(); // Empty report
            }
        }
    }
}
