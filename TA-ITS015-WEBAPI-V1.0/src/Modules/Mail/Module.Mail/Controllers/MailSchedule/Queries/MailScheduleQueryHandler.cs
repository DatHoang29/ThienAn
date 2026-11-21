using Mapster;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using SqlSugar;
using Wolverine;
using Modules.Mail.Core.Dto.EmailSchedule;
using Modules.Mail.Core.Dto.Render;
using Modules.Mail.Core.Entities;
using Modules.Mail.Core.Helper;
using Modules.Mail.Infrastructure;

using Modules.Mail.Infrastructure.Services.Mail.Report;
using Modules.Mail.Infrastructure.Services.Mail.Templates;

namespace Modules.Mail.Controllers.EmailSchedule.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu lịch gửi báo cáo qua email.
    ///
    ///              Lưới không trả cả danh sách báo cáo đính kèm — chỉ trả số lượng và bản tóm tắt
    ///              tên báo cáo. GetById mới trả đầy đủ Items để dialog dựng lại bảng động.
    /// Created date: 14/08/2026
    /// </summary>
    public class RptEmailScheduleQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<MailSchedule> _scheduleRep;
        private readonly BaseRepository<MailScheduleItem> _itemRep;
        private readonly BaseCacheService _cache;
        private readonly BaseRepository<MailAddress> _addressRep;
        private readonly RptEmailReportService _reportService;
        private readonly MailTemplateStore _templates;

        /// <summary>Số gợi ý tối đa trả về một lần.</summary>
        private const int MaxAddressSuggestions = 500;

        public RptEmailScheduleQueryHandler(
            BaseRepository<MailSchedule> scheduleRep,
            BaseRepository<MailScheduleItem> itemRep,
            BaseRepository<MailAddress> addressRep,
            BaseCacheService cache,
            RptEmailReportService reportService,
            MailTemplateStore templates)
        {
            _scheduleRep = scheduleRep;
            _itemRep = itemRep;
            _addressRep = addressRep;
            _cache = cache;
            _reportService = reportService;
            _templates = templates;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Gợi ý địa chỉ email cho ô To / CC / BCC và ô lọc người nhận ở nhật ký — lấy từ sổ địa chỉ
        ///              MailAddress (tự thêm mỗi khi lưu lịch). Trả danh sách địa chỉ đã sắp xếp, không trùng.
        /// Created date: 05/10/2026
        /// </summary>
        public async Task<List<string>> HandleAsync(MailAddressInput command)
        {
            var keyword = command.Keyword?.Trim().ToLowerInvariant();

            var list = await _addressRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrEmpty(keyword), u => u.Email.Contains(keyword!))
                .OrderBy(u => u.Email)
                .Select(u => u.Email)
                .Take(MaxAddressSuggestions)
                .ToListAsync();

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách lịch gửi báo cáo theo phân trang
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<SqlSugarPagedList<RptPageEmailScheduleOutput>> HandleAsync(RptPageEmailScheduleInput command)
        {
            // Lọc theo báo cáo phải tra sang bảng item: bảng lịch không còn cột ReportCode.
            // Lấy trước danh sách ScheduleId khớp để câu truy vấn chính không phải join.
            List<string>? scheduleIdsByReport = null;
            if (!string.IsNullOrWhiteSpace(command.ReportCode))
            {
                scheduleIdsByReport = await _itemRep.AsQueryable()
                    .Where(u => u.IsDelete == null && u.ReportCode == command.ReportCode)
                    .Select(u => u.ScheduleId)
                    .Distinct()
                    .ToListAsync();

                // Không có lịch nào dùng báo cáo này thì trả rỗng luôn, khỏi chạy câu chính.
                // Chèn giá trị không thể trùng để SqlSugar không sinh ra "IN ()".
                if (scheduleIdsByReport.Count == 0) scheduleIdsByReport.Add("__rpt_no_match__");
            }

            var pagedList = await _scheduleRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Frequency), u => u.Frequency == command.Frequency)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Recipient), u => u.Recipients.Contains(command.Recipient!)
                    || (u.Cc != null && u.Cc.Contains(command.Recipient!))
                    || (u.Bcc != null && u.Bcc.Contains(command.Recipient!)))
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .WhereIF(command.LastStatus.HasValue, u => u.LastStatus == command.LastStatus)
                .WhereIF(scheduleIdsByReport != null, u => scheduleIdsByReport!.Contains(u.ID))
                .Select(u => new RptPageEmailScheduleOutput() { }, true)
                // Lịch mới tạo lên đầu khi client không gửi tiêu chí sắp xếp
                .OrderByIF(string.IsNullOrWhiteSpace(command.Field), u => u.CreateTime, OrderByType.Desc)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            var records = pagedList.Records?.ToList() ?? new List<RptPageEmailScheduleOutput>();
            if (records.Count > 0)
            {
                // Nạp item của đúng các lịch trên trang hiện tại — một câu truy vấn, không N+1
                var pageIds = records.Select(u => u.ID).ToList();
                var items = await _itemRep.AsQueryable()
                    .Where(u => u.IsDelete == null && pageIds.Contains(u.ScheduleId))
                    .OrderBy(u => u.OrderNo)
                    .ToListAsync();

                var grouped = items.GroupBy(u => u.ScheduleId).ToDictionary(g => g.Key, g => g.ToList());
                var catalog = await _reportService.LoadAsync();

                foreach (var row in records)
                {
                    grouped.TryGetValue(row.ID, out var rowItems);
                    row.ItemCount = rowItems?.Count ?? 0;
                    row.ReportSummary = rowItems == null
                        ? null
                        : string.Join(", ", rowItems.Select(u => catalog.Name(u.ReportCode)));
                    row.ScheduleText = DescribeSchedule(row);
                }
            }

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách lịch gửi báo cáo theo tiêu chí lọc (không phân trang)
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<List<RptEmailScheduleOutput>> HandleAsync(RptEmailScheduleInput command)
        {
            var query = _scheduleRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Frequency), u => u.Frequency == command.Frequency)
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status);

            var list = await query
                .Select(u => new RptEmailScheduleOutput() { }, true)
                .OrderBy(u => u.CreateTime)
                .ToListAsync();

            if (list.Count == 0) return list;

            var ids = list.Select(u => u.ID).ToList();
            var items = await _itemRep.AsQueryable()
                .Where(u => u.IsDelete == null && ids.Contains(u.ScheduleId))
                .OrderBy(u => u.OrderNo)
                .ToListAsync();

            var grouped = items.GroupBy(u => u.ScheduleId).ToDictionary(g => g.Key, g => g.ToList());
            var catalog = await _reportService.LoadAsync();

            foreach (var row in list)
            {
                grouped.TryGetValue(row.ID, out var rowItems);
                FillItems(row, rowItems ?? new List<MailScheduleItem>(), catalog);
            }

            return list;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 lịch gửi báo cáo theo ID, kèm danh sách báo cáo đính kèm
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<RptEmailScheduleOutput> HandleAsync(MailIdScheduleInput command)
        {
            var entity = await _scheduleRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var output = entity.Adapt<RptEmailScheduleOutput>();

            var items = await _itemRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ScheduleId == entity.ID)
                .OrderBy(u => u.OrderNo)
                .ToListAsync();

            FillItems(output, items, await _reportService.LoadAsync());

            // Nội dung thư nạp vào wangEditor ở form sửa lịch
            output.Content = _templates.Read(entity.ContentPath);

            return output;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Gắn danh sách báo cáo và các trường hiển thị vào bản ghi trả về
        /// Created date: 14/08/2026
        /// </summary>
        private static void FillItems(RptEmailScheduleOutput output, List<MailScheduleItem> items,
            RptReportCatalog catalog)
        {
            output.Items = items.Select(u =>
            {
                var dto = u.Adapt<RptEmailScheduleItemOutput>();
                dto.ReportName = catalog.Name(u.ReportCode);
                return dto;
            }).ToList();

            output.ItemCount = output.Items.Count;
            output.ReportSummary = string.Join(", ", output.Items.Select(u => u.ReportName));
            output.ScheduleText = DescribeSchedule(output);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Diễn giải lịch chạy thành câu tiếng Việt
        /// Created date: 14/08/2026
        /// </summary>
        private static string DescribeSchedule(MailSchedule schedule)
            => RptScheduleTime.Describe(MailRptScheduleRule.From(schedule));
    }
}
