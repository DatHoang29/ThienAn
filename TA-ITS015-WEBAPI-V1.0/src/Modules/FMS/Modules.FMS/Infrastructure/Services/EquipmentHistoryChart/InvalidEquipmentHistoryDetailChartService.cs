using Modules.FMS.Controllers.EquipmentHistory.Dto;
using Modules.FMS.Controllers.EquipmentHistoryChart.Dto;
using Modules.FMS.Core.Exceptions;
using Shared.DTO.Dtos.Tms;
using Shared.Utility.Helpers;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.FMS.Infrastructure.Services.EquipmentHistoryChart
{
    public class InvalidEquipmentHistoryDetailChartService
    {
        /// <summary>
        /// Author: ThangNN
        /// Create date: 22/01/2025
        /// Description: Build incident chart
        /// </summary>
        public InvalidEquipmentHistoryDetailChartOutput BuildChart(List<TmsEquipmentResponse>? equipmentList, List<EquipmentHistoryOutput>? historyList, List<EquipmentStateInfo> stateInfoList, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var chart = new InvalidEquipmentHistoryDetailChartOutput();

                var equipmentInfoList = this.BuildEquipmentDataInfoList(equipmentList, historyList, stateInfoList);

                var seriesData = new List<BaseHelper.EChartHp.EChartSeries>();
                if (equipmentInfoList?.Count() > 0)
                {
                    foreach (var stateInfo in stateInfoList)
                    {
                        var series = new BaseHelper.EChartHp.EChartSeries();
                        series.name = stateInfo.Name;
                        series.type = "bar";
                        series.stack = "total";
                        series.barWidth = 30;
                        series.itemStyle = new EChartHelper.EChartItemStyle() { color = stateInfo.Color };
                        series.label = new EChartHelper.EChartSeriesLabel() { show = true };
                        
                        foreach (var equip in equipmentInfoList)
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
                                                              && item.EquipmentId == equip.ID) ?? 0;

                            series.data.Add(count.ToString());
                        }

                        seriesData.Add(series);
                    }
                }

                
               
                var titleInfo = new BaseHelper.EChartHp.EChartTitle();
                titleInfo.text = BaseMsg.FmsEquipmentHistoryChart.Label.InvalidEquipmentHistoryChart;
                titleInfo.subtext = $"{fromDate.ToString(DateTimeFormatEnum.DateDefault)} - {toDate.ToString(DateTimeFormatEnum.DateDefault)}";
                titleInfo.top = "85%";
                titleInfo.left = "center";
                chart.title = titleInfo;

                // xAxis: cột x => số lượng lỗi
                var xAxisInfo = new BaseHelper.EChartHp.EChartAxis();
                xAxisInfo.type = "value";
                //xAxisInfo.name = "lz.label.base.count";
                xAxisInfo.boundaryGap = true;
                chart.xAxis = xAxisInfo;

                // yAxis: cột y => tên thiết bị
                var yAxisInfo = new BaseHelper.EChartHp.EChartAxis();
                yAxisInfo.type = "category";
                //yAxisInfo.name = "lz.label.base.name";
                yAxisInfo.boundaryGap = true;
                yAxisInfo.data = equipmentInfoList?.Select(equip => equip.Name).ToArray() ?? new string[0];
                chart.yAxis = yAxisInfo;

                chart.series = seriesData;

                return chart;
            }
            catch
            {
                return new InvalidEquipmentHistoryDetailChartOutput();
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Create date: 22/01/2025
        /// </summary>
        private List<EquipmentDataInfo> BuildEquipmentDataInfoList(List<TmsEquipmentResponse>? equipmentList, List<EquipmentHistoryOutput>? historyList, List<EquipmentStateInfo> stateInfoList)
        {
            var dataList = new List<EquipmentDataInfo>();

            var stateList = new List<string>();
            foreach(var stateInfo in stateInfoList)
            {
                if(stateInfo.StateList?.Count() > 0)
                    stateList.AddRange(stateInfo.StateList);
            }

            equipmentList?.ForEach(equip =>
            {
                var count = historyList?.Where(item =>
                        item.NewState != null
                        && item.StartTime != null
                        && stateList.Contains(item.NewState)
                        && item.EquipmentId == equip.ID
                    ).Count() ?? 0;

                if (count > 0)
                {
                    dataList.Add(new EquipmentDataInfo()
                    {
                        ID = equip.ID,
                        Name = equip.Name,
                        Code = equip.Code,
                        EquipmentState = equip.EquipmentState,
                        EquipmentTypeId = equip.EquipmentTypeId,
                        InvalidStateCount = count
                    });
                }
            });

            dataList = dataList.OrderBy(item => item.InvalidStateCount).ToList();

            return dataList;
        }
    }
}
