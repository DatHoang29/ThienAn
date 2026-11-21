using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoNgay;
using Module.TOLL.Report.Core.Helpers;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Report.Core.Dto.InfoConfig;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Report.Infrastructure;
using Shared.DTO.Constants.Its015;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.TOLL.Report.Controllers.DoanhThu.DoanhThuTheoNgay.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý render/export báo cáo doanh thu theo ngày (DTTN)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTNQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private const string reportCode = TollReportCode.DoanhThuTheoNgay;
        private BaseRepository<TollTicketOutSummary> _baseRepo;
        UserManager _userManager;

        public DTTNQueryHandler(IMessageBus messBus, BaseRepository<TollTicketOutSummary> baseRepo, UserManager userManager)
        {
            _messBus = messBus;
            _baseRepo = baseRepo;
            _userManager = userManager;
        }

        /// <summary>
        /// Render report
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(DTTNRenderInput command)
        {
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
        public async Task<FileStreamResult> HandleAsync(DTTNExportInput command)
        {
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = JsonConvert.DeserializeObject<DTTNReportOutput>(dataStr);
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
        /// Query + build dữ liệu báo cáo
        /// </summary>
        private DTTNReportOutput QueryData(DTTNRenderInput command)
        {
            var output = new DTTNReportOutput();

            var ngay = BaseUtil.TimeServer.GetTimeServer(command.Ngay)?.Date;
            var station = string.IsNullOrWhiteSpace(command.Tram) ? null : command.Tram.Trim();
            var direction = string.IsNullOrWhiteSpace(command.Huong) ? null : command.Huong.Trim();

            var list = _baseRepo.Context.Ado.UseStoredProcedure()
                .SqlQuery<DTTNQueryOutput>(Its015Const.SqlStoreConst.DoanhThuTheoNgay,
                    new SugarParameter("@processdate", ngay),
                    new SugarParameter("@station", (object?)station ?? DBNull.Value),
                    new SugarParameter("@direction", (object?)direction ?? DBNull.Value))
                .ToList();

            int stt = 1;
            foreach (var item in list)
            {
                output.ChiTiet.Add(new DTTNData
                {
                    STT = stt++,
                    LoaiVe = item.TenLoaiXe,
                    MenhGiaVe = item.MenhGia,
                    SoLuot = item.SoLuong,
                    ThanhTien = item.ThanhTien
                });
            }

            var tongThanhTien = output.ChiTiet.Sum(x => x.ThanhTien);
            output.Cong = new DTTNCong
            {
                SoLuot = output.ChiTiet.Sum(x => x.SoLuot),
                ThanhTien = tongThanhTien,
                TongDoanhThu = tongThanhTien,
                BangChu = MoneyHelper.DocSoThanhChu(tongThanhTien)
            };

            output.Parameter = new DTTNParameter
            {
                Hinh = Its015InFoConst.Parameter.Hinh,
                Tram = _baseRepo.Context.GetTenTram(station),
                Ngay = ngay?.ToString("dd/MM/yyyy"),
                NgayHienTai = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"),
                NhanVienBaoCao = _userManager?.Account, // Người xuất báo cáo (runtime), không phải cấu hình
            };
            return output;
        }

        /// <summary>
        /// Nạp cấu hình thông tin báo cáo động từ RptInfoConfig vào node RptInfo.
        /// Cấu hình dùng chung ('*') hợp với cấu hình riêng của mã báo cáo hiện tại.
        /// </summary>
        private async Task LoadRptInfoAsync(DTTNReportOutput? data)
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
