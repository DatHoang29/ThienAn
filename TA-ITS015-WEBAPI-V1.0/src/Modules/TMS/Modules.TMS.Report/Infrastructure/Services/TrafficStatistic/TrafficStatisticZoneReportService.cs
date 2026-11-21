using Microsoft.Extensions.Localization;
using Modules.TMS.Report.Controllers.TrafficStatistic.Dto;
using Modules.TMS.Report.Core.Exceptions;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Dtos.Vds;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using Shared.Utility.Apis.Tms;
using System.Text.RegularExpressions;
using Shared.Utility.Utilities;
using static Shared.DTO.Enums.BaseEnums;
using Modules.TMS.Report.Core.Helpers;
using Wolverine;
using Modules.TMS.Extensions;
using TmsMsg = Modules.TMS.Core.Exceptions.BaseMsg;

namespace Modules.TMS.Report.Infrastructure.Services.TrafficStatistic
{
    public class TrafficStatisticZoneReportService
    {
        private IStringLocalizer _localizer;
        private IMessageBus _messBus;

        /// <summary>
        /// Author: ThangNN
        /// Description: 13/10/2026 (KienLT) — thêm <c>IMessageBus</c> để gọi thống kê VDS in-process thay Refit.
        /// Created date: 15/02/2025
        /// </summary>
        public TrafficStatisticZoneReportService(IStringLocalizer localizer, IMessageBus messBus)
        {
            this._localizer = localizer;
            this._messBus = messBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Truy xuất và xử lý dữ liệu báo cáo TrafficStatistic
        /// Created date: 15/02/2025
        /// </summary>
        public async Task<TrafficStatisticReport> BuildTrafficStatisticReport(TrafficStatisticZoneRenderInput command)
        {
            var reportData = new TrafficStatisticReport() { };

            var cycle = command.Cycle != null ? command.Cycle.ToLower() : "minute";
            command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

            var fromDate = command.FromDate ?? DateTime.Now.Date;
            var toDate = command.ToDate ?? DateTime.Now;

            this.CheckValidInput(cycle, fromDate, toDate);

            var zoneQuery = new TmsZoneListRequest() {};
            var zoneResponse = await BaseApiClient<IZoneApi>.Client(null, null, TimeSpan.FromSeconds(30)).List(zoneQuery);
            if (!zoneResponse.IsSuccessStatusCode || zoneResponse.Content?.Result == null)
                throw new InvalidOperationException("Không đọc được danh sách zone để lập báo cáo.");
            var zoneList = zoneResponse.Content.Result.Where(item => item.Type == "zone").ToList();
            if((zoneList?.Count() ?? 0) == 0)
                return reportData;

            var tsQuery = new VdsTrafficStatisticRequest() { FromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Local).ToUniversalTime(), ToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Local).ToUniversalTime()};
            // 13/10/2026: gọi IN-PROCESS qua Wolverine (handler VDS TrafficStatisticQueryHandler) thay Refit
            // IVdsTrafficStatisticApi. Giới hạn 30s, lỗi gói thành thông báo chung (InvokeVdsAsync) — không thành báo cáo rỗng.
            var tsList = await this._messBus.InvokeVdsAsync<List<VdsTrafficStatisticResponse>>(
                tsQuery, TimeSpan.FromSeconds(30), TmsMsg.VdsQuery.Exception.ReportStatisticUnavailable);
            foreach (var row in tsList)
            {
                row.FromTime = BaseUtil.TimeServer.ToServerTime(row.FromTime);
                if (row.ToTime.HasValue)
                    row.ToTime = BaseUtil.TimeServer.ToServerTime(row.ToTime.Value);
            }

            var timeList = cycle == "day"
                ? TrafficStatisticService.GetDateList(fromDate, toDate)
                : TrafficStatisticService.GetTimeList(fromDate, toDate, (cycle == "hour" ? 60 : 30));

            var zoneIdList = command.ZoneIdList?.Length > 0
                ? command.ZoneIdList
                : zoneList.Select(item => item.ID).ToArray();

            var equipmentList = command.EquipmentIdList?.Count() > 0
                ? await this.GetEquipmentList(zoneIdList, command.EquipmentIdList, command.ZoneType, command.EquipmentType)
                : null;

            equipmentList?.ForEach((equipment) =>
            {
                var itemList = new List<TrafficStatisticItemOutput>();

                foreach (var timeRange in timeList)
                {
                    itemList.Add(new TrafficStatisticItemOutput()
                    {
                        AvgSpeed = this.CalculateAvgTotal(tsList, equipment, "speed", timeRange.FromTime, timeRange.ToTime),
                        SpaceOccupy = this.CalculateAvgTotal(tsList, equipment, "occupy", timeRange.FromTime, timeRange.ToTime),
                        TotalVehicle = (int)this.CalculateAvgTotal(tsList, equipment, "total", timeRange.FromTime, timeRange.ToTime),
                        Time = command.Cycle == "day" ? timeRange.FromTime.ToString(DateTimeFormatEnum.DateDefault) : timeRange.ValueTime.ToString(DateTimeFormatEnum.DateTimeDefaultRevert)
                    });
                }

                reportData.DataList.Add(new TrafficStatisticReportOutput()
                {
                    Zone = equipment.ZoneName,
                    Camera = equipment.Name,
                    AvgSpeed = this.CalculateAvgTotal(tsList, equipment, "speed"),
                    TotalVehicle = (int)this.CalculateAvgTotal(tsList, equipment, "total"),
                    Items = itemList
                });
            });

            if (reportData.DataList?.Count() > 0)
            {
                var dataList = reportData.DataList.OrderBy(item => item.Zone).ToList();
                reportData.DataList = dataList;
            }

