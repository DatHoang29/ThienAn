using Modules.WP.Controllers.Statistical.Dto;
using Modules.WP.Report.Controllers.Statistical.Dto;
using Shared.DTO.Dtos.System;
using Shared.DTO.Enums;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using Wolverine;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.WP.Report.Infrastructure.Services.Statistical
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Service xử lý dữ liệu báo cáo thống kê lưu lượng truy cập (WpStatistical)
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalService
    {
        private List<SysOrgResponse>? _orgList;
        private IMessageBus _messBus;

        public StatisticalService(IMessageBus messBus)
        {
            this._messBus = messBus;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Truy xuất và xử lý dữ liệu báo cáo thống kê lưu lượng truy cập theo giờ trong ngày
        /// Created date: 17/07/2026
        /// </summary>
        public async Task<StatisticalReportOutput> BuildStatisticalReport(StatisticalRenderInput command)
        {
            try
            {
                var reportData = new StatisticalReportOutput() { };

                // Load danh sách tổ chức...
                await this.LoadDefaultData();

                var valueDate = (command.Date ?? DateTime.Now).Date;

                // Truy xuất dữ liệu thống kê 24 giờ của ngày valueDate (tái sử dụng handler của Modules.WP)
                var chartDataList = await this._messBus.InvokeAsync<List<WpStatisticalChartOutput>>(new WpStatisticalChartInput()
                {
                    TypeStat = BaseEnums.Statistical.Hour,
                    Date = valueDate
                });

                // Build danh sách chi tiết 24 giờ
                var total = 0;
                for (int hour = 0; hour < 24; hour++)
                {
                    var visits = chartDataList?.FirstOrDefault(item => item.Label == hour.ToString())?.Visits ?? 0;

                    reportData.ChiTiet.Add(new StatisticalData()
                    {
                        STT = hour + 1,
                        Ngay = valueDate.ToString(DateTimeFormatEnum.DateDefault),
                        Gio = new TimeSpan(hour, 0, 0).ToString(@"hh\:mm\:ss"),
                        LuuLuong = visits
                    });

                    total += visits;
                }

                // Dòng tổng cộng
                reportData.Cong.Nhan = "Tổng cộng";
                reportData.Cong.LuuLuong = total;

                this.SetReportParameters(reportData, valueDate, command);

                this.SetReportSignature(reportData);

                return reportData;
            }
            catch
            {
                return new StatisticalReportOutput(); // Empty report
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Set dữ liệu cho tham số ra
        /// Created date: 17/07/2026
        /// </summary>
        private void SetReportParameters(StatisticalReportOutput reportData, DateTime valueDate, StatisticalRenderInput? command = null)
        {
            // Đơn vị: Lấy từ danh sách tổ chức (org gốc và org trực thuộc đầu tiên)
            var rootOrg = this._orgList?.FirstOrDefault();
            var subOrg = this._orgList?.Skip(1).FirstOrDefault();

            reportData.Parameter.DonViCapTren = rootOrg?.Name?.ToUpper() ?? string.Empty;
            reportData.Parameter.DonViTrucThuoc = subOrg?.Name?.ToUpper() ?? string.Empty;
            reportData.Parameter.Hinh = this.GetReportLogo();
            reportData.Parameter.Ngay = valueDate.ToString(DateTimeFormatEnum.DateDefault);
            reportData.Parameter.NgayHienTai = DateTime.Now.ToString(DateTimeFormatEnum.DateTimeDefaultRevert);
            reportData.Parameter.NgayXacDinh = "ngày....tháng....năm.....";
            reportData.Parameter.DiaDanh = "Tp. Hồ Chí Minh"; // TODO: Lấy từ cấu hình hệ thống
            reportData.Parameter.NhanVienBaoCao = string.Empty; // TODO: Lấy theo user đăng nhập
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Set dữ liệu chữ ký báo cáo
        /// Created date: 17/07/2026
        /// </summary>
        private void SetReportSignature(StatisticalReportOutput reportData)
        {
            // TODO: Lấy chức danh/người ký từ cấu hình hệ thống
            reportData.Signature.ChucDanh1 = "TRẠM TRƯỞNG";
            reportData.Signature.ChuKi1 = this.GetReportSign();
            reportData.Signature.NguoiKi1 = string.Empty;

            reportData.Signature.ChucDanh2 = "GIÁM ĐỐC";
            reportData.Signature.ChuKi2 = this.GetReportSign();
            reportData.Signature.NguoiKi2 = string.Empty;

            reportData.Signature.ChucDanh3 = "NGƯỜI LẬP BIỂU";
            reportData.Signature.ChuKi3 = this.GetReportSign();
            reportData.Signature.NguoiKi3 = string.Empty;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Truy xuất và xử lý dữ liệu báo cáo thống kê lưu lượng truy cập theo ngày trong tháng
        /// Created date: 17/07/2026
        /// </summary>
        public async Task<StatisticalMonthReportOutput> BuildStatisticalMonthReport(StatisticalMonthRenderInput command)
        {
            try
            {
                var reportData = new StatisticalMonthReportOutput() { };

                // Load danh sách tổ chức...
                await this.LoadDefaultData();

                var year = command.Year ?? DateTime.Now.Year;
                var month = command.Month ?? DateTime.Now.Month;

                // Truy xuất dữ liệu thống kê từng ngày trong tháng (tái sử dụng handler của Modules.WP)
                var chartDataList = await this._messBus.InvokeAsync<List<WpStatisticalChartOutput>>(new WpStatisticalChartInput()
                {
                    TypeStat = BaseEnums.Statistical.Date,
                    Year = year,
                    Month = month
                });

                // Build danh sách chi tiết theo ngày trong tháng
                var total = 0;
                var daysInMonth = DateTime.DaysInMonth(year, month);
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(year, month, day);
                    var visits = chartDataList?.FirstOrDefault(item => item.Label == date.ToString("yyyy-MM-dd"))?.Visits ?? 0;

                    reportData.ChiTiet.Add(new StatisticalMonthData()
                    {
                        STT = day,
                        Thang = date.ToString(DateTimeFormatEnum.MonthYearDefault),
                        NgayThangNam = date.ToString(DateTimeFormatEnum.DateDefault),
                        LuuLuong = visits
                    });

                    total += visits;
                }

                // Dòng tổng cộng
                reportData.Cong.Nhan = "Tổng cộng";
                reportData.Cong.LuuLuong = total;

                this.SetMonthReportParameters(reportData, year, month, command);

                reportData.Signature = this.BuildReportSignature();

                return reportData;
            }
            catch
            {
                return new StatisticalMonthReportOutput(); // Empty report
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Truy xuất và xử lý dữ liệu báo cáo thống kê lưu lượng truy cập theo tháng trong năm
        /// Created date: 17/07/2026
        /// </summary>
        public async Task<StatisticalYearReportOutput> BuildStatisticalYearReport(StatisticalYearRenderInput command)
        {
            try
            {
                var reportData = new StatisticalYearReportOutput() { };

                // Load danh sách tổ chức...
                await this.LoadDefaultData();

                var year = command.Year ?? DateTime.Now.Year;

                // Truy xuất dữ liệu thống kê từng tháng trong năm (tái sử dụng handler của Modules.WP)
                var chartDataList = await this._messBus.InvokeAsync<List<WpStatisticalChartOutput>>(new WpStatisticalChartInput()
                {
                    TypeStat = BaseEnums.Statistical.Month,
                    Year = year
                });

                // Build danh sách chi tiết 12 tháng
                var total = 0;
                for (int month = 1; month <= 12; month++)
                {
                    var visits = chartDataList?.FirstOrDefault(item => item.Label == month.ToString())?.Visits ?? 0;

                    reportData.ChiTiet.Add(new StatisticalYearData()
                    {
                        STT = month,
                        Nam = year,
                        ThangNam = new DateTime(year, month, 1).ToString(DateTimeFormatEnum.MonthYearDefault),
                        LuuLuong = visits
                    });

                    total += visits;
                }

                // Dòng tổng cộng
                reportData.Cong.Nhan = "Tổng cộng";
                reportData.Cong.LuuLuong = total;

                this.SetYearReportParameters(reportData, year, command);

                reportData.Signature = this.BuildReportSignature();

                return reportData;
            }
            catch
            {
                return new StatisticalYearReportOutput(); // Empty report
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Set dữ liệu cho tham số ra của báo cáo tháng
        /// Created date: 17/07/2026
        /// </summary>
        private void SetMonthReportParameters(StatisticalMonthReportOutput reportData, int year, int month, StatisticalMonthRenderInput? command = null)
        {
            // Đơn vị: Lấy từ danh sách tổ chức (org gốc và org trực thuộc đầu tiên)
            var rootOrg = this._orgList?.FirstOrDefault();
            var subOrg = this._orgList?.Skip(1).FirstOrDefault();

            reportData.Parameter.DonViCapTren = rootOrg?.Name?.ToUpper() ?? string.Empty;
            reportData.Parameter.DonViTrucThuoc = subOrg?.Name?.ToUpper() ?? string.Empty;
            reportData.Parameter.Hinh = this.GetReportLogo();
            reportData.Parameter.Thang = month;
            reportData.Parameter.Nam = year;
            reportData.Parameter.NgayHienTai = DateTime.Now.ToString(DateTimeFormatEnum.DateTimeDefaultRevert);
            reportData.Parameter.NgayXacDinh = "ngày....tháng....năm.....";
            reportData.Parameter.DiaDanh = "Tp. Hồ Chí Minh"; // TODO: Lấy từ cấu hình hệ thống
            reportData.Parameter.NhanVienBaoCao = string.Empty; // TODO: Lấy theo user đăng nhập
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Set dữ liệu cho tham số ra của báo cáo năm
        /// Created date: 17/07/2026
        /// </summary>
        private void SetYearReportParameters(StatisticalYearReportOutput reportData, int year, StatisticalYearRenderInput? command = null)
        {
            // Đơn vị: Lấy từ danh sách tổ chức (org gốc và org trực thuộc đầu tiên)
            var rootOrg = this._orgList?.FirstOrDefault();
            var subOrg = this._orgList?.Skip(1).FirstOrDefault();

            reportData.Parameter.DonViCapTren = rootOrg?.Name?.ToUpper() ?? string.Empty;
            reportData.Parameter.DonViTrucThuoc = subOrg?.Name?.ToUpper() ?? string.Empty;
            reportData.Parameter.Hinh = this.GetReportLogo();
            reportData.Parameter.Nam = year;
            reportData.Parameter.NgayHienTai = DateTime.Now.ToString(DateTimeFormatEnum.DateTimeDefaultRevert);
            reportData.Parameter.NgayXacDinh = "ngày....tháng....năm.....";
            reportData.Parameter.DiaDanh = "Tp. Hồ Chí Minh"; // TODO: Lấy từ cấu hình hệ thống
            reportData.Parameter.NhanVienBaoCao = string.Empty; // TODO: Lấy theo user đăng nhập
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Build dữ liệu chữ ký báo cáo (dùng chung cho báo cáo tháng/năm)
        /// Created date: 17/07/2026
        /// </summary>
        private StatisticalSignature BuildReportSignature()
        {
            // TODO: Lấy chức danh/người ký từ cấu hình hệ thống
            return new StatisticalSignature()
            {
                ChucDanh1 = "TRẠM TRƯỞNG",
                ChuKi1 = this.GetReportSign(),
                NguoiKi1 = string.Empty,

                ChucDanh2 = "GIÁM ĐỐC",
                ChuKi2 = this.GetReportSign(),
                NguoiKi2 = string.Empty,

                ChucDanh3 = "NGƯỜI LẬP BIỂU",
                ChuKi3 = this.GetReportSign(),
                NguoiKi3 = string.Empty
            };
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy report logo
        /// Created date: 14/08/2026
        /// </summary>

        private string GetReportLogo()
        {
            return "wwwroot/upload/logo.png";
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy report sign
        /// Created date: 14/08/2026
        /// </summary>

        private string GetReportSign()
        {
            return "wwwroot/upload/sign.png";
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đệ quy biến đổi danh sách org dạng tree thành list
        /// Created date: 17/07/2026
        /// </summary>
        private void RecursivelyOrgList(List<SysOrgResponse> orgList, int depth = 0)
        {
            // Recursively up to a depth of 10
            if (depth > 10 || orgList == null)
                return;

            if (this._orgList == null)
                this._orgList = new List<SysOrgResponse>();

            orgList?.ForEach(org => {
                if (!this._orgList.Any(existOrg => existOrg.ID.Equals(org.ID, StringComparison.OrdinalIgnoreCase)))
                    this._orgList.Add(org);

                if (org?.Children?.Count() > 0)
                    this.RecursivelyOrgList(org.Children, depth + 1);
            });
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Load dữ liệu mặc định cần thiết
        /// Created date: 17/07/2026
        /// </summary>
        /// <returns></returns>
        private async Task LoadDefaultData()
        {
            try
            {
                // Fetch the org list
                var orgListQuery = new SysOrgListRequest() { ID = "0" };
                var orgListResponse = await BaseApiClient<IOrgApi>.Client().List(orgListQuery);
                var orgList = orgListResponse.Content?.Result;
                if (orgList?.Count() > 0)
                    this.RecursivelyOrgList(orgList);
            }
            catch { } // Không ném lỗi => Tiếp tục thực hiện task khác
        }
    }
}
