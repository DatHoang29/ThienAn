using Modules.Report.Base.Controllers.Management.Dto;
using Modules.TMS.Report.Controllers.OverSpeed.Dto;
using Modules.TMS.Report.Infrastructure.Services.OverSpeed;
using Wolverine;

namespace Modules.TMS.Report.Controllers.OverSpeed.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Render/Export báo cáo phương tiện vượt quá tốc độ — cùng luồng với các report FastReport khác:
    /// render thì dựng dữ liệu, lưu cache theo <c>ReportDataId</c> rồi render; export thì ưu tiên dữ liệu cache để
    /// file tải về khớp đúng bản người dùng vừa xem.
    /// Created date: 16/10/2026
    /// </summary>
    public class OverSpeedQueryHandler : IWolverineHandler
    {
        private const string ReportCode = "OverSpeedReport";

        private readonly IMessageBus _messBus;
        private readonly TmsOverSpeedReportService _reportService;

        public OverSpeedQueryHandler(IMessageBus messBus, TmsOverSpeedReportService reportService)
        {
            _messBus = messBus;
            _reportService = reportService;
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Render báo cáo vượt tốc độ
        /// Created date: 16/10/2026
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(OverSpeedRenderInput command)
        {
            var data = await _reportService.BuildOverSpeedReport(command);

            await _messBus.InvokeAsync<bool>(new SaveDataReportInput()
            {
                ReportDataId = command.ReportDataId,
                Data = data
            });

            return await _messBus.InvokeAsync<FileStreamResult>(new RenderReportInput()
            {
                Code = ReportCode,
                Data = data
            });
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Export báo cáo vượt tốc độ
        /// Created date: 16/10/2026
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(OverSpeedExportInput command)
        {
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = !string.IsNullOrWhiteSpace(dataStr)
                ? JsonConvert.DeserializeObject<OverSpeedReport>(dataStr)
                : null;
            data ??= await _reportService.BuildOverSpeedReport(command);

            return await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
            {
                Code = ReportCode,
                Data = data,
                Format = command.Format ?? "pdf",
                FileName = $"OverSpeedReport_{DateTime.Now:yyyyMMddHHmmss}"
            });
        }
    }
}
