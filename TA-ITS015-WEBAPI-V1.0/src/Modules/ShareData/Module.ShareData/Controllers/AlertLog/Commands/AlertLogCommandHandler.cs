using Module.ShareData.Core.Dto.AlertLog;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Infrastructure;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.ShareData.Controllers.AlertLog.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý xác nhận Cảnh báo và lỗi (ShareDataAlertLog).
    /// Created date: 2026-08-21
    /// </summary>
    public class AlertLogCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataAlertLog> _alertLogRep;
        private readonly BaseCacheService _cache;

        public AlertLogCommandHandler(
            BaseRepository<ShareDataAlertLog> alertLogRep,
            BaseCacheService cache)
        {
            _alertLogRep = alertLogRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xác nhận đã xử lý một hoặc nhiều cảnh báo. Trả về số bản ghi thực sự đổi trạng thái.
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<int> HandleAsync(ShareDataAcknowledgeAlertInput command)
        {
            var ids = command.Ids?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList() ?? [];
            if (ids.Count == 0) return 0;

            var existing = await _alertLogRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .CountAsync();

            if (existing != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            var affected = await _alertLogRep.AsUpdateable()
                .SetColumns(u => new ShareDataAlertLog
                {
                    Acknowledged = true,
                    AckBy = CurrentOperator(),
                    AckAt = DateTime.Now
                })
                .Where(u => u.IsDelete == null
                    && ids.Contains(u.ID)
                    && (u.Acknowledged == null || u.Acknowledged == false))
                .ExecuteCommandAsync();

            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataAlertLog);
            return affected;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xác nhận toàn bộ cảnh báo chưa xử lý theo bộ lọc hiện tại.
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<int> HandleAsync(ShareDataAcknowledgeAllAlertInput command)
        {
            var now = DateTime.Now;
            var ackBy = CurrentOperator();
            // OccurredAt lưu theo giờ server: mốc ToDate gửi theo UTC thì đổi về giờ server trước khi so
            var toDate = command.ToDate is { Kind: DateTimeKind.Utc } utcToDate ? utcToDate.ToLocalTime() : command.ToDate;

            var affected = await _alertLogRep.AsUpdateable()
                .SetColumns(u => new ShareDataAlertLog
                {
                    Acknowledged = true,
                    AckBy = ackBy,
                    AckAt = now
                })
                .Where(u => u.IsDelete == null && (u.Acknowledged == null || u.Acknowledged == false))
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(command.Severity.HasValue, u => u.Severity == command.Severity)
                .WhereIF(toDate != null, u => u.OccurredAt <= toDate)
                .ExecuteCommandAsync();

            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataAlertLog);
            return affected;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Tài khoản đang thao tác. Luồng nền không có HttpContext thì ghi 'system'.
        /// Created date: 2026-08-21
        /// </summary>
        private static string CurrentOperator()
        {
            try
            {
                return App.User == null ? "system" : App.User.GetUsername();
            }
            catch
            {
                return "system";
            }
        }
    }
}
