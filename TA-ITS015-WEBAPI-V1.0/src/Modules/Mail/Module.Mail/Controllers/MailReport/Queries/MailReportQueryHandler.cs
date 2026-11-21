using Mapster;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using SqlSugar;
using Wolverine;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.EmailReport;
using Modules.Mail.Core.Entities;
using Modules.Mail.Infrastructure;
using Modules.Mail.Infrastructure.Services.Mail.Report;

namespace Modules.Mail.Controllers.EmailReport.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn danh mục báo cáo gửi email.
    /// Created date: 01/10/2026
    /// </summary>
    public class RptEmailReportQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<MailReport> _reportRep;

        public RptEmailReportQueryHandler(BaseRepository<MailReport> reportRep)
        {
            _reportRep = reportRep;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh mục báo cáo theo phân trang
        /// Created date: 01/10/2026
        /// </summary>
        public async Task<SqlSugarPagedList<MailPageReportOutput>> HandleAsync(MailRptPageReportInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            return await _reportRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.PluginCode), u => u.PluginCode == command.PluginCode)
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .Select(u => new MailPageReportOutput() { }, true)
                // Mặc định xếp theo nhóm rồi thứ tự khi client không gửi tiêu chí sắp xếp
                .OrderByIF(string.IsNullOrWhiteSpace(command.Field), u => u.PluginCode)
                .OrderByIF(string.IsNullOrWhiteSpace(command.Field), u => u.OrderNo)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh mục báo cáo theo tiêu chí lọc (không phân trang)
        /// Created date: 01/10/2026
        /// </summary>
        public async Task<List<MailReportOutput>> HandleAsync(MailRptReportInput command)
        {
            var list = await _reportRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code == command.Code)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PluginCode), u => u.PluginCode == command.PluginCode)
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .Select(u => new MailReportOutput() { }, true)
                .OrderBy(u => u.PluginCode)
                .OrderBy(u => u.OrderNo)
                .ToListAsync();

            // Kỳ cố định khai trong code — cho form soạn lịch cảnh báo báo cáo lệch tần suất
            list.ForEach(x => x.PeriodType = RptReportCode.FixedPeriodOf(x.Code));
            return list;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 báo cáo theo ID
        /// Created date: 01/10/2026
        /// </summary>
        public async Task<MailReportOutput> HandleAsync(MailRptIdReportInput command)
        {
            var entity = await _reportRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var output = entity.Adapt<MailReportOutput>();
            output.PeriodType = RptReportCode.FixedPeriodOf(output.Code);
            return output;
        }
    }
}
