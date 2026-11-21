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

using Modules.TMS.Report.Infrastructure.Services.TrafficData.Rules;

namespace Modules.TMS.Report.Infrastructure.Services.TrafficData
{
    public class TrafficDataMonthService
    {
        private List<SysConfigDataResponse> _configDataList;
        private List<SysOpConfigResponse> _sysOpConfigList;
        private List<SysConfigDataResponse> _directionList;
        private IStringLocalizer _localizer;
        private IMessageBus _messBus;
        public TrafficDataMonthService(IStringLocalizer localizer, IMessageBus bus)
        {
            this._localizer = localizer;
            _messBus = bus;
        }

        private List<string> _excludedDirectionList = new List<string>() { "0", "all" };

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Truy xuất và xử lý dữ liệu báo cáo TrafficDataDay
        /// </summary>
        public async Task<TrafficDataMonthReport> BuildTrafficDataMonthReportList(TrafficDataMonthRenderInput command)
        {
            try
            {
                var reportData = new TrafficDataMonthReport() { };

                await this.LoadDefaultData();

                // Lấy giá trị thời gian được cấu hình cho ca
                // IMPORTANCE: DateTime C# GIỜ TỪ 0 - 23 | KHÔNG CÓ 24
                var workShiftInfo = this.GetWorkTimeInfo(command.ReportType);

                // Lấy danh sách ngày của tháng
                var daysInMonth = this.GetDaysInMonth(command.Month, command.Year);

                if ((this._directionList?.Count() ?? 0) == 0 || workShiftInfo == null || daysInMonth == null)
                    return reportData;

                var fromDate = daysInMonth.Min(); // Ngày đầu của tháng
                var toDate = daysInMonth.Max(); // Ngày cuối của tháng

                fromDate = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, 0); // Đầu ngày của ngày đầu tháng
                toDate = new DateTime(toDate.Year, toDate.Month, toDate.Day, 23, 59, 59, 999); // Cuối ngày của ngày cuối tháng

                // Cờ TỔNG HỢP này chỉ để MỞ RỘNG khoảng truy xuất dữ liệu sang ngày kế, KHÔNG được dùng để
                // dựng khoảng thời gian của một ca cụ thể — mỗi ca đã có cờ riêng trong workShiftInfo.
                var anyShiftCrossesMidnight = workShiftInfo.Any(shiftInfo => shiftInfo.HasNextDayShift == true);
                if (anyShiftCrossesMidnight)
                    toDate = toDate.AddDays(1);

                // Truy xuất dữ liệu trong 1 tháng hoặc 1 tháng + 1 ngày (nếu có ca lưỡng tính)
                // Đếm xe theo (giờ, direction, cỡ xe) gộp ở QuestDB — KHÔNG kéo raw cả tháng vào RAM nữa.
                // Cutover 03/09/2026: nguồn là module VDS (VdsTrafficData trên QuestDB).
                // 13/10/2026: gọi IN-PROCESS qua Wolverine (handler VDS TrafficDataQueryHandler) thay Refit
                // IVdsTrafficDataApi. Giới hạn 30s, lỗi gói thành thông báo chung (InvokeVdsAsync) — không thành báo cáo số 0.
                var trafficDataList = await _messBus.InvokeVdsAsync<List<VdsTrafficVolumeBreakdownRow>>(
                    new VdsTrafficVolumeBreakdownRequest { FromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Local).ToUniversalTime(), ToDate = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime() }, TimeSpan.FromSeconds(30), TmsMsg.VdsQuery.Exception.ReportDataUnavailable);
                foreach (var row in trafficDataList)
                    row.BucketTime = BaseUtil.TimeServer.ToServerTime(row.BucketTime);

                var totalByMonth = 0; // Tổng tất cả theo tháng (cuối)
                var totalByMonthAndShortLength = 0; // Tổng theo tháng theo loại xe < 6m (cuối)
                var totalByMonthAndMediumLength = 0; // Tổng theo tháng theo loại xe 6m-12m (cuối)
                var totalByMonthAndLongLength = 0; // Tổng theo tháng theo loại xe > 12m (cuối)

