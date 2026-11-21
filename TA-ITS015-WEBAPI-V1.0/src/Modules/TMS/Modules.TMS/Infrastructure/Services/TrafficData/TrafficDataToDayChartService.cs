using Modules.TMS.Controllers.TrafficData.Dto;
using Shared.DTO.Dtos.System;
using Shared.Utility.Helpers;

namespace Modules.TMS.Infrastructure.Services.TrafficData
{
    public class TrafficDataToDayChartService
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: Build 
        /// Created date: 12/11/2024
        /// </summary>
        public TmsTrafficDataToDayChartOutput BuildChart(List<TrafficDataOutput> dataList, List<SysConfigDataResponse> vehicleTypeList, List<int> hourList)
        {
            try
            {
                var chart = new TmsTrafficDataToDayChartOutput();

                var legendData = new List<string>();

                var seriesData = new List<BaseHelper.EChartHp.EChartSeries>();
                vehicleTypeList?.ForEach(vehicleType =>
                {
                    var series = new BaseHelper.EChartHp.EChartSeries();
                    series.name = vehicleType.Name;
                    series.itemStyle = new EChartHelper.EChartItemStyle() { };
                    series.type = "line";

                    legendData.Add(vehicleType.Name);

                    foreach (var hour in hourList)
                    {
                        var count = dataList?
                            .Where(td => td.DetectTime != null && td.DetectTime.Value.Hour == hour)
                            .Where(td => td.Type != null && td.Type.Equals(vehicleType.Code, StringComparison.OrdinalIgnoreCase))
                            .Count() ?? 0;

                        series.data.Add(count.ToString());
                    }

                    seriesData.Add(series);
                });

                // xAxis: thời gian
                var xAxisInfo = new BaseHelper.EChartHp.EChartAxis();
                xAxisInfo.data = hourList.Select(hour => hour.ToString()).ToArray();
                xAxisInfo.type = "category";
                //xAxisInfo.name = "lz.label.base.day";
                xAxisInfo.boundaryGap = true;
                chart.xAxis = xAxisInfo;

                chart.legend = new BaseHelper.EChartHp.EChartLegend();
                chart.legend.top = "5%";
                chart.legend.data = legendData.ToArray();

                var titleInfo = new BaseHelper.EChartHp.EChartTitle();
                titleInfo.text = "lz.label.tmsTrafficData.trafficVolumeChartTitle";
                titleInfo.subtext = "";
                titleInfo.top = "92%";
                titleInfo.left = "center";
                chart.title = titleInfo;

                chart.series = seriesData;

                return chart;
            }
            catch
            {
                return new TmsTrafficDataToDayChartOutput();
            }
        }
    }
}
