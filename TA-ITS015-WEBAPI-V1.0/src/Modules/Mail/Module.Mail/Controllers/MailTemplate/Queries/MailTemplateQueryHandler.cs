using Mapster;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using SqlSugar;
using Wolverine;
using Modules.Mail.Core.Dto.TemplateMail;
using Modules.Mail.Core.Entities;
using Modules.Mail.Infrastructure;
using Modules.Mail.Infrastructure.Services.Mail.Templates;

namespace Modules.Mail.Controllers.EmailTemplate.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn mẫu email. Danh sách không đọc file nội dung; chi tiết (theo ID / mã) trả kèm Content.
    /// Created date: 12/10/2026
    /// </summary>
    public class RptEmailTemplateQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<MailTemplate> _templateRep;
        private readonly MailTemplateStore _templates;

        public RptEmailTemplateQueryHandler(BaseRepository<MailTemplate> templateRep, MailTemplateStore templates)
        {
            _templateRep = templateRep;
            _templates = templates;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách mẫu email theo phân trang
        /// Created date: 12/10/2026
        /// </summary>
        public async Task<SqlSugarPagedList<RptPageEmailTemplateOutput>> HandleAsync(MailPageTemplateInput command)
        {
            var code = command.Code?.Trim();
            var name = command.Name?.Trim();

            return await _templateRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(code), u => u.Code.Contains(code!))
                .WhereIF(!string.IsNullOrWhiteSpace(name), u => u.Name.Contains(name!))
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .Select(u => new RptPageEmailTemplateOutput() { }, true)
                // Mặc định xếp theo thứ tự rồi mới tạo trước khi client không gửi tiêu chí sắp xếp
                .OrderByIF(string.IsNullOrWhiteSpace(command.Field), u => u.OrderNo)
                .OrderByIF(string.IsNullOrWhiteSpace(command.Field), u => u.CreateTime, OrderByType.Desc)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ mẫu email theo tiêu chí lọc (không phân trang, không kèm nội dung)
        /// Created date: 12/10/2026
        /// </summary>
        public async Task<List<RptEmailTemplateOutput>> HandleAsync(MailTemplateInput command)
        {
            var code = command.Code?.Trim();

            return await _templateRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(code), u => u.Code == code)
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .Select(u => new RptEmailTemplateOutput() { }, true)
                .OrderBy(u => u.OrderNo)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 mẫu email theo ID, kèm nội dung HTML
        /// Created date: 12/10/2026
        /// </summary>
        public async Task<RptEmailTemplateOutput> HandleAsync(MailIdTemplateInput command)
        {
            var entity = await _templateRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            return ToOutput(entity);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 mẫu email theo mã, kèm nội dung HTML
        /// Created date: 12/10/2026
        /// </summary>
        public async Task<RptEmailTemplateOutput> HandleAsync(MailCodeTemplateInput command)
        {
            var code = command.Code?.Trim();
            var entity = await _templateRep.GetFirstAsync(u => u.Code == code && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            return ToOutput(entity);
        }

        private RptEmailTemplateOutput ToOutput(MailTemplate entity)
        {
            var output = entity.Adapt<RptEmailTemplateOutput>();
            output.Content = _templates.Read(entity.FilePath);
            return output;
        }
    }
}
