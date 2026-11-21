using Wolverine;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.Render;
using Modules.Mail.Core.Dto.Smtp;
using Modules.Mail.Core.Entities;
using Modules.Mail.Core.Exceptions;
using Modules.Mail.Core.Helper;
using Modules.Report.Base.Core.Dto;

namespace Modules.Mail.Infrastructure.Services.Mail.Report
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Kết xuất một báo cáo đính kèm thành tệp để gửi mail.
    ///                1. BuildInput: chọn phân hệ theo RptEmailReport.PluginCode → {Phân hệ}Reports.Build dựng DTO export
    ///                   theo mã báo cáo (switch theo mã nằm trong từng {Phân hệ}Reports).
    ///                2. Phần chung: ReportDataId, gọi MessBus, hứng stream, chặn tệp rỗng, đặt tên tệp.
    ///
    ///              Thêm phân hệ mới (WP, FMS...):
    ///                1. Tham chiếu {Phân hệ}.Core trong Modules.Mail.csproj
    ///                2. Tạo Infrastructure/Services/Mail/Report/{Phân hệ}Reports.cs (List + Build) — mẫu: TollReports
    ///                3. Thêm 1 nhánh vào BuildInput dưới đây
    ///                4. Thêm ..{Phân hệ}Reports.List vào RptReportCode.Catalog
    ///              Quên bước 3 hoặc 4 thì lúc khởi động API ghi log lỗi (FindUnbuildable), không đợi tới giờ gửi.
    /// Created date: 17/08/2026
    /// </summary>
    public class ReportExporter : IScoped
    {
        private readonly IMessageBus _bus;

        public ReportExporter(IMessageBus bus)
        {
            _bus = bus;
        }

        /// <summary>
        /// Kết xuất báo cáo, trả về tệp đính kèm đã sẵn sàng.
        /// Ném lỗi nếu phân hệ / mã báo cáo chưa hỗ trợ hoặc tệp kết xuất ra rỗng.
        /// </summary>
        /// <param name="report">Dòng danh mục RptEmailReport của báo cáo — lấy PluginCode.</param>
        public async Task<MailAttachment> RenderAsync(MailReport report, MailScheduleItem item, MailRptPeriod period, CancellationToken ct = default)
        {
            var code = item.ReportCode.Trim();
            var format = NormalizeFormat(item.Format);

            var input = BuildInput(report.PluginCode, code, new RptExportContext(period, format))
                ?? throw Oops.Oh(BaseMsg.Mail.Exception.ReportNotSupported, code);

            // Id phiên dữ liệu sinh mới mỗi lần để KHÔNG đụng bộ đệm của phiên xem báo cáo trên giao diện —
            // job phải luôn truy vấn số liệu tươi.
            input.ReportDataId = $"MAIL_{Guid.NewGuid():N}";

            // Wolverine định tuyến theo kiểu thực tế của input (LLTNExportInput...), như controller đang gọi
            var file = await _bus.InvokeAsync<FileStreamResult>(input, ct);

            var bytes = await ReadAllAsync(file, ct);

            // Tệp rỗng thường do kỳ chưa có dữ liệu. Gửi đi thì người nhận mở ra trang trắng và tưởng doanh thu
            // bằng 0 — thà báo lỗi vào log để người vận hành biết.
            if (bytes.Length == 0)
                throw Oops.Oh(BaseMsg.Mail.Exception.EmptyReportFile);

            return new MailAttachment(BuildFileName(code, period, format), bytes);
        }

        /// <summary>DTO export của một báo cáo; null = phân hệ / mã chưa có nhánh kết xuất.</summary>
        public static BaseReportInput? BuildInput(string? pluginCode, string reportCode, RptExportContext ctx)
            => pluginCode?.Trim() switch
            {
                MailRptModule.Toll => TollReports.Build(reportCode, ctx),
                // RptModule.WP => WpReports.Build(reportCode, ctx),
                _ => null,
            };

        /// <summary>
        /// Các mã khai trong RptReportCode.Catalog mà BuildInput không dựng được (thiếu nhánh phân hệ hoặc thiếu nhánh
        /// mã trong {Phân hệ}Reports.Build). Gọi lúc khởi động API — xem RptEmailStartupService.
        /// </summary>
        public static List<string> FindUnbuildable()
        {
            var ctx = new RptExportContext(
                MailRptPeriodResolver.ResolvePeriod(MailRptPeriodType.Month, DateTime.Now), MailConst.FileFormat.Pdf);

            return RptReportCode.Catalog
                .Where(x => BuildInput(x.Module, x.Code, ctx) == null)
                .Select(x => $"{x.Module}/{x.Code}")
                .ToList();
        }

        /// <summary>
        /// Đổi mốc thời gian theo giờ máy chủ sang UTC trước khi gửi sang phân hệ báo cáo.
        ///
        /// PHẢI CÓ BƯỚC NÀY. Phân hệ báo cáo quy ước mọi DateTime đầu vào là UTC+0 (giao diện gửi lên như vậy) rồi
        /// tự đổi về giờ máy chủ. Job tính kỳ từ GetTimeServer() (Kind = Local); đưa thẳng sang thì ConvertTimeFromUtc
        /// ném lỗi, còn chỉ dán nhãn Utc thì khoảng thời gian lệch đúng bằng độ lệch múi giờ và lấy lố sang kỳ sau.
        /// SpecifyKind(Local) để kỳ ngày (Local) và kỳ tháng / quý / năm (Unspecified) cho cùng kết quả.
        /// </summary>
        public static DateTime ToUtc(DateTime serverTime)
            => DateTime.SpecifyKind(serverTime, DateTimeKind.Local).ToUniversalTime();

        /// <summary>
        /// Tên tệp đính kèm theo MÃ báo cáo rút gọn, không theo tên tiếng Việt: tên có dấu / dấu cách hay bị hộp thư
        /// cắt hoặc mã hoá sai. Tầng render đặt tên "Report_&lt;mốc&gt;" không nói được là báo cáo gì nên phải đặt lại.
        /// </summary>
        public static string BuildFileName(string reportCode, MailRptPeriod period, string format)
            => $"{RptReportCode.ToFileName(reportCode)}_{period.Label}.{format}";

        /// <summary>Chuẩn hoá định dạng, mặc định pdf khi khai sai.</summary>
        public static string NormalizeFormat(string? format)
        {
            var f = format?.Trim().ToLowerInvariant();

            return f is MailConst.FileFormat.Pdf
                     or MailConst.FileFormat.Excel
                     or MailConst.FileFormat.Word
                ? f
                : MailConst.FileFormat.Pdf;
        }

        /// <summary>
        /// Hứng toàn bộ FileStreamResult ra byte[]. Bắt buộc: thư có thể phải thử gửi lại nhiều lần mà Stream đọc
        /// một lần là hết, lần thử thứ hai sẽ đính kèm tệp rỗng.
        /// </summary>
        private static async Task<byte[]> ReadAllAsync(FileStreamResult? file, CancellationToken ct)
        {
            if (file?.FileStream == null) return Array.Empty<byte>();

            await using var stream = file.FileStream;

            if (stream.CanSeek) stream.Position = 0;

            if (stream is MemoryStream ms) return ms.ToArray();

            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }
    }

    /// <summary>
    /// Description: Tham số chung đưa vào {Phân hệ}Reports.Build để dựng DTO export: kỳ (đã đổi sang UTC) và định dạng.
    /// Created date: 03/10/2026
    /// </summary>
    public sealed class RptExportContext
    {
        public RptExportContext(MailRptPeriod period, string format)
        {
            Period = period;
            Format = format;
        }

        public MailRptPeriod Period { get; }

        /// <summary>Định dạng tệp đã chuẩn hoá (pdf / xlsx / docx).</summary>
        public string Format { get; }

        /// <summary>Ngày của kỳ ngày, đã đổi sang UTC.</summary>
        public DateTime Ngay => ReportExporter.ToUtc(Period.Ngay);

        /// <summary>Đầu / cuối khoảng ngày, đã đổi sang UTC.</summary>
        public DateTime From => ReportExporter.ToUtc(Period.From);
        public DateTime To => ReportExporter.ToUtc(Period.To);

        public int Thang => Period.Thang;
        public int Quy => Period.Quy;
        public int Nam => Period.Nam;
    }
}
