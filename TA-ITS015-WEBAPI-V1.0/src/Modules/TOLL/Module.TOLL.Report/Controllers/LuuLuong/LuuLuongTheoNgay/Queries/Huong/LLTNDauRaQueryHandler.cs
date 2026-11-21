using Module.TOLL.Core.Constants;
using Mapster;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay.Huong;
using Module.TOLL.Core.Enums;
using Module.TOLL.Report.Core.Helpers;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Report.Base.Infrastructure;
using Modules.Report.Core.Dto.InfoConfig;
using Modules.TOLL.Core.Entities;
using Shared.DTO.Constants.Its015;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoNgay.Queries.Huong
{
    public class LLTNDauRaQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private const string reportCode = TollReportCode.LuuLuongTheoNgayDauRa;
        private BaseRepository<TollTicketOutSummary> _baseRepo;
        UserManager _userManager;
        public LLTNDauRaQueryHandler(IMessageBus messBus, BaseRepository<TollTicketOutSummary> baseRepo, UserManager userManager)
        {
            _messBus = messBus;
            _baseRepo = baseRepo;
            _userManager = userManager;
        }
        /// <summary>
        /// Render report
        /// ChuongLP
        /// Date: 2026-07-24
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(LLTNDauRaRenderInput command)
        {
            //Xử lý dữ liệu query bla bla
            var data = QueryData(command);
            //Nạp cấu hình thông tin báo cáo (header/footer/chữ ký) từ RptInfoConfig
            await LoadRptInfoAsync(data);
            //Lưu dữ liệu
            var rs = await _messBus.InvokeAsync<bool>(new SaveDataReportInput()
            {
                ReportDataId = command.ReportDataId,
                Data = data
            });
            //Render report
            var report = await _messBus.InvokeAsync<FileStreamResult>(new RenderReportInput()
            {
                Code = reportCode,
                Data = data
            });
            return report;

        }

        /// <summary>
        /// Export report
        /// ChuongLP
        /// Date: 2026-07-24
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTNDauRaExportInput command)
        {
            //Lấy lại dữ liệu đã render (nếu có), chưa có thì query lại
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = string.IsNullOrEmpty(dataStr)
                ? QueryData(command)
                : JsonConvert.DeserializeObject<LLTNDauRaReportOutput>(dataStr);
            await LoadRptInfoAsync(data);
            var report = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
            {
                Code = reportCode,
                Data = data,
                Format = command.Format, //Lưu ý có format
                FileName = $"Report_{DateTime.Now.ToString("yyyyMMddHHmmss")}",
            });
            return report;

        }
        /// <summary>
        /// Nạp cấu hình thông tin báo cáo động từ RptInfoConfig vào node RptInfo.
        /// Cấu hình dùng chung ('*') hợp với cấu hình riêng của mã báo cáo hiện tại.
        /// ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        private async Task LoadRptInfoAsync(LLTNDauRaReportOutput? data)
        {
            if (data == null)
                return;

            var rptInfo = await _messBus.InvokeAsync<RptInfoConfigDataOutput>(new RptInfoConfigDataInput()
            {
                ReportCode = reportCode
            });

            data.RptInfo = rptInfo ?? new RptInfoConfigDataOutput();
        }

        /// <summary>
        /// ChuongLP
        /// Date: 2026-07-24
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private LLTNDauRaReportOutput? QueryData(LLTNRenderInput command)
        {
            var output = new LLTNDauRaReportOutput();
            var station = string.IsNullOrWhiteSpace(command.Tram) ? null : command.Tram.Trim();
            var direction = string.IsNullOrWhiteSpace(command.Huong) ? "" : command.Huong.Trim();
            var ngay = BaseUtil.TimeServer.GetTimeServer(command.Ngay)?.Date;

            var processDateParam = new SugarParameter("@processdate", ngay);
            var stationParam = new SugarParameter("@station", (object?)station ?? DBNull.Value);
            /* Báo cáo hướng ra cố định direction = 2, không lấy theo tham số FE gửi lên */
            var directionParam = new SugarParameter("@direction", TollEnums.DirectionCode.Out);

            var list = _baseRepo.Context.Ado.UseStoredProcedure().SqlQuery<LLTNQueryOutput>(Its015Const.SqlStoreConst.LuuLuongTheoNgayDauRa, processDateParam, stationParam, directionParam).ToList();

            int stt = 1;
            foreach (var item in list)
            {
                var dt = new LLTNData
                {
                    STT = stt++,
                    LoaiVe = item.TenLoaiXe,
                    VeLuot = item.LL_VL,
                    VeThang = item.LL_VT,
                    TongCong = item.LL_Tong
                };
                output.ChiTiet.Add(dt);
            }
            output.Cong = new LLTNCong
            {
                VeLuot = output.ChiTiet.Sum(x => x.VeLuot),
                VeThang = output.ChiTiet.Sum(x => x.VeThang),
                TongCong = output.ChiTiet.Sum(x => x.TongCong)
            };
            output.Parameter = new LLTNParameter
            {
                Ngay = ngay?.ToString("dd/MM/yyyy"),
                Tram = _baseRepo.Context.GetTenTram(station),
                Hinh = "https://tacorp.vn/wp-content/uploads/2020/06/LOGO-THIEN-AN-small.png",
                NgayHienTai = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"),
                NhanVienBaoCao = _userManager?.Account, // Người xuất báo cáo (runtime), không phải cấu hình
            };
            return output;
        }
    }
}