                // Tính toán dữ liệu báo cáo dựa trên dữ liệu đã lọc
                // Duyệt ngày trong tháng (Cấp 1) => Title: Theo ngày | Total: Theo ngày
                foreach (var day in daysInMonth)
                {
                    var trafficDataDay = new TrafficDataDayOutput()
                    {
                        Day = day,
                        Title = day.ToString(DateTimeFormatEnum.DayMonthDefault),
                        TotalByLengthTitle = "TỔNG"
                    };

                    var totalByDay = 0; // Tổng theo ngày (ngang)
                    var totalByDayAndShortLength = 0; // Tổng theo ngày theo loại xe < 6m (dọc)
                    var totalByDayAndMediumLength = 0; // Tổng theo ngày theo loại xe 6m-12m (dọc)
                    var totalByDayAndLongLength = 0; // Tổng theo ngày theo loại xe > 12m (dọc)

                    // Duyệt danh sách ca làm việc (Cấp 2) => Title: Theo ca | Total: Theo ca
                    var idx = 0;
                    foreach (var shift in workShiftInfo)
                    {
                        var trafficDataShift = new TrafficDataShiftOutput()
                        {
                            Shift = shift.ShiftCode,
                            Title = shift.ShiftName
                        };

                        var totalByShift = 0; // Tổng theo ca

                        // Duyệt danh sách hướng (Cấp 3) => Title: Theo hướng | Total: Theo hướng
                        foreach (var dir in this._directionList.OrderBy(x => x.OrderNo))
                        {
                            // Dùng cờ CỦA TỪNG CA: ca ngày 6h-14h phải kết thúc trong cùng ngày. Trước
                            // 25/09/2026 chỗ này truyền cờ tổng hợp, nên chỉ cần cấu hình có MỘT ca đêm là mọi
                            // ca ngày bị cộng thêm 24 giờ và các ca đè lên nhau.
                            var shortLengthVehicleCount = this.CountVehicleByShift(day, shift.HasNextDayShift, shift.FromHour, shift.ToHour, dir.Code, TmsVehicleLengthEnum.Short, trafficDataList);
                            var mediumLengthVehicleCount = this.CountVehicleByShift(day, shift.HasNextDayShift, shift.FromHour, shift.ToHour, dir.Code, TmsVehicleLengthEnum.Medium, trafficDataList);
                            var longLengthVehicleCount = this.CountVehicleByShift(day, shift.HasNextDayShift, shift.FromHour, shift.ToHour, dir.Code, TmsVehicleLengthEnum.Long, trafficDataList);

                            var totalByDirection = shortLengthVehicleCount + mediumLengthVehicleCount + longLengthVehicleCount; // Tổng heo hướng trên mỗi ca (tất cả loại xe)
                            trafficDataShift.TrafficVolumeList.Add(new TrafficVolumeInfo()
                            {
                                Direction = dir.Code,
                                DirectionName = dir.Name,
                                ShortLengthVehicleCount = shortLengthVehicleCount,
                                MediumLengthVehicleCount = mediumLengthVehicleCount,
                                LongLengthVehicleCount = longLengthVehicleCount,
                                TotalVehicleCount = totalByDirection
                            });

                            totalByShift += totalByDirection; // Cộng dồn cho ca
                            totalByDay += totalByDirection; // Cộng dồn cho ngày

                            totalByDayAndShortLength += shortLengthVehicleCount; // Cộng dồn cho ngày theo loại xe < 6m
                            totalByDayAndMediumLength += mediumLengthVehicleCount; // Cộng dồn cho ngày theo loại xe 6m-12m
                            totalByDayAndLongLength += longLengthVehicleCount; // Cộng dồn cho ngày theo loại xe > 12m

                            totalByMonthAndShortLength += shortLengthVehicleCount; // Cộng dồn cho tháng  theo loại xe < 6m
                            totalByMonthAndMediumLength += mediumLengthVehicleCount; // Cộng dồn cho tháng theo loại xe 6m-12m
                            totalByMonthAndLongLength += longLengthVehicleCount; // Cộng dồn cho tháng theo loại xe > 12m
                            totalByMonth += shortLengthVehicleCount + mediumLengthVehicleCount + longLengthVehicleCount; // Cộng dồn tất cả theo tháng
                        }

                        trafficDataShift.TotalByShift = totalByShift; // Tổng theo ca
                        trafficDataShift.TotalByShiftStr = (++idx % 2 == 0 ? " " : "") + totalByShift.ToString("N0");
                        trafficDataDay.TrafficDataShiftList.Add(trafficDataShift);
                    }

                    trafficDataDay.TotalByDay = totalByDay; // Tổng theo ngày
                    trafficDataDay.TotalByDayAndShortLength = totalByDayAndShortLength; // Tổng theo ngày và theo loại < 6m
                    trafficDataDay.TotalByDayAndMediumLength = totalByDayAndMediumLength; // Tổng theo ngày và theo loại 6m-12m
                    trafficDataDay.TotalByDayAndLongLength = totalByDayAndLongLength; // Tổng theo ngày và theo loại > 12m

                    reportData.TrafficDataDayList.Add(trafficDataDay);
                }

