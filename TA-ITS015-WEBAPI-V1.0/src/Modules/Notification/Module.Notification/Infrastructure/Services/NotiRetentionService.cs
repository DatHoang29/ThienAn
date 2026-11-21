using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.Notification.Infrastructure.Services.Config;
using Modules.Notification.Core.Constants;
using Modules.Notification.Core.Entities;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Notification.Infrastructure.Services
{
    /// <summary>
    /// Author: SonTH
    /// Description: Job dọn dữ liệu thông báo cũ.
    ///
    ///              Được gọi bởi MỘT recurring job Hangfire duy nhất ("noti-retention-scan",
    ///              đăng ký trong Startup.Configure khi Hangfire:Enable = true) — cùng nếp
    ///              rpt-email-scan. KHÔNG tạo job riêng cho từng thông báo: hết hạn tính được
    ///              từ dữ liệu (SendTime + RetentionDays), Hangfire không bổ sung gì.
    ///
    ///              Tiêu chí xoá hard: SendTime cũ hơn RetentionDays. Xoá recipient TRƯỚC
    ///              message để không sót mồ côi nếu process chết giữa chừng — message mồ côi
    ///              sẽ bị dọn ở lượt quét sau.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiRetentionService : IScoped
    {
        private readonly BaseRepository<NotiMessage> _messageRep;
        private readonly BaseRepository<NotiRecipient> _recipientRep;
        private readonly NotiSettings _settings;
        private readonly ILogger<NotiRetentionService> _logger;

        public NotiRetentionService(
            BaseRepository<NotiMessage> messageRep,
            BaseRepository<NotiRecipient> recipientRep,
            IOptions<NotiSettings> settings,
            ILogger<NotiRetentionService> logger)
        {
            _messageRep = messageRep;
            _recipientRep = recipientRep;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Quét và xoá hard dữ liệu hết hạn. Hangfire gọi trực tiếp hàm này.
        /// Created date: 17/09/2026
        /// </summary>
        public async Task ScanAndCleanAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var retentionCutoff = now.AddDays(-_settings.RetentionDays);

            // ClearFilter để tính cả message đã bị soft-delete ở tầng khác — retention là bước cuối, phải dọn sạch.
            var expiredIds = await _messageRep.AsQueryable()
                .ClearFilter()
                .Where(m => m.SendTime < retentionCutoff)
                .Select(m => m.ID)
                .ToListAsync();

            if (expiredIds.Count == 0) return;

            var recipientsDeleted = 0;
            foreach (var chunk in Chunk(expiredIds, NotiConst.InsertBatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Recipient trước — message xoá trước sẽ để lại recipient mồ côi nếu process chết.
                recipientsDeleted += await _recipientRep.Context
                    .Deleteable<NotiRecipient>()
                    .Where(r => chunk.Contains(r.MessageId))
                    .ExecuteCommandAsync();

                await _messageRep.Context
                    .Deleteable<NotiMessage>()
                    .Where(m => chunk.Contains(m.ID))
                    .ExecuteCommandAsync();
            }

            _logger.LogInformation("Notification retention cleaned {Messages} messages / {Recipients} recipients, cutoff={Cutoff}",
                expiredIds.Count, recipientsDeleted, retentionCutoff);
        }

        /// <summary>Chia nhỏ danh sách ID để mệnh đề IN không phình quá lớn.</summary>
        private static IEnumerable<List<string>> Chunk(List<string> source, int size)
        {
            for (var i = 0; i < source.Count; i += size)
            {
                yield return source.Skip(i).Take(size).ToList();
            }
        }
    }
}
