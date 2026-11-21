using Microsoft.Extensions.Logging;
using Module.Notification.Infrastructure.Services.Config;
using Modules.Notification.Core.Constants;
using Modules.Notification.Core.Entities;
using Modules.Notification.Core.Enums;
using Modules.Notification.Core.Exceptions;
using Modules.Notification.Core.Messages;
using Modules.Notification.Infrastructure;
using Modules.Notification.Infrastructure.Services;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Modules.Notification.Controllers.Notification.Commands
{
    /// <summary>
    /// Author: SonTH
    /// Description: Handler GỬI thông báo — điểm vào cho mọi module khác qua IMessageBus.
    ///
    ///              Một lệnh = một nhánh duy nhất: check idempotency → validate target →
    ///              resolve người nhận → chèn NotiMessage + fan-out NotiRecipient trong một
    ///              transaction → broadcast tín hiệu nhẹ (best-effort, sau commit).
    ///              Không tạo Hangfire job cho luồng gửi (tài liệu §6.6).
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiSendHandler : IWolverineHandler
    {
        private readonly BaseRepository<NotiMessage> _messageRep;
        private readonly BaseRepository<NotiRecipient> _recipientRep;
        private readonly INotiUserResolver _resolver;
        private readonly NotiBroadcaster _broadcaster;
        private readonly NotiConfigService _configService;
        private readonly ILogger<NotiSendHandler> _logger;

        public NotiSendHandler(
            BaseRepository<NotiMessage> messageRep,
            BaseRepository<NotiRecipient> recipientRep,
            INotiUserResolver resolver,
            NotiBroadcaster broadcaster,
            NotiConfigService configService,
            ILogger<NotiSendHandler> logger)
        {
            _messageRep = messageRep;
            _recipientRep = recipientRep;
            _resolver = resolver;
            _broadcaster = broadcaster;
            _configService = configService;
            _logger = logger;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gửi một thông báo. Trả về Id thông báo + số người nhận đã fan-out.
        /// Created date: 17/09/2026
        /// </summary>
        public async Task<SendNotificationResult> HandleAsync(SendNotificationCommand command)
        {
            // 1. Chống gửi trùng: key đã tồn tại thì trả kết quả cũ, không fan-out lần hai.
            //    ClearFilter để tính cả bản ghi đã soft-delete — gửi trùng là gửi trùng.
            if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                var existed = await _messageRep.AsQueryable()
                    .ClearFilter()
                    .FirstAsync(m => m.IdempotencyKey == command.IdempotencyKey);

                if (existed != null)
                {
                    var duplicatedCount = await _recipientRep.AsQueryable()
                        .ClearFilter()
                        .Where(r => r.MessageId == existed.ID)
                        .CountAsync();

                    return new SendNotificationResult
                    {
                        MessageId = existed.ID,
                        RecipientCount = duplicatedCount,
                        Duplicated = true,
                    };
                }
            }

            // 2. Ràng buộc theo TargetType (validator chỉ kiểm được định dạng, không kiểm được dữ liệu).
            Normalize(command);

            // 3. Resolve người nhận theo phạm vi — bảng user/org thuộc phân hệ System, hỏi qua API.
            var seeds = await _resolver.ResolveAsync(command);
            if (seeds.Count == 0)
            {
                throw Oops.Oh(BaseMsg.Noti.Exception.RecipientsEmpty);
            }

            var maxRecipients = _configService.MaxRecipientsPerMessage;
            if (seeds.Count > maxRecipients)
            {
                throw Oops.Oh(BaseMsg.Noti.Exception.TooManyRecipients);
            }

            // 4. Chèn message + fan-out recipient trong MỘT transaction (theo đúng nếp Mail:
            //    BeginTran thủ công, không rollback thì transaction treo lại trên connection).
            var now = DateTime.Now;
            var message = BuildMessage(command, now);
            var recipients = new List<NotiRecipient>();

            try
            {
                _messageRep.Context.Ado.BeginTran();

                await _messageRep.InsertWithDiffLogAsync(message);

                var order = 1;
                foreach (var seed in seeds)
                {
                    // Code là NOT NULL trên EntityTenant — đặt tên có nghĩa để tra cứu nhanh.
                    recipients.Add(new NotiRecipient
                    {
                        Code = $"{message.ID}_{order:000000}",
                        MessageId = message.ID,
                        UserId = seed.UserId,
                        Category = message.Category,
                        Severity = message.Severity,
                        SendTime = message.SendTime,
                        CreateUId = null,
                    });
                    order++;
                }

                foreach (var batch in Chunk(recipients, NotiConst.InsertBatchSize))
                {
                    await _recipientRep.InsertRangeAsync(batch);
                }

                _messageRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _messageRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            // 5. Broadcast tín hiệu nhẹ SAU commit — lỗi broadcast chỉ log, không fail gửi.
            if (_configService.EnableBroadcast)
            {
                _broadcaster.NotifyCreated(recipients, message.Severity);
            }

            _logger.LogInformation("Notification sent: MessageId={MessageId}, Target={Target}, Recipients={Count}, Category={Category}, SourceKey={SourceKey}",
                message.ID, command.TargetType, recipients.Count, message.Category, command.SourceKey);

            return new SendNotificationResult
            {
                MessageId = message.ID,
                RecipientCount = recipients.Count,
            };
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Chuẩn hoá + kiểm tra ràng buộc theo TargetType.
        /// Created date: 17/09/2026
        /// </summary>
        private static void Normalize(SendNotificationCommand command)
        {
            // Mức độ lạ thì about mặt về Info thay vì chặn: gửi thông báo không nên chết vì enum mới.
            if (!Enum.TryParse<NotiEnums.SeverityEnum>(command.Severity, true, out _))
            {
                command.Severity = NotiEnums.SeverityEnum.Info.ToString();
            }

            switch (command.TargetType)
            {
                case NotiEnums.TargetTypeEnum.User when command.UserIds == null || command.UserIds.All(u => string.IsNullOrWhiteSpace(u)):
                    throw Oops.Oh(BaseMsg.Noti.Validation.UserIdsRequired);

                case NotiEnums.TargetTypeEnum.Org when command.OrgIds == null || command.OrgIds.All(u => string.IsNullOrWhiteSpace(u)):
                    throw Oops.Oh(BaseMsg.Noti.Validation.OrgIdsRequired);
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Dựng entity NotiMessage từ lệnh gửi — chỉ các cột lõi ghi nhận + hiển thị.
        /// Created date: 17/09/2026
        /// </summary>
        private static NotiMessage BuildMessage(SendNotificationCommand command, DateTime now)
        {
            return new NotiMessage
            {
                Code = $"NTI{now:yyyyMMddHHmmssfff}{Random.Shared.Next(100, 999)}",
                Title = command.Title,
                Content = command.Content,
                Category = command.Category,
                Severity = command.Severity,
                TargetType = command.TargetType.ToString(),
                SourceKey = command.SourceKey,
                IdempotencyKey = command.IdempotencyKey,
                SendTime = now,
                CreateUId = null,
            };
        }

        /// <summary>Chia nhỏ danh sách recipient cho từng lệnh INSERT.</summary>
        private static IEnumerable<List<T>> Chunk<T>(List<T> source, int size)
        {
            for (var i = 0; i < source.Count; i += size)
            {
                yield return source.Skip(i).Take(size).ToList();
            }
        }
    }
}
