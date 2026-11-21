using Module.TOLL.Core.Constants;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.EmailSchedule;
using Modules.Mail.Core.Dto.Render;
using Modules.Mail.Core.Entities;
using Modules.Mail.Core.Helper;

using Modules.Mail.Infrastructure.Services.Mail.Config;
using Modules.Mail.Infrastructure.Services.Mail.Report;
using Modules.Mail.Infrastructure.Services.Mail.Templates;

namespace Modules.Mail.Infrastructure.Services.Mail.Delivery
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Dựng thư xem trước cho giao diện — cùng đường tính kỳ và cùng MailBodyRenderer với job gửi, nên
    ///              thư xem trước và thư gửi đi giống nhau. Không kết xuất báo cáo, không gửi.
    /// Created date: 29/09/2026
    /// </summary>
    public class RptEmailPreviewService : IScoped
    {
        private readonly MailConfigService _configService;
        private readonly MailTemplateStore _templates;
        private readonly BaseRepository<MailTemplate> _templateRep;
        private readonly RptEmailReportService _reportService;
        private readonly MailInlineImageService _inlineImages;

        public RptEmailPreviewService(
            MailConfigService configService,
            MailTemplateStore templates,
            BaseRepository<MailTemplate> templateRep,
            RptEmailReportService reportService,
            MailInlineImageService inlineImages)
        {
            _configService = configService;
            _templates = templates;
            _templateRep = templateRep;
            _reportService = reportService;
            _inlineImages = inlineImages;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xem trước thư theo form lịch đang soạn. Kỳ dữ liệu tính theo LẦN CHẠY KẾ TIẾP để người dùng
        ///              thấy đúng thư sẽ nhận được lần tới. Nội dung lấy từ editor (Content); rỗng thì lấy nội dung mẫu.
        /// Created date: 29/09/2026
        /// </summary>
        public async Task<MailPreviewOutput> PreviewAsync(MailPreviewScheduleInput input)
        {
            var now = BaseUtil.TimeServer.GetTimeServer();
            var frequency = string.IsNullOrWhiteSpace(input.Frequency) ? MailRptPeriodType.Month : input.Frequency.Trim().ToLowerInvariant();
            var rule = MailRptScheduleRule.Of(frequency, input.DayOfMonth, input.MonthInPeriod, input.DaysOfWeek, input.HourOfDay, input.MinuteOfHour);
            var sendAt = RptScheduleTime.GetNextDueTime(rule, now) ?? now;

            // Tần suất lệch kỳ dữ liệu (VD chạy hàng ngày kèm báo cáo tháng) không sai nhưng dễ ngoài ý muốn
            var periodWarnings = new List<string>();

            var catalog = await _reportService.LoadAsync();

            var items = (input.Items ?? new List<MailScheduleItemInput>())
                .Where(x => catalog.IsActive(x.ReportCode))
                .ToList();

            MailRenderContext ctx;
            if (items.Count == 0)
            {
                ctx = BuildSampleContext(sendAt, catalog);
            }
            else
            {
                var lines = new List<MailReportLine>();

                var line = 0;
                foreach (var item in items)
                {
                    line++;
                    var code = item.ReportCode.Trim();
                    var reportName = catalog.Name(code);
                    var periodType = RptReportCode.ResolvePeriodType(code, frequency);
                    var period = MailRptPeriodResolver.ResolvePeriod(periodType, sendAt, item.PeriodOffset);

                    var mismatch = MailRptPeriodResolver.CheckFrequencyMatch(frequency, periodType, item.PeriodOffset);
                    if (mismatch != null)
                        periodWarnings.Add($"Dòng {line} ({reportName}): {mismatch}");

                    lines.Add(LineOf(catalog, code, period, ReportExporter.NormalizeFormat(item.Format)));
                }

                ctx = new MailRenderContext { Lines = lines, SentAt = sendAt };
            }

            ctx.ScheduleName = string.IsNullOrWhiteSpace(input.Name) ? ctx.ScheduleName : input.Name;

            var output = new MailPreviewOutput();
            var content = input.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                content = await LoadTemplateContentAsync(input.TemplateCode, output.Warnings);
                output.TemplateCode = input.TemplateCode?.Trim();
            }

            await RenderAsync(output, input.Subject, content, ctx);
            output.SendAt = sendAt;
            output.Warnings.AddRange(periodWarnings);

            AddTagWarnings(output.Warnings, input.Subject, "tiêu đề");
            AddTagWarnings(output.Warnings, content, "nội dung thư");

            return output;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thư mẫu của một mẫu với dữ liệu giả — ảnh thu nhỏ ở bước chọn mẫu.
        /// Created date: 29/09/2026
        /// </summary>
        public async Task<MailPreviewOutput> SampleAsync(string? code)
        {
            var now = BaseUtil.TimeServer.GetTimeServer();
            var ctx = BuildSampleContext(now, await _reportService.LoadAsync());

            var output = new MailPreviewOutput { TemplateCode = code?.Trim() };
            var content = await LoadTemplateContentAsync(code, output.Warnings);

            await RenderAsync(output, null, content, ctx);
            output.SendAt = now;
            return output;
        }

        /// <summary>Nội dung HTML của mẫu theo mã; không có thì ghi cảnh báo và trả null.</summary>
        private async Task<string?> LoadTemplateContentAsync(string? code, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var key = code.Trim();
            var template = await _templateRep.GetFirstAsync(u => u.Code == key && u.IsDelete == null);
            if (template == null)
            {
                warnings.Add($"Không có mẫu [{key}].");
                return null;
            }

            if (template.Status == BaseEnums.StatusEnum.Disable)
                warnings.Add($"Mẫu [{template.Name}] đang ngừng sử dụng.");

            var content = _templates.Read(template.FilePath);
            if (content == null)
                warnings.Add($"Không tìm thấy file nội dung của mẫu [{template.Name}].");

            return content;
        }

        private async Task RenderAsync(MailPreviewOutput output, string? subjectTemplate, string? content, MailRenderContext ctx)
        {
            var subject = MailBodyRenderer.RenderSubject(subjectTemplate, ctx);
            var prefix = (await _configService.GetAsync()).SubjectPrefix?.Trim();

            output.Subject = string.IsNullOrEmpty(prefix) ? subject : $"{prefix} {subject}";
            // Trình duyệt không hiểu cid: — đổi ảnh nội tuyến khai ở Mail:InlineImages sang data: cho bản xem trước.
            // Ảnh chèn trong editor là link http của server nên hiện được luôn.
            output.Html = _inlineImages.ToDataUri(MailBodyRenderer.RenderBody(content, ctx));
        }

        /// <summary>Dữ liệu giả: hai báo cáo tháng, lấy giá trị mẫu từ danh mục tag.</summary>
        private static MailRenderContext BuildSampleContext(DateTime sendAt, RptReportCatalog catalog)
        {
            var period = MailRptPeriodResolver.ResolvePeriod(MailRptPeriodType.Month, sendAt);

            return new MailRenderContext
            {
                ScheduleName = MailTag.Find(MailConst.Placeholder.TenLich)?.Sample,
                Lines = new List<MailReportLine>
                {
                    LineOf(catalog, TollReportCode.LuuLuongTheoThangDauVao, period, MailConst.FileFormat.Pdf),
                    LineOf(catalog, TollReportCode.DoanhThuTheoThang, period, MailConst.FileFormat.Excel),
                },
                SentAt = sendAt,
            };
        }

        /// <summary>Một dòng báo cáo với tên tệp dự kiến (bản xem trước không kết xuất thật).</summary>
        private static MailReportLine LineOf(RptReportCatalog catalog, string code, MailRptPeriod period, string format) => new()
        {
            ReportName = catalog.Name(code),
            GroupName = catalog.Group(code),
            PeriodText = period.Text,
            FileName = ReportExporter.BuildFileName(code, period, format),
        };

        /// <summary>Cảnh báo (không chặn) khi có tag không thuộc danh mục — người nhận sẽ thấy nguyên chữ tag.</summary>
        private static void AddTagWarnings(List<string> warnings, string? text, string where)
        {
            var unknown = MailTag.FindUnknown(text);
            if (unknown.Count > 0)
                warnings.Add($"Tag không hỗ trợ trong {where}: {string.Join(", ", unknown)}");
        }
    }
}
