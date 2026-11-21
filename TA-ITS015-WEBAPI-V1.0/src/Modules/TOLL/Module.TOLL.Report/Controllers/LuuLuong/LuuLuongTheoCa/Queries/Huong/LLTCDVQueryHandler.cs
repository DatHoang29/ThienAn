using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoCa;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoCa.Huong;
using Module.TOLL.Report.Core.Helpers;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Report.Core.Dto.InfoConfig;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Report.Infrastructure;
using Shared.DTO.Constants.Its015;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoCa.Queries.Huong
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý render/export báo cáo lưu lượng vé theo ca - hướng đầu vào
    /// Created date: 27/07/2026
    /// </summary>
    public class LLTCDVQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private const string reportCode = TollReportCode.LuuLuongTheoCaDauVao;
        private BaseRepository<TollTransactionIn> _baseRepo;
        UserManager _userManager;

        public LLTCDVQueryHandler(IMessageBus messBus, BaseRepository<TollTransactionIn> baseRepo, UserManager userManager)
        {
            _messBus = messBus;
            _baseRepo = baseRepo;
            _userManager = userManager;
        }

        /// <summary>
        /// Render report
        /// Author: HieuNV
        /// Date: 2026-07-27
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(LLTCDVRenderInput command)
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
        /// Author: HieuNV
        /// Date: 2026-07-27
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTCDVExportInput command)
        {
            //Xử lý dữ liệu query bla bla
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = JsonConvert.DeserializeObject<LLTCDVReportOutput>(dataStr);
            if (string.IsNullOrEmpty(dataStr))
            {
                data = QueryData(command);
            }
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
        /// Hàm xử lý QueryData
        /// Author: HieuNV
        /// Date: 2026-07-27
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private LLTCDVReportOutput? QueryData(LLTCRenderInput command)
        {
            var output = new LLTCDVReportOutput();

            var station = string.IsNullOrWhiteSpace(command.Tram) ? null : command.Tram.Trim();
            var direction = string.IsNullOrWhiteSpace(command.Huong) ? "" : command.Huong.Trim();
            var ngay = BaseUtil.TimeServer.GetTimeServer(command.Ngay)?.Date;
            var ca = string.IsNullOrWhiteSpace(command.Ca) ? "" : command.Ca.Trim();

            var processDateParam = new SugarParameter("@processdate", ngay);
            var stationParam = new SugarParameter("@station", (object?)station ?? DBNull.Value);
            var directionParam = new SugarParameter("@direction", direction);
            var shiftParam = new SugarParameter("@shift", ca);

            var list = _baseRepo.Context.Ado.UseStoredProcedure()
                .SqlQuery<LLTCQueryOutput>(Its015Const.SqlStoreConst.LuuLuongTheoCa, processDateParam, stationParam, directionParam, shiftParam)
                .ToList();

            int stt = 1;
            foreach (var item in list)
            {
                var dt = new LLTCData
                {
                    STT = stt++,
                    LoaiVe = item.TenLoaiXe,
                    VeLuot = item.LL_VL,
                    VeThang = item.LL_VT,
                    TongCong = item.LL_Tong
                };
                output.ChiTiet.Add(dt);
            }

            output.Cong = new LLTCCong
            {
                Nhan = "Tổng cộng",
                VeLuot = output.ChiTiet.Sum(x => x.VeLuot),
                VeThang = output.ChiTiet.Sum(x => x.VeThang),
                TongCong = output.ChiTiet.Sum(x => x.TongCong)
            };

            output.Parameter = new LLTCParameter
            {
                Ngay = ngay?.ToString("dd/MM/yyyy"),
                Tram = _baseRepo.Context.GetTenTram(station),
                Ca = _baseRepo.Context.GetTenCa(ca),
                Hinh = Its015InFoConst.Parameter.Hinh,
                NgayHienTai = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"),
                NhanVienBaoCao = _userManager?.Account, // Người xuất báo cáo (runtime), không phải cấu hình
            };
            return output;
        }

        /// <summary>
        /// Nạp cấu hình thông tin báo cáo động từ RptInfoConfig vào node RptInfo.
        /// Cấu hình dùng chung ('*') hợp với cấu hình riêng của mã báo cáo hiện tại.
        /// </summary>
        private async Task LoadRptInfoAsync(LLTCDVReportOutput? data)
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
