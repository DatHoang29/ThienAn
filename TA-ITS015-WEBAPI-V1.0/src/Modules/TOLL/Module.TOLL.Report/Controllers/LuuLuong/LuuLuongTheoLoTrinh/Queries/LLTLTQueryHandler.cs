using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoLoTrinh;
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

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoLoTrinh.Queries
{
    public class LLTLTQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private const string reportCode = TollReportCode.LuuLuongTheoLoTrinh;
        private BaseRepository<TollTransactionOut> _baseRepo;
        UserManager _userManager;
        public LLTLTQueryHandler(IMessageBus messBus, BaseRepository<TollTransactionOut> baseRepo, UserManager userManager)
        {
            _messBus = messBus;
            _baseRepo = baseRepo;
            _userManager = userManager;
        }
        /// <summary>
        /// Render report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTLTRenderInput command)
        {
            ValidateInput(command);
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
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTLTExportInput command)
        {
            ValidateInput(command);
            //Xử lý dữ liệu query bla bla
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = JsonConvert.DeserializeObject<LLTLTReportOutput>(dataStr);
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
        /// Kiểm tra tính hợp lệ của tham số ngày
        /// </summary>
        private static void ValidateInput(LLTLTRenderInput command)
        {
            if (command.NgayBatDau > command.NgayKetThuc)
            {
                throw Oops.Oh(TcsManager.Message.EndDateMustBeGreaterThanStartDate);
            }
        }

        private LLTLTReportOutput? QueryData(LLTLTRenderInput command)
        {
            ValidateInput(command);
            var output = new LLTLTReportOutput();

            var station = string.IsNullOrWhiteSpace(command.Tram) ? null : command.Tram.Trim();
            var direction = string.IsNullOrWhiteSpace(command.Huong) ? "" : command.Huong.Trim();
            var ngayBatDau = BaseUtil.TimeServer.GetTimeServer(command.NgayBatDau);
            var ngayKetThuc = BaseUtil.TimeServer.GetTimeServer(command.NgayKetThuc);

            var startDay = new SugarParameter("@startDate", ngayBatDau);
            var endDay = new SugarParameter("@endDate", ngayKetThuc);
            var directionParam = new SugarParameter("@direction", direction);
            var stations = new SugarParameter("@station", (object?)station ?? DBNull.Value);

            var list = _baseRepo.Context.Ado.UseStoredProcedure().SqlQuery<LLTLTQueryOutput>(Its015Const.SqlStoreConst.LuuLuongTheoLoTrinh, startDay, endDay, directionParam, stations).ToList();

            int stt = 1;
            foreach (var item in list)
            {
                var dt = new LLTLTData
                {
                    STT = stt++,
                    LoTrinh = item.TenLoTrinh,
                    LoaiXe = item.TenLoaiXe,
                    LuuLuong = item.LuuLuong,
                };
                output.ChiTiet.Add(dt);
            }

            output.Cong = new LLTLTCong
            {
                LuuLuong = output.ChiTiet.Sum(x => x.LuuLuong),
            };

            output.Parameter = new LLTLTParameter
            {
                NgayBatDau = command.NgayBatDau.ToString("HH:mm:ss dd/MM/yyyy"),
                NgayKetThuc = command.NgayKetThuc.ToString("HH:mm:ss dd/MM/yyyy"),
                Tram = _baseRepo.Context.GetTenTram(station),
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
        private async Task LoadRptInfoAsync(LLTLTReportOutput? data)
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
