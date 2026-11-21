using Modules.TMS.Report.Infrastructure.Services.TrafficData.Rules;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.TrafficData.Dto;
using Modules.TMS.Report.Controllers.TrafficData.Dto;
using Modules.TMS.Report.Core.Exceptions;
using Modules.TMS.Report.Core.Helpers;
using Shared.DTO.Dtos.System;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.Dtos.Vds;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using Shared.Utility.Apis.Tms;
using Shared.Utility.Utilities;
using Wolverine;
using Modules.TMS.Extensions;
using TmsMsg = Modules.TMS.Core.Exceptions.BaseMsg;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Report.Infrastructure.Services.TrafficData
{
    public class TrafficDataDayService
    {
        private List<SysConfigDataResponse> _configDataList;
        private List<SysConfigDataResponse> _directionList;
        private IStringLocalizer _localizer;
        private IMessageBus _messBus;

        private List<string> _excludedDirectionList = new List<string>(){ "0", "all" };

        public TrafficDataDayService(IStringLocalizer localizer, IMessageBus bus)
        {
            this._localizer = localizer;
            this._messBus = bus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Truy xuất và xử lý dữ liệu báo cáo TrafficDataDay
        /// </summary>
        public async Task<TrafficDataDayReport> BuildTrafficDataDayReport(TrafficDataDayRenderInput command)
        {
            try
            {
                var reportData = new TrafficDataDayReport() { };

                await this.LoadDefaultData();

                var workTimeInfo = this.GetWorkTimeInfo(command.WorkShiftValue);

                if ((this._directionList?.Count() ?? 0) == 0 || command.ValueDate == null || workTimeInfo.Count == 0)
                    return reportData;

                var valueDate = BaseUtil.TimeServer.GetTimeServer(command.ValueDate).Value;
                var fromDate = new DateTime(valueDate.Year, valueDate.Month, valueDate.Day, 0, 0, 0, 0); // Đầu ngày
                var toDate = new DateTime(valueDate.Year, valueDate.Month, valueDate.Day, 23, 59, 59, 999); // Cuối ngày
                // Cờ TỔNG HỢP này chỉ để MỞ RỘNG khoảng truy xuất dữ liệu sang ngày kế, KHÔNG được dùng để
                // quyết định một giờ cụ thể thuộc ngày nào — mỗi giờ đã có cờ riêng trong workTimeInfo.
                var anyHourCrossesMidnight = workTimeInfo.Any(timeInfo => timeInfo.HasNextDayShift == true);
                if (anyHourCrossesMidnight)
                    toDate = toDate.AddDays(1);

                // Truy xuất dữ liệu trong 1 ngày hoặc 2 ngày (nếu có ca lưỡng tính)
                // Đếm theo (giờ, direction, cỡ xe) gộp ở QuestDB — cutover 03/09/2026 sang module VDS.
                // 13/10/2026: gọi IN-PROCESS qua Wolverine (handler VDS TrafficDataQueryHandler) thay Refit
                // IVdsTrafficDataApi. Giới hạn 30s như Refit cũ; lỗi nội bộ phía VDS ghi log rồi trả thông báo chung
                // (InvokeVdsAsync) — vẫn là lỗi, không thành báo cáo số 0.
                var trafficDataList = await this._messBus.InvokeVdsAsync<List<VdsTrafficVolumeBreakdownRow>>(
                    new VdsTrafficVolumeBreakdownRequest { FromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Local).ToUniversalTime(), ToDate = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime() }, TimeSpan.FromSeconds(30), TmsMsg.VdsQuery.Exception.ReportDataUnavailable);
                foreach (var row in trafficDataList)
                    row.BucketTime = BaseUtil.TimeServer.ToServerTime(row.BucketTime);


                // Tính toán dữ liệu báo cáo dựa trên dữ liệu đã lọc

                var totalByShift = 0; // Tổng tất cả theo ca (cuối)
                var totalByShiftAndShortLength = 0; // Tổng theo ca theo loại xe < 6m (cuối)
                var totalByShiftAndMediumLength = 0; // Tổng theo ca theo loại xe 6m-12m (cuối)
                var totalByShiftAndLongLength = 0; // Tổng theo ca theo loại xe > 12m (cuối)

                foreach (var time in workTimeInfo)
                {
                    var trafficDataHour = new TrafficDataHourOutput()
                    {
                        Hour = time.Hour,
                        Title = time.HourName
                    };

                    foreach (var dir in this._directionList)
                    {
                        // Dùng cờ CỦA TỪNG GIỜ: ca 22h-6h thì 22h/23h thuộc ngày báo cáo, 0h-5h thuộc ngày kế.
                        // Trước 25/09/2026 chỗ này truyền cờ tổng hợp nên 22h/23h bị lấy sang ngày kế.
                        var shortLengthVehicleCount = this.CountVehicleByHour(valueDate, time.HasNextDayShift, time.Hour, dir.Code, TmsVehicleLengthEnum.Short, trafficDataList);
                        var mediumLengthVehicleCount = this.CountVehicleByHour(valueDate, time.HasNextDayShift, time.Hour, dir.Code, TmsVehicleLengthEnum.Medium, trafficDataList);
                        var longLengthVehicleCount = this.CountVehicleByHour(valueDate, time.HasNextDayShift, time.Hour, dir.Code, TmsVehicleLengthEnum.Long, trafficDataList);

                        trafficDataHour.TrafficVolumeList.Add(new TrafficVolumeInfo()
                        {
                            Direction = dir.Code,
                            DirectionName = dir.Name,
                            ShortLengthVehicleCount = shortLengthVehicleCount,
                            MediumLengthVehicleCount = mediumLengthVehicleCount,
                            LongLengthVehicleCount = longLengthVehicleCount,
                            TotalVehicleCount = shortLengthVehicleCount + mediumLengthVehicleCount + longLengthVehicleCount // Tổng theo giờ theo hướng (ngang)
                        });

                        if (!dir.Code.Equals("All", StringComparison.OrdinalIgnoreCase))
                        {
                            totalByShiftAndShortLength += shortLengthVehicleCount; // Cộng dồn theo loại xe < 6m (dọc)
                            totalByShiftAndMediumLength += mediumLengthVehicleCount; // Cộng dồn theo loại xe 6m-12m (dọc)
                            totalByShiftAndLongLength += longLengthVehicleCount; // Cộng dồn theo loại xe > 12m (dọc)
                            totalByShift += shortLengthVehicleCount + mediumLengthVehicleCount + longLengthVehicleCount; // Cộng dồn tất cả
                        }
                    }

                    reportData.TrafficDataList.Add(trafficDataHour);
                }

                reportData.TotalByDayAndShortLength = totalByShiftAndShortLength;
                reportData.TotalByDayAndMediumLength = totalByShiftAndMediumLength;
                reportData.TotalByDayAndLongLength = totalByShiftAndLongLength;
                reportData.TotalByDay = totalByShift;

                this.LocalizeReportColumns(reportData, command);

                this.SetReportParameters(reportData, command);

                return reportData;
            }
            catch
            {
                throw; // Never turn a failed data source into a zero report.
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Đến số lượng xe theo giờ
        /// </summary>
        private int CountVehicleByHour(DateTime valueDate, bool hasNextDayShift, int hour, string dirCode, string lengthCode, List<VdsTrafficVolumeBreakdownRow> trafficDataList)
        {
            if ((trafficDataList?.Count() ?? 0) == 0)
                return 0;

            try
            {
                // Breakdown gộp theo giờ: row có BucketTime = đầu giờ. Đếm = cộng VehicleCount các row đúng
                // giờ + đúng ngày (ca lưỡng tính sang ngày kế) + đúng cỡ xe + đúng/khác hướng.
                var targetDate = (hasNextDayShift ? valueDate.AddDays(1) : valueDate).Date;

                var query = trafficDataList.Where(item =>
                    !string.IsNullOrWhiteSpace(item.Direction)
                    && !string.IsNullOrWhiteSpace(item.VehicleSize)
                    && item.BucketTime.Hour == hour
                    && item.BucketTime.Date == targetDate
                    && item.VehicleSize!.Trim().Equals(lengthCode.Trim(), StringComparison.OrdinalIgnoreCase));

                query = dirCode.Trim().Equals("All", StringComparison.OrdinalIgnoreCase)
                    ? query.Where(item => !this._excludedDirectionList.Contains(item.Direction!))
                    : query.Where(item => item.Direction!.Trim().Equals(dirCode.Trim(), StringComparison.OrdinalIgnoreCase));

                return query.Sum(item => item.VehicleCount);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Set dữ liệu cho tham số ra
        /// </summary>
        private void SetReportParameters(TrafficDataDayReport reportData, TrafficDataDayRenderInput command = null)
        {
            var dataDate = command?.ValueDate != null
                ? command.ValueDate.Value.ToString(DateTimeFormatEnum.DateDefault)
                : string.Empty;

            reportData.Parameters.DataDate = command.WorkShiftName + " - " +dataDate;
            reportData.Parameters.ReportDate = DateTime.Now.ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = RptBaseHelper.LogoHp.GetLogo();
            reportData.Parameters.ReportSign = this.GetReportSign();

            //"BÁO CÁO LƯU LƯỢNG GIAO THÔNG THEO NGÀY"
            reportData.Parameters.ReportTitle = this._localizer[BaseMsg.RptTrafficVolumeDay.Label.RptTitle].Value.ToUpper();
            //"LƯU LƯỢNG GIAO THÔNG {0} - NGÀY {1}"
            reportData.Parameters.SubTitle = String.Format(this._localizer[BaseMsg.RptTrafficVolumeDay.Label.RptSubTitle].Value, command.WorkShiftName, command.ValueDate.Value.ToString(DateTimeFormatEnum.DateDefault)).ToUpper();
            reportData.Parameters.PageLabel = this._localizer[BaseMsg.RptBase.Label.Page].Value;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Dịch thuật tiêu đề cột
        /// </summary>
        private void LocalizeReportColumns(TrafficDataDayReport reportData, TrafficDataDayRenderInput command = null)
        {
            reportData.Columns.Time = this._localizer[BaseMsg.RptBase.Label.Time].Value.ToUpper(); //"THỜI GIAN";
            reportData.Columns.Direction = this._localizer[BaseMsg.RptTrafficVolumeDay.Entity.Direction].Value.ToUpper(); //"HƯỚNG";
            reportData.Columns.ShortLength = this._localizer[BaseMsg.RptTrafficVolumeDay.Entity.ShortLength].Value.ToUpper(); //"XE DÀI < 6m";
            reportData.Columns.MediumLength = this._localizer[BaseMsg.RptTrafficVolumeDay.Entity.MediumLength].Value.ToUpper(); //"XE DÀI 6m-12m";
            reportData.Columns.LongLength = this._localizer[BaseMsg.RptTrafficVolumeDay.Entity.LongLength].Value.ToUpper(); //"XE DÀI > 12m";
            reportData.Columns.HourTotal = this._localizer[BaseMsg.RptTrafficVolumeDay.Label.HourTotal].Value.ToUpper(); //"TỔNG";
            reportData.Columns.ShiftTotal = this._localizer[BaseMsg.RptTrafficVolumeDay.Label.ShiftTotal].Value; //"TỔNG";
        }


        private string GetReportSign()
        {
            return "wwwroot/upload/sign.png";
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Build danh sách "khoảng giờ làm việc" theo ca
        /// </summary>
        private List<(int Hour, string HourName, bool HasNextDayShift)> GetWorkTimeInfo(string? shiftValue)
        {
            var (fromHour, toHour, _) = TrafficReportShiftRules.Parse(shiftValue);

            var workTimeInfo = new List<(int Hour, string ShiftName, bool HasNextDayShift)>();

            if (fromHour < toHour)
            {
                // IMPORTACNE: hour < toHour => ĐẢM BẢO MAX = 23 KHÔNG CHẠM 24
                for (int hour = fromHour; hour < toHour; hour++)
                {
                    workTimeInfo.Add((hour, $"{hour}h-{hour + 1}h", false));
                }
            }
            else // => CA LÀM VIỆC LƯỠNG TÍNH NẰM GIỮA 2 NGÀY
            {
                // IMPORTACNE: hour <= 23 => MAX = 23 KHÔNG CHẠM 24
                for (int hour = fromHour; hour <= 23; hour++)
                {
                    workTimeInfo.Add((hour, $"{hour}h-{hour + 1}h", false));
                }

                // IMPORTACNE: hour < toHour
                for (int hour = 0; hour < toHour; hour++)
                {
                    workTimeInfo.Add((hour, $"{hour}h-{hour + 1}h", true));
                }
            }

            return workTimeInfo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Load dữ liệu mặc định cần thiết
        /// </summary>
        /// <returns></returns>
        private async Task LoadDefaultData()
        {
            try
            {
                // Fetch the configData list
                
                var configDataListQuery = new SysConfigDataListRequest() { };
                var configDataListResponse = await BaseApiClient<IConfigDataApi>.Client().List(configDataListQuery);
                this._configDataList = configDataListResponse.Content?.Result ?? new List<SysConfigDataResponse>();

                // Get the direction list
                this._directionList = this._configDataList?.Where
                    (config => 
                        !string.IsNullOrWhiteSpace(config.Code)
                        && !string.IsNullOrWhiteSpace(config.ConfigTypeCode)
                        && config.ConfigTypeCode.Equals(SysConfigTypeCodeEnum.TmsDirectionConfigCode, StringComparison.OrdinalIgnoreCase)
                    ).ToList() ?? null;

                // Filter the direction list
                if (this._directionList?.Count() > 0)
                {
                    // Get the exclude direction list => Ex: 0, All...
                    var excludeDirectionList = this._configDataList?.Where
                        (config =>
                            !string.IsNullOrWhiteSpace(config.Code)
                            && !string.IsNullOrWhiteSpace(config.ConfigTypeCode)
                            && config.ConfigTypeCode.Equals(SysConfigTypeCodeEnum.TmsDirectionAllCode, StringComparison.OrdinalIgnoreCase)
                        ).ToList() ?? null;

                    if (excludeDirectionList?.Count() > 0)
                        this._excludedDirectionList = excludeDirectionList.Select(config => config.Code.Trim().ToLower()).ToList();

                    // Removing unused directions
                    this._directionList = this._directionList.Where(dir => !this._excludedDirectionList.Contains(dir.Code.Trim().ToLower())).ToList();

                    // Add direction "All"
                    if (this._directionList?.Count() > 0)
                    {
                        this._directionList.Add(new SysConfigDataResponse()
                        {
                            Code = "All",
                            Name = this._localizer[BaseMsg.RptBase.Label.Total].Value
                        });
                    }
                }
            }
            catch { } // Không ném lỗi => Tiếp tục thực hiện task khác
        }
    }
}
