using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay.Huong;
using Module.TOLL.Report.Core.Helpers;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Report.Core.Dto.InfoConfig;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Report.Infrastructure;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoNgay.Queries.Huong
{
    public class LLTNDauVaoQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private const string reportCode = TollReportCode.LuuLuongTheoNgayDauVao;
        /// <summary>Hướng vào theo quy ước TollTicketOutSummary.Direction (1 = vào, 2 = ra)</summary>
        private const string DirectionIn = "1";
        private BaseRepository<TollTransactionIn> _baseRepo;
        private UserManager _userManager;
        public LLTNDauVaoQueryHandler(IMessageBus messBus, UserManager userManager, BaseRepository<TollTransactionIn> baseRepo)
        {
            _messBus = messBus;
            _userManager = userManager;
            _baseRepo = baseRepo;
        }
        /// <summary>
        /// Render report
        /// Author: ChuongLP
        /// Date: 2026-07-24
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(LLTNDauVaoRenderInput command)
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
        /// Author: ChuongLP
        /// Date: 2026-07-24
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(LLTNDauVaoExportInput command)
        {
            //Xử lý dữ liệu query bla bla
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = string.IsNullOrEmpty(dataStr)
                ? QueryData(command)
                : JsonConvert.DeserializeObject<LLTNDauVaoReportOutput>(dataStr);
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
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        private async Task LoadRptInfoAsync(LLTNDauVaoReportOutput? data)
        {
            if (data == null)
                return;

            var rptInfo = await _messBus.InvokeAsync<RptInfoConfigDataOutput>(new RptInfoConfigDataInput()
            {
                ReportCode = reportCode
            });

            /* Toàn bộ thông tin cấu hình (header/footer/signature) nằm ở node RptInfo.
               File .frx bind thẳng vào RptInfo, không dùng giá trị tĩnh nữa. */
            data.RptInfo = rptInfo ?? new RptInfoConfigDataOutput();
        }

        /// <summary>
        /// Hàm xử lý QueryData
        /// Author: ChuongLP
        /// Date: 2026-07-24
        /// </summary>
        private LLTNDauVaoReportOutput? QueryData(LLTNRenderInput command)
        {
            var output = new LLTNDauVaoReportOutput();
            /* Trạm rỗng = lấy tất cả trạm -> phải truyền DBNull.
               Truyền "" sẽ khiến store so sánh StationId = '' và không khớp dòng nào. */
            var station = string.IsNullOrWhiteSpace(command.Tram) ? null : command.Tram.Trim();
            var ngay = BaseUtil.TimeServer.GetTimeServer(command.Ngay)?.Date;

            var processDateParam = new SugarParameter("@processdate", ngay);
            var stationParam = new SugarParameter("@station", (object?)station ?? DBNull.Value);
            /* Báo cáo hướng vào cố định direction = 1, không lấy theo tham số FE gửi lên */
            var directionParam = new SugarParameter("@direction", DirectionIn);

            var list = _baseRepo.Context.Ado.UseStoredProcedure().SqlQuery<LLTNQueryOutput>(Shared.DTO.Constants.Its015.Its015Const.SqlStoreConst.LuuLuongTheoNgayDauVao, processDateParam, stationParam, directionParam).ToList();
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
            /* Chỉ giữ các field runtime (phụ thuộc dữ liệu truy vấn), không set giá trị cấu hình tĩnh.
               Đơn vị/logo/địa danh/chữ ký... lấy từ RptInfo (DB) qua LoadRptInfoAsync. */
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
