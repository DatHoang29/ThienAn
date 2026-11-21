using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoNam;
using Module.TOLL.Report.Core.Helpers;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Report.Core.Dto.InfoConfig;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Report.Infrastructure;
using Shared.DTO.Constants.Its015;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.TOLL.Report.Controllers.DoanhThu.DoanhThuTheoNam.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý render/export báo cáo doanh thu theo năm (DTTNam)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTNamQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private const string reportCode = TollReportCode.DoanhThuTheoNam;
        private BaseRepository<TollTicketOutSummary> _baseRepo;
        UserManager _userManager;

        public DTTNamQueryHandler(IMessageBus messBus, BaseRepository<TollTicketOutSummary> baseRepo, UserManager userManager)
        {
            _messBus = messBus;
            _baseRepo = baseRepo;
            _userManager = userManager;
        }

        /// <summary>
        /// Render report
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(DTTNamRenderInput command)
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
        public async Task<FileStreamResult> HandleAsync(DTTNamExportInput command)
        {
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = JsonConvert.DeserializeObject<DTTNamReportOutput>(dataStr);
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
        private DTTNamReportOutput QueryData(DTTNamRenderInput command)
        {
            var output = new DTTNamReportOutput();

            var station = string.IsNullOrWhiteSpace(command.Tram) ? null : command.Tram.Trim();
            var direction = string.IsNullOrWhiteSpace(command.Huong) ? null : command.Huong.Trim();

            var list = _baseRepo.Context.Ado.UseStoredProcedure()
                .SqlQuery<DTTNamQueryOutput>(Its015Const.SqlStoreConst.DoanhThuTheoNam,
                    new SugarParameter("@year", command.Nam),
                    new SugarParameter("@station", (object?)station ?? DBNull.Value),
                    new SugarParameter("@direction", (object?)direction ?? DBNull.Value))
                .ToList();

            int stt = 1;
            foreach (var item in list)
            {
                output.ChiTiet.Add(new DTTNamData
                {
                    STT = stt++,
                    Ngay = item.NgayThangNam,
                    L1_SL = item.LuuLuong_1, L1_TT = item.ThanhTien_1,
                    L2_SL = item.LuuLuong_2, L2_TT = item.ThanhTien_2,
                    L3_SL = item.LuuLuong_3, L3_TT = item.ThanhTien_3,
                    L4_SL = item.LuuLuong_4, L4_TT = item.ThanhTien_4,
                    L5_SL = item.LuuLuong_5, L5_TT = item.ThanhTien_5,
                    TongSL = item.LuuLuong_Tong, TongTT = item.ThanhTien_Tong
                });
            }

            var tongTT = output.ChiTiet.Sum(x => x.TongTT);
            output.Cong = new DTTNamCong
            {
                L1_SL = output.ChiTiet.Sum(x => x.L1_SL), L1_TT = output.ChiTiet.Sum(x => x.L1_TT),
                L2_SL = output.ChiTiet.Sum(x => x.L2_SL), L2_TT = output.ChiTiet.Sum(x => x.L2_TT),
                L3_SL = output.ChiTiet.Sum(x => x.L3_SL), L3_TT = output.ChiTiet.Sum(x => x.L3_TT),
                L4_SL = output.ChiTiet.Sum(x => x.L4_SL), L4_TT = output.ChiTiet.Sum(x => x.L4_TT),
                L5_SL = output.ChiTiet.Sum(x => x.L5_SL), L5_TT = output.ChiTiet.Sum(x => x.L5_TT),
                TongSL = output.ChiTiet.Sum(x => x.TongSL),
                TongTT = tongTT,
                TongDoanhThu = tongTT,
                BangChu = MoneyHelper.DocSoThanhChu(tongTT)
            };

            output.Parameter = new DTTNamParameter
            {
                Hinh = Its015InFoConst.Parameter.Hinh,
                Tram = _baseRepo.Context.GetTenTram(station),
                Nam = command.Nam.ToString(),
                NgayHienTai = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"),
                NhanVienBaoCao = _userManager?.Account, // Người xuất báo cáo (runtime), không phải cấu hình
            };
            return output;
        }

        /// <summary>
        /// Nạp cấu hình thông tin báo cáo động từ RptInfoConfig vào node RptInfo.
        /// Cấu hình dùng chung ('*') hợp với cấu hình riêng của mã báo cáo hiện tại.
        /// </summary>
        private async Task LoadRptInfoAsync(DTTNamReportOutput? data)
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
