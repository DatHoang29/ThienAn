using Mapster;
using Modules.Notification.Controllers.Notification.Dto;
using Modules.Notification.Core.Entities;
using Modules.Notification.Core.Exceptions;
using Modules.Notification.Infrastructure;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using SqlSugar;
using Wolverine;

namespace Modules.Notification.Controllers.Notification.Queries
{
    /// <summary>
    /// Author: SonTH
    /// Description: Truy vấn thông báo của user hiện tại: phân trang, badge unread, chi tiết.
    ///
    ///              Mọi truy vấn đều scope theo UserId lấy từ claims — user chỉ thấy đúng
    ///              dòng NotiRecipient của mình. Lưới chạy trên NotiRecipient (đã denormalize
    ///              Category/Severity/SendTime); Title/Content của CHỈ trang hiện tại được
    ///              nạp thêm từ NotiMessage — một câu truy vấn, không N+1 (nếp Mail).
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<NotiRecipient> _recipientRep;
        private readonly BaseRepository<NotiMessage> _messageRep;

        public NotiQueryHandler(
            BaseRepository<NotiRecipient> recipientRep,
            BaseRepository<NotiMessage> messageRep)
        {
            _recipientRep = recipientRep;
            _messageRep = messageRep;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Danh sách thông báo của user hiện tại, phân trang.
        /// Created date: 17/09/2026
        /// </summary>
        public async Task<SqlSugarPagedList<NotiListOutput>> HandleAsync(NotiPageInput command)
        {
            var userId = CurrentUserId();

            // Lọc keyword phải tra sang NotiMessage (Title nằm đó) — lấy trước danh sách MessageId
            // khớp để câu truy vấn chính không phải join (nếp RptEmailScheduleQueryHandler).
            List<string>? matchedMessageIds = null;
            if (!string.IsNullOrWhiteSpace(command.Keyword))
            {
                matchedMessageIds = await _messageRep.AsQueryable()
                    .Where(m => m.IsDelete == null
                                && (m.Title.Contains(command.Keyword!) || m.Content.Contains(command.Keyword!)))
                    .Select(m => m.ID)
                    .Distinct()
                    .ToListAsync();

                // Không khớp thông báo nào thì trả rỗng luôn — chèn giá trị không thể trùng
                // để SqlSugar không sinh ra "IN ()".
                if (matchedMessageIds.Count == 0) matchedMessageIds.Add("__noti_no_match__");
            }

            var pagedList = await _recipientRep.AsQueryable()
                .Where(u => u.UserId == userId && u.IsDelete == null)
                .WhereIF(command.IsRead == true, u => u.ReadTime != null)
                .WhereIF(command.IsRead == false, u => u.ReadTime == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Category), u => u.Category == command.Category)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Severity), u => u.Severity == command.Severity)
                .WhereIF(command.FromTime.HasValue, u => u.SendTime >= command.FromTime)
                .WhereIF(command.ToTime.HasValue, u => u.SendTime <= command.ToTime)
                .WhereIF(matchedMessageIds != null, u => matchedMessageIds!.Contains(u.MessageId))
                .Select(u => new NotiListOutput() { }, true)
                // Thông báo mới lên đầu khi client không gửi tiêu chí sắp xếp
                .OrderByIF(string.IsNullOrWhiteSpace(command.Field), u => u.SendTime, OrderByType.Desc)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            var records = pagedList.Records?.ToList() ?? new List<NotiListOutput>();
            if (records.Count > 0)
            {
                // Nạp nội dung của đúng các thông báo trên trang hiện tại — một câu truy vấn
                var pageMessageIds = records.Select(u => u.MessageId).Distinct().ToList();
                var messages = await _messageRep.AsQueryable()
                    .Where(m => m.IsDelete == null && pageMessageIds.Contains(m.ID))
                    .ToListAsync();

                var grouped = messages.GroupBy(m => m.ID).ToDictionary(g => g.Key, g => g.First());

                foreach (var row in records)
                {
                    if (grouped.TryGetValue(row.MessageId, out var message))
                    {
                        row.Title = message.Title;
                        row.Content = message.Content;
                        row.SourceKey = message.SourceKey;
                    }
                }
            }

            return pagedList;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Số thông báo chưa đọc của user hiện tại — nguồn cho badge.
        /// Created date: 17/09/2026
        /// </summary>
        public async Task<NotiUnreadCountOutput> HandleAsync(NotiUnreadCountInput command)
        {
            var userId = CurrentUserId();

            var count = await _recipientRep.AsQueryable()
                .Where(u => u.UserId == userId && u.IsDelete == null && u.ReadTime == null)
                .CountAsync();

            return new NotiUnreadCountOutput { Count = count };
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Chi tiết một thông báo theo Id dòng người nhận. Dòng không thuộc
        ///              user hiện tại trả DataNotExist — không lộ sự tồn tại của thông báo người khác.
        /// Created date: 17/09/2026
        /// </summary>
        public async Task<NotiDetailOutput> HandleAsync(NotiIdInput command)
        {
            var userId = CurrentUserId();

            var recipient = await _recipientRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            if (recipient.UserId != userId)
            {
                // Của người khác vẫn trả "không tồn tại" thay vì "không có quyền".
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);
            }

            var message = await _messageRep.GetByIdAsync(recipient.MessageId)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var output = recipient.Adapt<NotiDetailOutput>();
            output.Title = message.Title;
            output.Content = message.Content;
            output.SourceKey = message.SourceKey;
            output.TargetType = message.TargetType;

            return output;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: UserId từ JWT claims. FE endpoint không nhận userId từ client —
        ///              thiếu claim là lỗi phiên đăng nhập, trả lỗi để FE đưa về màn login.
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