            this.LocalizeReportColumns(reportData, command);

            this.SetReportParameters(reportData, command);

            return reportData;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 15/02/2025
        /// </summary>
        public async Task<List<TmsEquipmentResponse>> GetEquipmentList(string[]? zoneIdList, string[]? equipmentIdList, string? zoneType, string? equipmentType)
        {
            var request = new TmsEquipmentZoneListRequest()
            {
                ZoneIdList = zoneIdList,
                EquipmentIdList = equipmentIdList,
                ZoneType = zoneType,
                EquipmentType = equipmentType
            };
            // Báo cáo toàn tuyến có thể cần ghép nhiều zone; timeout mặc định của
            // BaseApiClient không đủ cho truy vấn này khi chọn tất cả camera.
            var response = await BaseApiClient<IEquipmentApi>
                .Client(null, null, TimeSpan.FromSeconds(30))
                .ListByEquipmentZoneList(request);
            if (!response.IsSuccessStatusCode || response.Content?.Result == null)
                throw new InvalidOperationException("Không đọc được danh sách camera theo zone để lập báo cáo.");

            return response.Content.Result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 15/02/2025
        /// </summary>
        public float CalculateAvgTotal(List<VdsTrafficStatisticResponse>? tsList, TmsEquipmentResponse equipment, string type, DateTime? fromTime = null, DateTime? toTime = null)
        {
            if((tsList?.Count() ?? 0) == 0 || equipment == null)
                return 0;

            var filterList = fromTime != null && toTime != null
                    ? tsList.Where(item =>
                        item.EquipmentId == equipment.ID
                        && item.FromTime >= fromTime
                        && item.ToTime < toTime
                    ).ToList()
                    : tsList.Where(item =>
                        item.EquipmentId == equipment.ID
                    ).ToList();

            if (type == "speed")
            {
                var avgSpeed = filterList.Count() > 0
                    ? filterList.Average(item => item.AvgSpeed)
                    : 0;

                return avgSpeed != null ? (float)avgSpeed.Value : (float)0;
            }

            if (type == "total")
            {
                var total = filterList.Count() > 0
                ? filterList.Sum(item => item.TotalVehicle)
                : 0;

                return total != null ? (int)total : 0;
            }

            return 0;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 15/02/2025
        /// </summary>
        private void CheckValidInput(string cycle, DateTime fromDate, DateTime toDate)
        {
            var idx = 0;
            var timeRange = toDate - fromDate;

            if (cycle.Equals("minute", StringComparison.OrdinalIgnoreCase) && (timeRange.TotalHours < 0 || timeRange.TotalHours > 24))
            {
                var msg = Regex.Replace(this._localizer[BaseMsg.RptTrafficStatistic.Validation.LimitHourRange], @"\{.*?\}", match => { return $"{{{idx++}}}"; });
                throw Oops.Oh(msg, 24);
            }

            if (cycle.Equals("hour", StringComparison.OrdinalIgnoreCase) && (timeRange.TotalDays < 0 || timeRange.TotalDays > 7))
            {
                var msg = Regex.Replace(this._localizer[BaseMsg.RptTrafficStatistic.Validation.LimitDateRange], @"\{.*?\}", match => { return $"{{{idx++}}}"; });
                throw Oops.Oh(msg, 7);
            }

            if (cycle.Equals("day", StringComparison.OrdinalIgnoreCase) && (timeRange.TotalDays < 0 || timeRange.TotalDays > 7))
            {
                var msg = Regex.Replace(this._localizer[BaseMsg.RptTrafficStatistic.Validation.LimitDateRange], @"\{.*?\}", match => { return $"{{{idx++}}}"; });
                throw Oops.Oh(msg, 7);
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Set dữ liệu cho tham số ra
        /// Created date: 15/02/2025
        /// </summary>
        private void SetReportParameters(TrafficStatisticReport reportData, TrafficStatisticZoneRenderInput command = null)
        {
            var dataDate = command?.FromDate != null && command?.ToDate != null
            ? command.FromDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault) + " - " + command.ToDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault)
            : string.Empty;

            reportData.Parameters.DataDate = dataDate;
            reportData.Parameters.ReportDate = DateTime.Now.ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = RptBaseHelper.LogoHp.GetLogo();
            reportData.Parameters.ReportSign = this.GetReportSign();

            // Dịch thuật tham số
            reportData.Parameters.ReportTitle = this._localizer[BaseMsg.RptTrafficStatistic.Label.ZoneRptTitle].Value.ToUpper();
            reportData.Parameters.PageLabel = this._localizer[BaseMsg.RptBase.Label.Page].Value;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Dịch thuật tiêu đề cột
        /// Created date: 15/02/2025
        /// </summary>
        private void LocalizeReportColumns(TrafficStatisticReport reportData, TrafficStatisticZoneRenderInput command = null)
        {
            reportData.Columns.Zone = this._localizer[BaseMsg.RptTrafficStatistic.Label.Zone].Value.ToUpper();
            reportData.Columns.Camera = this._localizer[BaseMsg.RptTrafficStatistic.Entity.EquipmentId].Value.ToUpper();
            reportData.Columns.AvgSpeed = this._localizer[BaseMsg.RptTrafficStatistic.Entity.AvgSpeed].Value.ToUpper();
            reportData.Columns.Amount = this._localizer[BaseMsg.RptTrafficStatistic.Entity.TotalVehicleNumber].Value.ToUpper();
            reportData.Columns.Time = this._localizer[BaseMsg.RptTrafficStatistic.Entity.FromTime].Value.ToUpper();
        }

        private string GetReportSign()
        {
            return "wwwroot/upload/sign.png";
        }
    }
}
