using Modules.FMS.Controllers.EquipmentHistory.Dto;
using Modules.FMS.Controllers.EquipmentHistoryChart.Dto;
using Modules.FMS.Core.Exceptions;
using Shared.Utility.Helpers;

namespace Modules.FMS.Infrastructure.Services.EquipmentHistoryChart
{
    public class InvalidEquipmentHistoryChartService
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: Build incident chart
        /// Created date: 22/01/2025
        /// </summary>
        public InvalidEquipmentHistoryChartOutput BuildChart(List<EquipmentHistoryOutput>? historyList, List<EquipmentStateInfo> stateInfoList, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var chart = new InvalidEquipmentHistoryChartOutput();

                var dateList = BaseHelper.EChartHp.GetListDay(fromDate, toDate, "dd/MM/yyyy");

                var legendData = new List<string>();

                var seriesData = new List<BaseHelper.EChartHp.EChartSeries>();
                foreach (var stateInfo in stateInfoList)
                {
                    var series = new BaseHelper.EChartHp.EChartSeries();
                    series.name = stateInfo.Name;
                    series.itemStyle = new EChartHelper.EChartItemStyle() { color = stateInfo.Color };
                    series.type = "line";

                    legendData.Add(stateInfo.Name);

                    foreach (var date in dateList)
                    {
                        var count = 0;

                        //if ((historyList?.Count() ?? 0) == 0)
                        //{
                        //    series.data.Add(count.ToString());
                        //    continue;
                        //}

                        count = historyList?.Count(item => item.NewState != null
                                                          && item.StartTime != null
                                                          && stateInfo.HasState(item.NewState.ToString())
                                                          && item.StartTime.Value.Date == date.Value.Date) ?? 0;

                        series.data.Add(count.ToString());
                    }

                    seriesData.Add(series);
                }
                
                // xAxis: thời gian
                var xAxisInfo = new BaseHelper.EChartHp.EChartAxis();
                xAxisInfo.data = dateList.Select(kv => kv.Key).ToArray();
                xAxisInfo.type = "category";
                //xAxisInfo.name = "lz.label.base.day";
                xAxisInfo.boundaryGap = true;
                chart.xAxis = xAxisInfo;

                chart.legend = new BaseHelper.EChartHp.EChartLegend();
                chart.legend.top = "5%";
                chart.legend.data = legendData.ToArray();

                var titleInfo = new BaseHelper.EChartHp.EChartTitle();
                titleInfo.text = BaseMsg.FmsEquipmentHistoryChart.Label.InvalidEquipmentHistoryChart;
                titleInfo.subtext = "";
                titleInfo.top = "92%";
                titleInfo.left = "center";
                chart.title = titleInfo;

                chart.series = seriesData;

                return chart;
            }
            catch
            {
                return new InvalidEquipmentHistoryChartOutput();
            }
        }
    }
}
