using Mapster;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;
using Modules.Mail.Core.Dto.TemplateMail;
using Modules.Mail.Core.Entities;
using Modules.Mail.Infrastructure;
using Modules.Mail.Infrastructure.Services.Mail.Templates;

namespace Modules.Mail.Controllers.EmailTemplate.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / bật tắt / xóa mẫu email.
    /// Created date: 12/10/2026
    /// </summary>
    public class RptEmailTemplateCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<MailTemplate> _templateRep;
        private readonly MailTemplateStore _templates;
        private readonly BaseCacheService _cache;

        public RptEmailTemplateCommandHandler(
            BaseRepository<MailTemplate> templateRep,
            MailTemplateStore templates,
            BaseCacheService cache)
        {
            _templateRep = templateRep;
            _templates = templates;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 mẫu. Mã là duy nhất trong các bản ghi chưa xoá.
        /// Created date: 12/10/2026
        /// </summary>
        public async Task HandleAsync(MailAddTemplateInput command)
        {
            Normalize(command);

            var isExist = await _templateRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => u.Code == command.Code && u.IsDelete == null);

            if (isExist)
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            var entity = command.Adapt<MailTemplate>();
            entity.CreateUId = null;

            try
            {
                _templateRep.Context.Ado.BeginTran();

                await _templateRep.InsertWithDiffLogAsync(entity);

                // File đặt tên theo ID nên ghi SAU khi insert; ghi file lỗi thì rollback cả mẫu
                entity.FilePath = await _templates.WriteTemplateAsync(entity.ID, command.Content!);
                await _templateRep.Context.Updateable<MailTemplate>()
                    .SetColumns(u => u.FilePath == entity.FilePath)
                    .Where(u => u.ID == entity.ID)
                    .ExecuteCommandAsync();

                _templateRep.Context.Ado.CommitTran();
            }
            catch
            {
                // Không rollback thì transaction treo lại trên connection và làm hỏng các lệnh sau
                try { _templateRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 mẫu — ghi đè file nội dung của mẫu.
        /// Created date: 12/10/2026
        /// </summary>
        public async Task HandleAsync(MailUpdateTemplateInput command)
        {
            Normalize(command);

            var entity = await _templateRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var isExist = await _templateRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => u.Code == command.Code && u.ID != command.ID && u.IsDelete == null);

            if (isExist)
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // Map dữ liệu từ command vào bản ghi gốc — thiếu bước này thì Update thành công nhưng DB không đổi.
            command.Adapt(entity);
            entity.FilePath = await _templates.WriteTemplateAsync(entity.ID, command.Content!);

            await _templateRep.UpdateWithDiffLogAsync(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Bật / tắt 1 mẫu. Mẫu tắt bị ẩn khỏi ô chọn mẫu ở form lịch.
        /// Created date: 12/10/2026
        /// </summary>
        public async Task HandleAsync(MailToggleTemplateInput command)
        {
            var entity = await _templateRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            entity.Status = command.Status;

            await _templateRep.UpdateWithDiffLogAsync(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa mềm 1 mẫu (giữ file nội dung).
        /// Created date: 12/10/2026
        /// </summary>
        public async Task HandleAsync(MailDeleteTemplateInput command)
        {
            var entity = await _templateRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            await _templateRep.SoftDeleteAsync<MailTemplate>(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa mềm nhiều mẫu.
        /// Created date: 12/10/2026
        /// </summary>
        public async Task HandleAsync(List<MailDeleteTemplateInput> command)
        {
            var ids = command.Select(item => item.ID).Distinct().ToList();

            var count = await _templateRep.AsQueryable()
                .Where(u => ids.Contains(u.ID) && u.IsDelete == null)
                .CountAsync();

            if (count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            await _templateRep.SoftDeleteAsync<MailTemplate>(u => ids.Contains(u.ID));
            ClearCache();
        }

        /// <summary>Cắt khoảng trắng, chuẩn hoá giá trị rỗng, làm sạch HTML.</summary>
        private static void Normalize(MailAddTemplateInput command)
        {
            command.Code = command.Code?.Trim() ?? string.Empty;
            command.Name = command.Name?.Trim() ?? string.Empty;
            // Mẫu được phép trống nội dung — người dùng soạn tiếp ở form lịch
            command.Content = MailHtmlSanitizer.Sanitize(command.Content);
            command.Remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();
            command.Status ??= BaseEnums.StatusEnum.Enable;
        }

        private void ClearCache()
        {
            _cache.RemoveByPrefixKey(CacheConst.Mail.RptEmailTemplate);
        }
    }
}
