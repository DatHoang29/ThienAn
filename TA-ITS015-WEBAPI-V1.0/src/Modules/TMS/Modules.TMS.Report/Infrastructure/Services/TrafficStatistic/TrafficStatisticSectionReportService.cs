using Mapster;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Report.Controllers.TrafficStatistic.Dto;
using Modules.TMS.Report.Core.Exceptions;
using Modules.TMS.Report.Core.Helpers;
using Shared.DTO.Constants.Module;
using Shared.DTO.Dtos.System;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Dtos.Vds;
using Shared.Infrastructure.Services;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using Shared.Utility.Apis.Tms;
using Shared.Utility.Utilities;
using Wolverine;
using Modules.TMS.Extensions;
using TmsMsg = Modules.TMS.Core.Exceptions.BaseMsg;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Report.Infrastructure.Services.TrafficStatistic
{
    public class TrafficStatisticSectionReportService
    {
        private IStringLocalizer _localizer;
        private UserManager _user;

        private  Dictionary<string, string>? _directionConfigList;
        private IMessageBus _messBus;
        public TrafficStatisticSectionReportService(IStringLocalizer localizer,UserManager user,IMessageBus messBus)
        {
            this._localizer = localizer;
            _user = user;
            _messBus = messBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 15/02/2025
        /// Description: Truy xuất và xử lý dữ liệu báo cáo TrafficStatistic
        /// </summary>
        public async Task<TrafficStatisticReport> BuildTrafficStatisticReport(TrafficStatisticSectionRenderInput command)
        {
            var reportData = new TrafficStatisticReport() { };
            var cycle = command.Cycle ?? "5";
            command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

            var fromDate = command.FromDate ?? DateTime.Now.Date;
            var toDate = command.ToDate ?? DateTime.Now;

            var timeRange = toDate - fromDate;
            if (timeRange.TotalHours < 0 || timeRange.TotalHours > 24)
            {
                var idx = 0;
                throw Oops.Oh(BaseMsg.RptTrafficStatistic.Validation.LimitHourRange, 24);
            }

            var cycleValue = 0;
            int.TryParse(cycle, out cycleValue);
            if (cycleValue <= 0)
                cycleValue = 5;

            //var zoneQuery = new TmsZoneListRequest() { ID = command.ZoneId};
            //var zoneResponse = await BaseApiClient<IZoneApi>.Client().List(zoneQuery);
            //var zoneList = zoneResponse.Content?.Result?.Where(item => item.Type == "section").ToList();
            //if (command.ZoneId != null)
            //    zoneList = zoneList.Where(item => item.ID == command.ZoneId).ToList();
            //if((zoneList?.Count() ?? 0) == 0)
            //    return reportData;

            //ZoneID: ko có hoặc truyền ID đoạn
            //Lấy thông tin zone và các zone con liên quan  (section-zone)
            var zoneQuery = new TmsZoneWithChildrenRequest() { ID = command.ZoneId};
            var zoneResponse = await BaseApiClient<IZoneApi>.Client(null, null, TimeSpan.FromSeconds(30)).TreeWithChild(zoneQuery);
            if (!zoneResponse.IsSuccessStatusCode || zoneResponse.Content?.Result == null)
                throw new InvalidOperationException("Không đọc được danh sách đoạn tuyến để lập báo cáo.");
            var zoneList = zoneResponse.Content.Result.ToList();
            //Tìm danh sách thiết bị tương ứng với zone
            
            
            
            //Thống kê DL từ bảng static từ thiết bị-zone tương ứng



            var tsQuery = new VdsTrafficStatisticRequest() { FromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Local).ToUniversalTime(), ToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Local).ToUniversalTime()};
            // 13/10/2026: gọi IN-PROCESS qua Wolverine (handler VDS TrafficStatisticQueryHandler) thay Refit
            // IVdsTrafficStatisticApi. Giới hạn 30s, lỗi gói thành thông báo chung (InvokeVdsAsync) — không thành báo cáo rỗng.
            var tsList = await _messBus.InvokeVdsAsync<List<VdsTrafficStatisticResponse>>(
                tsQuery, TimeSpan.FromSeconds(30), TmsMsg.VdsQuery.Exception.ReportStatisticUnavailable);
            foreach (var row in tsList)
            {
                row.FromTime = BaseUtil.TimeServer.ToServerTime(row.FromTime);
                if (row.ToTime.HasValue)
                    row.ToTime = BaseUtil.TimeServer.ToServerTime(row.ToTime.Value);
            }

            var timeList = TrafficStatisticService.GetTimeList(fromDate, toDate, cycleValue);

            var tasks = zoneList.Select(async zone =>
            {
                var equipmentList = await this.GetEquipmentList(zone);
                var itemList = new List<TrafficStatisticItemOutput>();

                foreach (var timeRange in timeList)
                {
                    itemList.Add(new TrafficStatisticItemOutput()
                    {
                        AvgSpeed = this.CalculateAvgTotal(tsList, equipmentList, "speed", timeRange.FromTime, timeRange.ToTime),
                        SpaceOccupy = this.CalculateAvgTotal(tsList, equipmentList, "occupy", timeRange.FromTime, timeRange.ToTime),
                        TotalVehicle = (int)this.CalculateAvgTotal(tsList, equipmentList, "total", timeRange.FromTime, timeRange.ToTime),
                        Time = timeRange.ValueTime.ToString(DateTimeFormatEnum.DateTimeDefaultRevert)
                    });
                }

                // NOTE: Xem xét chu kỳ 5 phút đối với báo cáo theo Đoạn Tuyến
                // NOTE: Có group theo FromTime và ToTime tính Sum và Avg cho toàn bộ camera thuộc đoạn tuyến

                return new TrafficStatisticReportOutput()
                {
                    Section = zone.Name,
                    AvgSpeed = this.CalculateAvgTotal(tsList, equipmentList, "speed"),
                    SpaceOccupy = this.CalculateAvgTotal(tsList, equipmentList, "occupy"),
                    TotalVehicle = (int)this.CalculateAvgTotal(tsList, equipmentList, "total"),
                    Items = itemList
                };
            });
            reportData.DataList.AddRange(await Task.WhenAll(tasks));

            if (reportData.DataList?.Count() > 0)
            {
                var dataList = reportData.DataList.OrderBy(item => item.Section).ToList();
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
        public async Task<List<TmsEquipmentResponse>> GetEquipmentList(TmsZoneResponse zone)
        {
            var directionId = zone.LaneId != null ? ((int)zone.LaneId).ToString() : null;
            //var request = new TmsEquipmentPositionRequest() { 
            //    ZoneId = zone.ID,
            //    DirectionId = directionId
            //};
            //var response = await BaseApiClient<IEquipmentApi>.Client(null,null,TimeSpan.FromSeconds(7)).ListByPosition(request);
            //var equipmentList = response.Content?.Result;

            //return equipmentList ?? new List<TmsEquipmentResponse>();

            var list = await _messBus.InvokeAsync<List<TmsEquipmentDataOutput>>(new TmsEquipmentPositionInput()
            {
                ZoneId = zone.ID,
                DirectionId = directionId
            });

            return list.Adapt<List<TmsEquipmentResponse>>();
        }





        /// <summary>
        /// Author: ThangNN
        /// Created date: 15/02/2025
        /// </summary>
        public float CalculateAvgTotal(List<VdsTrafficStatisticResponse>? tsList, List<TmsEquipmentResponse>? equipmentList, string type, DateTime? fromTime = null, DateTime? toTime = null)
        {
            if((tsList?.Count() ?? 0) == 0 || (equipmentList?.Count() ?? 0) == 0 ||tsList == null || equipmentList == null)
                return 0;

            var equipIdList = equipmentList.Select(item => item.ID).ToList();

            var filterList = tsList
                .Where(item => equipIdList.Contains(item.EquipmentId))
                .WhereIF(fromTime != null, item => item.FromTime >= fromTime)
                .WhereIF(toTime != null, item => item.ToTime < toTime)
                .ToList();
               

            if (filterList.Count == 0) return 0;
            
            if (type == "speed")
            {
                var avgSpeed = filterList.Count() > 0
                    ? filterList.Average(item => item.AvgSpeed)
                    : 0;

                return (float)(avgSpeed ?? 0);
            }

            if (type == "total")
            {
                var total = filterList.Count() > 0
                ? filterList.Sum(item => item.TotalVehicle)
                : 0;

                return total ?? 0;
            }

            if (type == "occupy")
            {
                // Bản VDS: SpaceOccupy là double? (đã là số), không còn parse string như AvgSpaceOccupy cũ.
                var avgOccupy = filterList.Count() > 0
                ? filterList.Average(item => item.SpaceOccupy)
                : 0;

                return (float)((avgOccupy ?? 0) * 100);
            }

            return 0;
        }

        /// <summary> 
        /// Author: ThangNN
        /// Created date: 15/02/2025
        /// Description: Set dữ liệu cho tham số ra
        /// </summary>
        private void SetReportParameters(TrafficStatisticReport reportData, TrafficStatisticSectionRenderInput command = null)
        {
            var dataDate = command?.FromDate != null && command?.ToDate != null
            ? command.FromDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault) + " - " + command.ToDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault)
            : string.Empty;

            reportData.Parameters.DataDate = dataDate;
            reportData.Parameters.ReportUser = _user.Account;
            reportData.Parameters.ReportDate = DateTime.Now.ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = RptBaseHelper.LogoHp.GetLogo();
            reportData.Parameters.ReportSign = this.GetReportSign();

            // Dịch thuật tham số
            reportData.Parameters.ReportTitle = this._localizer[BaseMsg.RptTrafficStatistic.Label.SectionRptTitle].Value.ToUpper();
            reportData.Parameters.PageLabel = this._localizer[BaseMsg.RptBase.Label.Page].Value;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 15/02/2025
        /// Description: Dịch thuật tiêu đề cột
        /// </summary>
        private void LocalizeReportColumns(TrafficStatisticReport reportData, TrafficStatisticSectionRenderInput command = null)
        {
            reportData.Columns.Section = this._localizer[BaseMsg.RptTrafficStatistic.Label.Section].Value.ToUpper();
            reportData.Columns.AvgSpeed = this._localizer[BaseMsg.RptTrafficStatistic.Entity.AvgSpeed].Value.ToUpper();
            reportData.Columns.TotalVehicle = this._localizer[BaseMsg.RptTrafficStatistic.Entity.TotalVehicleNumber].Value.ToUpper();
            reportData.Columns.SpaceOccupy = this._localizer[BaseMsg.RptTrafficStatistic.Entity.SpaceOccupy].Value.ToUpper();
            reportData.Columns.Time = this._localizer[BaseMsg.RptTrafficStatistic.Entity.FromTime].Value.ToUpper();
        }

        //private string GetReportLogo()
        //{
        //    return "wwwroot/upload/logo.png";
        //}


        private string GetReportSign()
        {
            return "wwwroot/upload/sign.png";
        }

        ///// <summary>
        ///// Load các cấu hình liên quan đến dữ liệu này
        ///// </summary>
        ///// <returns></returns>
        //private async Task LoadConfig()
        //{
        //    //Load cấu hình hướng di chuyển
        //    var query = new SysOpConfigListRequest() { GroupCode = ItsOpConfigConst.ItsOpConfig };
        //    var response = await BaseApiClient<IOpConfigApi>.Client(null,null,TimeSpan.FromSeconds(5)).List(query);
        //    this._directionConfigList = response.Content?.Result?.ToDictionary(x => x.Code, x => x.Value) ?? new Dictionary<string, string>();
        //}


    }
}