                reportData.TotalByMonth = totalByMonth; // Tổng tất cả theo tháng
                reportData.TotalByMonthAndShortLength = totalByMonthAndShortLength; // Tổng tất cả theo tháng và theo loại < 6m
                reportData.TotalByMonthAndMediumLength = totalByMonthAndMediumLength; // Tổng tất cả theo tháng và theo loại 6m-12m
                reportData.TotalByMonthAndLongLength = totalByMonthAndLongLength; // Tổng tất cả theo tháng và theo loại > 12m

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
        /// Description: Set dữ liệu cho tham số ra
        /// </summary>
        private void SetReportParameters(TrafficDataMonthReport reportData, TrafficDataMonthRenderInput command = null)
        {
            reportData.Parameters.DataDate = this._localizer[BaseMsg.RptBase.Label.Month].Value + $" {command.Month.ToString().PadLeft(2, '0')}/{command.Year}";
            reportData.Parameters.ReportDate = DateTime.Now.ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = RptBaseHelper.LogoHp.GetLogo();
            reportData.Parameters.ReportSign = this.GetReportSign();

            // Dịch thuật tham số
            //"BÁO CÁO LƯU LƯỢNG GIAO THÔNG THEO THÁNG"
            reportData.Parameters.ReportTitle = this._localizer[BaseMsg.RptTrafficVolumeMonth.Label.RptTitle].Value.ToUpper();
            reportData.Parameters.PageLabel = this._localizer[BaseMsg.RptBase.Label.Page].Value;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Dịch thuật tiêu đề cột
        /// </summary>
        private void LocalizeReportColumns(TrafficDataMonthReport reportData, TrafficDataMonthRenderInput command = null)
        {
            reportData.Columns.Time = this._localizer[BaseMsg.RptBase.Label.Time].Value.ToUpper(); //"THỜI GIAN";
            reportData.Columns.Direction = this._localizer[BaseMsg.RptTrafficVolumeDay.Entity.Direction].Value.ToUpper(); //"HƯỚNG";
            reportData.Columns.ShortLength = this._localizer[BaseMsg.RptTrafficVolumeDay.Entity.ShortLength].Value.ToUpper(); //"XE DÀI < 6m";
            reportData.Columns.MediumLength = this._localizer[BaseMsg.RptTrafficVolumeDay.Entity.MediumLength].Value.ToUpper(); //"XE DÀI 6m-12m";
            reportData.Columns.LongLength = this._localizer[BaseMsg.RptTrafficVolumeDay.Entity.LongLength].Value.ToUpper(); //"XE DÀI > 12m";
            reportData.Columns.ShiftTotal = this._localizer[BaseMsg.RptTrafficVolumeMonth.Label.ShiftTotal].Value.ToUpper(); //"TỔNG THEO CA";
            reportData.Columns.DirTotal = this._localizer[BaseMsg.RptTrafficVolumeMonth.Label.DirTotal].Value.ToUpper(); //"TỔNG THEO HƯỚNG";
            reportData.Columns.DayTotal = this._localizer[BaseMsg.RptTrafficVolumeMonth.Label.DayTotal].Value.ToUpper(); //"TỔNG THEO NGÀY";
            reportData.Columns.MonthTotal = this._localizer[BaseMsg.RptTrafficVolumeMonth.Label.MonthTotal].Value; //"TỔNG THEO THÁNG";
        }


        private string GetReportSign()
        {
            return "wwwroot/upload/sign.png";
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Build danh sách "khoảng thời gian làm việc" theo ngày
        /// </summary>
        private List<(int FromHour, int ToHour, string ShiftCode, string ShiftName, bool HasNextDayShift)> GetWorkTimeInfo(string? reportType)
        {
            if (string.IsNullOrWhiteSpace(reportType))
                return null;

            List<SysOpConfigResponse> workShiftList = null; // Danh sách cấu hình ca làm việc

            switch (reportType.Trim().ToLower())
            {
                case TmsTrafficDataReportTypeEnum.TrafficMonthReportByShift:
                    workShiftList = this._sysOpConfigList?.Where(config =>
                        config.Code != null
                        && config.GroupCode != null
                        && config.GroupCode.Trim().Equals(BaseOpConfigCodeEnum.DayWorkShift, StringComparison.OrdinalIgnoreCase)
                        && config.Code.Trim().StartsWith(WorkShiftEnum.WorkShift, StringComparison.OrdinalIgnoreCase)
                    ).OrderBy(x => x.OrderNo).ToList() ?? null;

                    break;

                case TmsTrafficDataReportTypeEnum.TrafficMonthReportByTime:
                    workShiftList = this._sysOpConfigList?.Where(config =>
                        config.Code != null
                        && config.GroupCode != null
                        && config.GroupCode.Trim().Equals(BaseOpConfigCodeEnum.DayWorkTime, StringComparison.OrdinalIgnoreCase)
                        && config.Code.Trim().StartsWith(WorkShiftEnum.WorkTime, StringComparison.OrdinalIgnoreCase)
                    ).OrderBy(x => x.OrderNo).ToList() ?? null;

                    break;
            }

            if ((workShiftList?.Count() ?? 0) == 0)
                return null;

            var workTimeInfo = new List<(int FromHour, int ToHour, string ShiftCode, string ShiftName, bool HasNextDayShift)>(); // Đối tượng thông tin các ca làm việc
            foreach (var config in workShiftList)
            {
                var (fromHour, toHour, hasNextDayShift) = TrafficReportShiftRules.Parse(config.Value);
                workTimeInfo.Add((fromHour, toHour, config.Code, (config.Name ?? config.Code).Replace("(", "\n("), hasNextDayShift));
            }

            return workTimeInfo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 28/12/2024
        /// Description: Đến số lượng xe theo khoảng thời gian
        /// </summary>
        private int CountVehicleByShift(DateTime valueDate, bool hasNextDayShift, int fromHour, int toHour, string dirCode, string lengthCode, List<VdsTrafficVolumeBreakdownRow> trafficDataList)
        {
            if ((trafficDataList?.Count() ?? 0) == 0)
                return 0;
            try
            {
                // IMPORTANCE: DateTime C# MAX (KẾT THÚC) GIỜ PHÚT GIÂY 23:59:59 | MIN (BẮT ĐẦU) 00:00:00
                // Khoảng thời gian theo ca => logic lại thời gian
                var fromTime = new DateTime(valueDate.Year, valueDate.Month, valueDate.Day, fromHour, 0, 0, 0);
                var toTime = hasNextDayShift ?
                    new DateTime(valueDate.AddDays(1).Year, valueDate.AddDays(1).Month, valueDate.AddDays(1).Day, (toHour - 1), 59, 59, 999)
                    :
                    new DateTime(valueDate.Year, valueDate.Month, valueDate.Day, (toHour - 1), 59, 59, 999);

                // Breakdown đã gộp theo giờ ở DB: mỗi row là một (BucketTime=đầu giờ, Direction, VehicleSize).
                // Đếm = cộng VehicleCount các row trong khoảng ca, đúng cỡ xe, đúng/khác hướng theo yêu cầu.
                var query = trafficDataList.Where(item =>
                    !string.IsNullOrWhiteSpace(item.Direction)
                    && !string.IsNullOrWhiteSpace(item.VehicleSize)
                    && item.BucketTime >= fromTime
                    && item.BucketTime <= toTime
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
        /// Description: Lấy số ngày của tháng
        /// </summary>
        private List<DateTime> GetDaysInMonth(int? month, int? year)
        {
            if (month is null || year is null)
                return null;

            int daysCount = DateTime.DaysInMonth(year.Value, month.Value);
            var dayList = new List<DateTime>();

            for (int i = 1; i <= daysCount; i++)
            {
                dayList.Add(new DateTime(year.Value, month.Value, i));
            }

            return dayList;
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
                this._configDataList = configDataListResponse.Content?.Result;

                // Fetch the sysOpConfig list

                var sysOpConfigListQuery = new SysOpConfigListRequest() { };
                var sysOpConfigListResponse = await BaseApiClient<IOpConfigApi>.Client().List(sysOpConfigListQuery);
                this._sysOpConfigList = sysOpConfigListResponse.Content?.Result;

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
                }
            }
            catch { } // Không ném lỗi => Tiếp tục thực hiện task khác
        }
    }
}
