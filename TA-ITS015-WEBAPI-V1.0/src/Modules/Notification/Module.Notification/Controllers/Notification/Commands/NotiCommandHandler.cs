using Modules.Notification.Core.Entities;
using Modules.Notification.Core.Exceptions;
using Modules.Notification.Controllers.Notification.Dto;
using Modules.Notification.Infrastructure;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.Notification.Controllers.Notification.Commands
{
    /// <summary>
    /// Author: SonTH
    /// Description: Handler thao tác của user trên thông báo của CHÍNH MÌNH: đánh dấu đã đọc, xoá.
    ///
    ///              Mọi câu lệnh đều kèm điều kiện UserId = user hiện tại — kể cả khi FE truyền
    ///              Id của người khác thì cũng chỉ ra rỗng/không đổi gì, không có gì bị lộ.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<NotiRecipient> _recipientRep;

        public NotiCommandHandler(BaseRepository<NotiRecipient> recipientRep)
        {
            _recipientRep = recipientRep;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Đánh dấu đã đọc — theo Ids hoặc tất cả (All = true cho nút "đọc hết").
        ///              Chỉ đụng dòng chưa đọc của đúng user hiện tại.
        /// Created date: 17/09/2026
        /// </summary>
        public async Task<bool> HandleAsync(NotiMarkReadInput command)
        {
            var userId = CurrentUserId();
            var now = DateTime.Now;

            if (!command.All && (command.Ids == null || command.Ids.Count == 0))
            {
                // Không có gì để đánh dấu — coi như thành công, FE khỏi hiện lỗi thừa.
                return true;
            }

            var ids = command.Ids ?? new List<string>();

            // Cập nhật hàng loạt bằng một câu UPDATE có điều kiện — an toàn đa instance vì
            // mỗi user chỉ đụng dòng của mình, không có hai request cùng đổi một dòng.
            var updated = await _recipientRep.Context.Updateable<NotiRecipient>()
                .SetColumns(r => new NotiRecipient { ReadTime = now })
                .Where(r => r.UserId == userId
                            && r.IsDelete == null
                            && r.ReadTime == null
                            && (command.All || ids.Contains(r.ID)))
                .ExecuteCommandAsync();

            return updated >= 0;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Xoá (soft-delete) các dòng thông báo của chính mình theo Ids.
        ///              NotiMessage giữ nguyên — dọn hẳn là việc của job retention.
        /// Created date: 17/09/2026
        /// </summary>
        public async Task<bool> HandleAsync(NotiDeleteInput command)
        {
            var userId = CurrentUserId();

            if (command.Ids == null || command.Ids.Count == 0)
            {
                return true;
            }

            var rows = await _recipientRep.AsQueryable()
                .Where(u => u.UserId == userId && u.IsDelete == null && command.Ids.Contains(u.ID))
                .ToListAsync();

            foreach (var row in rows)
            {
                await _recipientRep.SoftDeleteAsync<NotiRecipient>(row);
            }

            return true;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: UserId từ JWT claims — không nhận userId từ client.
        /// Created date: 17/09/2026
        /// </summary>
        private static string CurrentUserId()
        {
            var userId = App.User?.FindFirst(ClaimConst.UserId)?.Value;

            return string.IsNullOrWhiteSpace(userId)
                ? throw Oops.Oh(BaseMsg.Noti.Exception.UserMissing)
                : userId;
        }
    }
}
