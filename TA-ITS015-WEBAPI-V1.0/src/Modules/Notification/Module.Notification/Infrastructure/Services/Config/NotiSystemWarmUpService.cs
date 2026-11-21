using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Notification.Core.Entities;
using Modules.Notification.Infrastructure;
using Modules.System.Controllers.Org.Dto;
using Modules.System.Controllers.User.Dto;
using Modules.System.Core.Entities;
using Wolverine;
using static Shared.DTO.Enums.BaseEnums;

namespace Module.Notification.Infrastructure.Services.Config
{
    /// <summary>
    /// Author: SonTH
    /// Description: Warm-up mapping SqlSugar trước khi Kestrel nhận request, cho các entity
    ///              mà phân hệ Notification sẽ chạm tới:
    ///              (1) entity System (SysUser/SysOrg/SysPos/SysUserRole/SysRole) qua MessBus;
    ///              (2) entity của chính Notification (NotiMessage/NotiRecipient) trên DB NotiDefault.
    ///
    ///              VÌ SAO CẦN: SqlSugar InitMappingInfo không thread-safe — lần CHẠM ĐẦU vào
    ///              một loại entity sẽ đưa mapping vào cache dùng chung; nếu nhiều luồng chạm
    ///              lần đầu song song thì một luồng đang enumerate trong khi luồng kia thêm vào
    ///              -> "Collection was modified; enumeration operation may not execute."
    ///              Hosted service chạy TRƯỚC khi nhận request nên các lần chạm đầu được dồn về
    ///              MỘT luồng, tuần tự. Sau khi cache ấm, InitMappingInfo chỉ còn tra cứu.
    ///
    ///              Lỗi warm-up chỉ log, KHÔNG chặn khởi động (DB chưa sẵn sàng thì request
    ///              thực tế vẫn có thể chạy bình thường như mọi module khác của app).
    /// Created date: 18/09/2026
    /// </summary>
    public class NotiSystemWarmUpService : IHostedService
    {
        private readonly IMessageBus _bus;
        private readonly BaseRepository<NotiMessage> _messageRep;
        private readonly BaseRepository<NotiRecipient> _recipientRep;
        private readonly ILogger<NotiSystemWarmUpService> _logger;

        public NotiSystemWarmUpService(
            IMessageBus bus,
            BaseRepository<NotiMessage> messageRep,
            BaseRepository<NotiRecipient> recipientRep,
            ILogger<NotiSystemWarmUpService> logger)
        {
            _bus = bus;
            _messageRep = messageRep;
            _recipientRep = recipientRep;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // (0) Bổ sung index mà [SugarIndex] không diễn tả được: unique CÓ ĐIỀU KIỆN
                //     WHERE IdempotencyKey IS NOT NULL — SQL Server unique thường chỉ cho
                //     1 dòng NULL, trong khi phần lớn message không có key. Idempotent:
                //     đã tồn tại thì bỏ qua, chạy lại mỗi khởi động không sao.
                await EnsureIdempotencyIndexAsync();

                // (1) Entity System — hai lời gọi đúng như NotiUserResolver sẽ dùng
                await _bus.InvokeAsync<List<UserOutput>>(new ListUserInput { Status = StatusEnum.Enable });
                await _bus.InvokeAsync<List<SysOrg>>(new OrgInput { ID = "0" });

                // (2) Entity Noti* — để lần gửi đầu không phải là lần chạm đầu
                await _messageRep.AsQueryable().CountAsync(cancellationToken);
                await _recipientRep.AsQueryable().CountAsync(cancellationToken);

                _logger.LogInformation("[Notification] Warm-up mapping (System + Noti) done.");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Notification] Warm-up mapping thất bại (DB chưa sẵn sàng?) — bỏ qua, không chặn khởi động.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Author: SonTH
        /// Description: Tạo unique index có điều kiện cho IdempotencyKey nếu chưa có — chạy sau
        ///              bước InitTables của SqlSugar (AddSqlSugar chạy trước hosted service).
        ///              Bảng/cột do SqlSugar generate theo EnableInitTable của ConfigId NotiDefault;
        ///              riêng loại index filtered này thuộc phần "gia cố" của chính module.
        /// Created date: 18/09/2026
        /// </summary>
        private async Task EnsureIdempotencyIndexAsync()
        {
            const string indexName = "UX_NotiMessage_IdempotencyKey";

            var exists = Convert.ToInt32(await _messageRep.Context.Ado.GetScalarAsync(
                "SELECT COUNT(1) FROM sys.indexes WHERE name = @name AND object_id = OBJECT_ID('dbo.NotiMessage')",
                new { name = indexName }));

            if (exists == 0)
            {
                await _messageRep.Context.Ado.ExecuteCommandAsync(
                    $"CREATE UNIQUE NONCLUSTERED INDEX [{indexName}] ON dbo.NotiMessage (IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;");
            }
        }
    }
}
