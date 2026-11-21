using Module.TOLL.Core.Constants;
using Module.TOLL.Report.Core.Helpers;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Report.Core.Dto.InfoConfig;
using Modules.Report.Base.Infrastructure;
using Modules.TOLL.Core.Entities;
using Shared.DTO.Constants.Its015;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;
using Mapster;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoThang;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoThang.Huong;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoThang.Queries.Huong
{
    public class LLTTDVQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private const string reportCode = TollReportCode.LuuLuongTheoThangDauVao;
        private BaseRepository<TollTransactionOut> _baseRepo;
        UserManager _userManager;
        public LLTTDVQueryHandler(IMessageBus messBus, BaseRepository<TollTransactionOut> baseRepo, UserManager userManager)
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
        public async Task<FileStreamResult> HandleAsync(LLTTDVRenderInput command)
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
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTTDVExportInput command)
        {
            //Xử lý dữ liệu query bla bla
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = JsonConvert.DeserializeObject<LLTTDVReportOutput>(dataStr);
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
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private LLTTDVReportOutput? QueryData(LLTTRenderInput command)
        {
            var output = new LLTTDVReportOutput();

            var station = string.IsNullOrWhiteSpace(command.Tram) ? null : command.Tram.Trim();
            var direction = string.IsNullOrWhiteSpace(command.Huong) ? "" : command.Huong.Trim();
            var nam = (command.Nam);
            var thang = command.Thang;

            var stationParam = new SugarParameter("@station", (object?)station ?? DBNull.Value);
            var directionParam = new SugarParameter("@direction", direction);
            var year = new SugarParameter("@year", nam);
            var month = new SugarParameter("@month", thang);

            var list = _baseRepo.Context.Ado.UseStoredProcedure().SqlQuery<LLTTQueryOutput>(Its015Const.SqlStoreConst.LuuLuongTheoThang, stationParam, directionParam, year, month).ToList();

            foreach (var item in list)
            {
                var dt = new LLTTDVData
                {
                    Ngay = item.NgayThangNam,
                    L1_VL = item.LuuLuong_VeLuot_1 ?? 0,
                    L1_TQ = item.LuuLuong_VeTQ_1 ?? 0,
                    L2_VL = item.LuuLuong_VeLuot_2 ?? 0,
                    L2_TQ = item.LuuLuong_VeTQ_2 ?? 0,
                    L3_VL = item.LuuLuong_VeLuot_3 ?? 0,
                    L3_TQ = item.LuuLuong_VeTQ_3 ?? 0,
                    L4_VL = item.LuuLuong_VeLuot_4 ?? 0,
                    L4_TQ = item.LuuLuong_VeTQ_4 ?? 0,
                    L5_VL = item.LuuLuong_VeLuot_5 ?? 0,
                    L5_TQ = item.LuuLuong_VeTQ_5 ?? 0,
                    TongVeLuot = item.LL_Tong_VL ?? 0,
                    TongTQ = item.LL_Tong_VTQ ?? 0,
                    PhiDBTQ = item.BTC ?? 0,
                    UT_MotXe = item.UT_1X ?? 0,
                    UT_DoanXe = item.UT_DX ?? 0,
                    SC_MotXe = item.SC_1X ?? 0,
                    SC_DoanXe = item.SC_DX ?? 0
                };

                output.ChiTiet.Add(dt);
            }
            output.Cong = new LLTTCong
            {
                TongXeQuaTram = output.ChiTiet.Sum(x => x.TongVeLuot + x.TongTQ + (int)x.PhiDBTQ + x.UT_MotXe + x.UT_DoanXe + x.SC_MotXe + x.SC_DoanXe),
                L1_VL = output.ChiTiet.Sum(x => x.L1_VL),
                L1_TQ = output.ChiTiet.Sum(x => x.L1_TQ),
                L2_VL = output.ChiTiet.Sum(x => x.L2_VL),
                L2_TQ = output.ChiTiet.Sum(x => x.L2_TQ),
                L3_VL = output.ChiTiet.Sum(x => x.L3_VL),
                L3_TQ = output.ChiTiet.Sum(x => x.L3_TQ),
                L4_VL = output.ChiTiet.Sum(x => x.L4_VL),
                L4_TQ = output.ChiTiet.Sum(x => x.L4_TQ),
                L5_VL = output.ChiTiet.Sum(x => x.L5_VL),
                L5_TQ = output.ChiTiet.Sum(x => x.L5_TQ),
                TongVeLuot = output.ChiTiet.Sum(x => x.TongVeLuot),
                TongTQ = output.ChiTiet.Sum(x => x.TongTQ),
                PhiDBTQ = output.ChiTiet.Sum(x => x.PhiDBTQ),
                UT_MotXe = output.ChiTiet.Sum(x => x.UT_MotXe),
                UT_DoanXe = output.ChiTiet.Sum(x => x.UT_DoanXe),

                SC_MotXe = output.ChiTiet.Sum(x => x.SC_MotXe),
                SC_DoanXe = output.ChiTiet.Sum(x => x.SC_DoanXe)
            };

            output.Parameter = new LLTTDVParameter
            {
                Nam = command.Nam,
                Tram = _baseRepo.Context.GetTenTram(station),
                Hinh = Its015InFoConst.Parameter.Hinh,
                NgayHienTai = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"),
                TuNgay = list.FirstOrDefault()?.NgayThangNam ?? "",
                DenNgay = list.LastOrDefault()?.NgayThangNam ?? "",
                Thang = command.Thang,
                NhanVienBaoCao = _userManager?.Account, // Người xuất báo cáo (runtime), không phải cấu hình
            };
            return output;
        }

        /// <summary>
        /// Nạp cấu hình thông tin báo cáo động từ RptInfoConfig vào node RptInfo.
        /// Cấu hình dùng chung ('*') hợp với cấu hình riêng của mã báo cáo hiện tại.
        /// </summary>
        private async Task LoadRptInfoAsync(LLTTDVReportOutput? data)
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
