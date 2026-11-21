using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoThoiGian;
using Module.TOLL.Report.Core.Helpers;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Report.Core.Dto.InfoConfig;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Report.Infrastructure;
using Shared.DTO.Constants.Its015;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.TOLL.Report.Controllers.DoanhThu.DoanhThuTheoThoiGian.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý render/export báo cáo doanh thu theo thời gian (DTTTG)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTTGQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private const string reportCode = TollReportCode.DoanhThuTheoThoiGian;
        private BaseRepository<TollTicketOutSummary> _baseRepo;
        UserManager _userManager;

        public DTTTGQueryHandler(IMessageBus messBus, BaseRepository<TollTicketOutSummary> baseRepo, UserManager userManager)
        {
            _messBus = messBus;
            _baseRepo = baseRepo;
            _userManager = userManager;
        }

        /// <summary>
        /// Render report
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(DTTTGRenderInput command)
        {
            ValidateInput(command);
            var data = QueryData(command);
            //Nạp cấu hình thông tin báo cáo (header/footer/chữ ký) từ RptInfoConfig
            await LoadRptInfoAsync(data);

            var rs = await _messBus.InvokeAsync<bool>(new SaveDataReportInput()
            {
                ReportDataId = command.ReportDataId,
                Data = data
            });

            var report = await _messBus.InvokeAsync<FileStreamResult>(new RenderReportInput()
            {
                Code = reportCode,
                Data = data
            });
            return report;
        }

        /// <summary>
        /// Export report
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(DTTTGExportInput command)
        {
            ValidateInput(command);
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = JsonConvert.DeserializeObject<DTTTGReportOutput>(dataStr);
            if (string.IsNullOrEmpty(dataStr))
                data = QueryData(command);

            await LoadRptInfoAsync(data);

            var report = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
            {
                Code = reportCode,
                Data = data,
                Format = command.Format,
                FileName = $"Report_{DateTime.Now.ToString("yyyyMMddHHmmss")}",
            });
            return report;
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của tham số ngày
        /// </summary>
        private static void ValidateInput(DTTTGRenderInput command)
        {
            if (command.NgayBatDau > command.NgayKetThuc)
            {
                throw Oops.Oh(TcsManager.Message.EndDateMustBeGreaterThanStartDate);
            }
        }

        /// <summary>
        /// Query + build dữ liệu báo cáo
        /// </summary>
        private DTTTGReportOutput QueryData(DTTTGRenderInput command)
        {
            ValidateInput(command);
            var output = new DTTTGReportOutput();

            var tuNgay = BaseUtil.TimeServer.GetTimeServer(command.NgayBatDau);
            var denNgay = BaseUtil.TimeServer.GetTimeServer(command.NgayKetThuc);
            var station = string.IsNullOrWhiteSpace(command.Tram) ? null : command.Tram.Trim();

            var list = _baseRepo.Context.Ado.UseStoredProcedure()
                .SqlQuery<DTTTGQueryOutput>(Its015Const.SqlStoreConst.DoanhThuTheoThoiGian,
                    new SugarParameter("@NgayBatDau", tuNgay),
                    new SugarParameter("@NgayKetThuc", denNgay),
                    new SugarParameter("@Station", (object?)station ?? DBNull.Value))
                .Where(x => x.MenhGia > 0)
                .ToList();

            foreach (var item in list)
            {
                output.ChiTiet.Add(new DTTTGData
                {
                    ThuTuNhom = item.TenNhom?.StartsWith("1") == true ? 1 : item.TenNhom?.StartsWith("2") == true ? 2 : item.TenNhom?.StartsWith("3") == true ? 3 : 4,
                    TenNhom = item.TenNhom,
                    LoaiVe = item.LoaiXe,
                    MenhGia = item.MenhGia,
                    LoTrinh = item.LoTrinh,
                    MTC_SLVe = item.SoLuong,
                    MTC_ThanhTien = item.ThanhTien,
                    ETC_HC_SLVe = item.SoLuong_DieuChinh,
                    ETC_HC_ThanhTien = item.ThanhTien_DieuChinh,
                    TongHop_SLVe = item.SoLuong_Tong,
                    TongHop_ThanhTien = item.ThanhTien_Tong
                });
            }

            output.Cong = new DTTTGCong
            {
                MTC_SLVe = output.ChiTiet.Sum(x => x.MTC_SLVe),
                MTC_ThanhTien = output.ChiTiet.Sum(x => x.MTC_ThanhTien),
                ETC_HC_SLVe = output.ChiTiet.Sum(x => x.ETC_HC_SLVe),
                ETC_HC_ThanhTien = output.ChiTiet.Sum(x => x.ETC_HC_ThanhTien),
                TongHop_SLVe = output.ChiTiet.Sum(x => x.TongHop_SLVe),
                TongHop_ThanhTien = output.ChiTiet.Sum(x => x.TongHop_ThanhTien)
            };

            output.Parameter = new DTTTGParameter
            {
                Hinh = Its015InFoConst.Parameter.Hinh,
                Tram = _baseRepo.Context.GetTenTram(station),
                NgayBatDau = command.NgayBatDau.ToString("HH:mm:ss dd/MM/yyyy"),
                NgayKetThuc = command.NgayKetThuc.ToString("HH:mm:ss dd/MM/yyyy"),
                NgayHienTai = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"),
                NhanVienBaoCao = _userManager?.Account, // Người xuất báo cáo (runtime), không phải cấu hình
            };
            return output;
        }

        /// <summary>
        /// Nạp cấu hình thông tin báo cáo động từ RptInfoConfig vào node RptInfo.
        /// Cấu hình dùng chung ('*') hợp với cấu hình riêng của mã báo cáo hiện tại.
        /// </summary>
        private async Task LoadRptInfoAsync(DTTTGReportOutput? data)
        {
            if (data == null)
                return;

            var rptInfo = await _messBus.InvokeAsync<RptInfoConfigDataOutput>(new RptInfoConfigDataInput()
            {
                ReportCode = reportCode
            });

            data.RptInfo = rptInfo ?? new RptInfoConfigDataOutput();
        }
    }
}
