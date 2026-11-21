using Mapster;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;
using Modules.Mail.Core.Dto.EmailReport;
using Modules.Mail.Core.Entities;
using Modules.Mail.Core.Exceptions;
using Modules.Mail.Infrastructure;

namespace Modules.Mail.Controllers.EmailReport.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / bật tắt / xóa danh mục báo cáo gửi email.
    ///
    ///              RptEmailScheduleItem tham chiếu báo cáo theo MÃ (Code), nên báo cáo đang có lịch dùng thì
    ///              KHÔNG được xoá hay đổi mã — lịch sẽ trỏ tới mã không còn. Muốn ngừng thì tắt (ToggleState):
    ///              báo cáo tắt bị ẩn khỏi ô chọn và bị bỏ qua lúc gửi, lịch vẫn gửi các báo cáo còn lại.
    /// Created date: 01/10/2026
    /// </summary>
    public class RptEmailReportCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<MailReport> _reportRep;
        private readonly BaseRepository<MailScheduleItem> _itemRep;
        private readonly BaseCacheService _cache;

        public RptEmailReportCommandHandler(
            BaseRepository<MailReport> reportRep,
            BaseRepository<MailScheduleItem> itemRep,
            BaseCacheService cache)
        {
            _reportRep = reportRep;
            _itemRep = itemRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 báo cáo. Mã là duy nhất trong các bản ghi chưa xoá.
        /// Created date: 01/10/2026
        /// </summary>
        public async Task HandleAsync(MailRptAddReportInput command)
        {
            Normalize(command);

            var isExist = await _reportRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => u.Code == command.Code && u.IsDelete == null);

            if (isExist)
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            var entity = command.Adapt<MailReport>();
            entity.CreateUId = null;

            await _reportRep.InsertWithDiffLogAsync(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 báo cáo. Đổi mã chỉ được khi chưa lịch nào dùng mã cũ.
        /// Created date: 01/10/2026
        /// </summary>
        public async Task HandleAsync(MailRptUpdateReportInput command)
        {
            Normalize(command);

            var entity = await _reportRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var isExist = await _reportRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => u.Code == command.Code && u.ID != command.ID && u.IsDelete == null);

            if (isExist)
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            if (!string.Equals(entity.Code, command.Code, StringComparison.Ordinal))
                await EnsureNotUsedAsync(entity.Code);

            // Map dữ liệu từ command vào bản ghi gốc — thiếu bước này thì Update thành công nhưng DB không đổi.
            command.Adapt(entity);

            await _reportRep.UpdateWithDiffLogAsync(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Bật / tắt 1 báo cáo. Tắt không ảnh hưởng dữ liệu lịch: báo cáo bị bỏ qua lúc gửi.
        /// Created date: 01/10/2026
        /// </summary>
        public async Task HandleAsync(MailRptToggleReportInput command)
        {
            var entity = await _reportRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            entity.Status = command.Status;

            await _reportRep.UpdateWithDiffLogAsync(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa mềm 1 báo cáo. Chặn khi còn lịch dùng.
        /// Created date: 01/10/2026
        /// </summary>
        public async Task HandleAsync(MailRptDeleteReportInput command)
        {
            var entity = await _reportRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            await EnsureNotUsedAsync(entity.Code);

            await _reportRep.SoftDeleteAsync<MailReport>(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa mềm nhiều báo cáo. Chỉ một báo cáo đang được dùng là chặn cả lô.
        /// Created date: 01/10/2026
        /// </summary>
        public async Task HandleAsync(List<MailRptDeleteReportInput> command)
        {
            var ids = command.Select(item => item.ID).Distinct().ToList();

            var entities = await _reportRep.AsQueryable()
                .Where(u => ids.Contains(u.ID) && u.IsDelete == null)
                .ToListAsync();

            if (entities.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            foreach (var entity in entities)
                await EnsureNotUsedAsync(entity.Code);

            await _reportRep.SoftDeleteAsync<MailReport>(u => ids.Contains(u.ID));
            ClearCache();
        }

        /// <summary>Chặn khi mã báo cáo đang nằm trong lịch gửi email (item của lịch chưa xoá).</summary>
        private async Task EnsureNotUsedAsync(string code)
        {
            var count = await _itemRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ReportCode == code)
                .Select(u => u.ScheduleId)
                .Distinct()
                .CountAsync();

            if (count > 0)
                throw Oops.Oh(BaseMsg.Mail.Exception.ReportInUse, code, count);
        }

        /// <summary>Cắt khoảng trắng, chuẩn hoá giá trị rỗng. Không kiểm tra mã có trong RptManagement (frx).</summary>
        private static void Normalize(MailRptAddReportInput command)
        {
            command.Code = command.Code?.Trim() ?? string.Empty;
            command.Name = command.Name?.Trim() ?? string.Empty;
            command.PluginCode = command.PluginCode?.Trim() ?? string.Empty;
            command.DetailParams = string.IsNullOrWhiteSpace(command.DetailParams) ? null : command.DetailParams.Trim();
            command.Remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();
            command.Status ??= BaseEnums.StatusEnum.Enable;
        }

        private void ClearCache()
        {
            _cache.RemoveByPrefixKey(CacheConst.Mail.RptEmailReport);
        }
    }
}
