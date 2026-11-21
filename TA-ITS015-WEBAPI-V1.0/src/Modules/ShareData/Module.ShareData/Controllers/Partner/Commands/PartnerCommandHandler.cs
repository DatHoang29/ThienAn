using Mapster;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Dto.Session;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataActivityService;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Wolverine;

namespace Module.ShareData.Controllers.Partner.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa và thao tác kết nối của Đối tác chia sẻ (ShareDataPartner)
    /// Created date: 2026-08-04
    /// </summary>
    public class PartnerCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataPartner> _partnerRep;
        private readonly BaseRepository<ShareDataSession> _sessionRep;
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly BaseRepository<ShareDataLastSend> _lastSendRep;
        private readonly BaseCacheService _cache;
        private readonly ShareDataActivityLogger _activityLogger;

        public PartnerCommandHandler(
            BaseRepository<ShareDataPartner> partnerRep,
            BaseRepository<ShareDataSession> sessionRep,
            BaseRepository<ShareDataSubscription> subscriptionRep,
            BaseRepository<ShareDataLastSend> lastSendRep,
            BaseCacheService cache,
            ShareDataActivityLogger activityLogger)
        {
            _partnerRep = partnerRep;
            _sessionRep = sessionRep;
            _subscriptionRep = subscriptionRep;
            _lastSendRep = lastSendRep;
            _cache = cache;
            _activityLogger = activityLogger;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 đối tác. 
        /// Khởi tạo Status = Enable, SessionState = Disconnected. Xóa mốc còn sót của đối tác cũ cùng mã: đối tác mới gửi từ đầu.
        /// Created date: 2026-08-04
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task HandleAsync(ShareDataAddPartnerInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            command.Address = command.Address?.Trim();
            command.EndPointApiUrl = command.EndPointApiUrl?.Trim();

            if (await _partnerRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            command.Status ??= BaseEnums.StatusEnum.Enable;
            command.SessionState = BaseEnums.SessionState.Disconnected;
            command.CreateUId = null;
            command.ConnectionType ??= ConnectionType.Api;
            command.ConnectionConfig = command.ConnectionConfig?.Trim();

            await ClearCursorsAsync(command.Code);

            var entity = command.Adapt<ShareDataPartner>();
            await _partnerRep.InsertWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPartner);

            await _activityLogger.LogCreateAsync(entity);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 đối tác. Không cho đổi mã: mốc gửi và gói tin nhận gắn theo mã, đổi mã thì đối tác bị gửi lại từ đầu.
        /// Created date: 2026-08-04
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task HandleAsync(ShareDataUpdatePartnerInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            command.Address = command.Address?.Trim();
            command.EndPointApiUrl = command.EndPointApiUrl?.Trim();
            command.ConnectionType ??= ConnectionType.Api;
            command.ConnectionConfig = command.ConnectionConfig?.Trim();

            if (await _partnerRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            var entity = await _partnerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (!string.Equals(entity.Code?.Trim(), command.Code, StringComparison.Ordinal))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerCodeLocked);

            if (IsConnectionParamChanged(entity, command) && await HasActiveSessionAsync(command.ID))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerSessionActive);

            var before = entity.Adapt<ShareDataPartner>();
            await _partnerRep.UpdateWithDiffLogAsync(command.Adapt(entity));
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPartner);

            await _activityLogger.LogUpdateAsync(before, entity);

            if (before.Status != BaseEnums.StatusEnum.Disable && entity.Status == BaseEnums.StatusEnum.Disable)
                await PauseActiveSubscriptionsAsync(entity, null, $"Tắt đối tác \"{entity.Name}\"");
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 đối tác, xóa luôn mốc gửi của đối tác
        /// Created date: 2026-08-04
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task HandleAsync(ShareDataDeletePartnerInput command)
        {
            var entity = await _partnerRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await HasAliveSubscriptionAsync(u => u.PartnerId == command.ID))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerHasActiveSubscription);

            await CloseActiveSessionsAsync(command.ID, ShareDataConst.Reason.PartnerDisabled);

            var before = entity.Adapt<ShareDataPartner>();
            entity.Status = BaseEnums.StatusEnum.Disable;
            entity.SessionState = BaseEnums.SessionState.Disconnected;

            await _partnerRep.UpdateWithDiffLogAsync(entity);
            await _partnerRep.SoftDeleteAsync<ShareDataPartner>(entity);
            await ClearCursorsAsync(entity.Code);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPartner);

            await _activityLogger.LogDeleteAsync(before);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều đối tác (xóa mềm), xóa luôn mốc gửi của các đối tác đó
        /// Created date: 2026-08-04
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task HandleAsync(List<ShareDataDeletePartnerInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _partnerRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            if (await HasAliveSubscriptionAsync(u => u.PartnerId != null && ids.Contains(u.PartnerId)))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerHasActiveSubscription);

            foreach (var id in ids)
                await CloseActiveSessionsAsync(id, ShareDataConst.Reason.PartnerDisabled);

            await _partnerRep.AsUpdateable()
                .SetColumns(u => new ShareDataPartner
                {
                    Status = BaseEnums.StatusEnum.Disable,
                    SessionState = BaseEnums.SessionState.Disconnected
                })
                .Where(u => ids.Contains(u.ID))
                .ExecuteCommandAsync();

            await _partnerRep.SoftDeleteAsync<ShareDataPartner>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPartner);

            foreach (var item in entities)
            {
                await ClearCursorsAsync(item.Code);
                await _activityLogger.LogDeleteAsync(item);
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Mở phiên kết nối tới đối tác.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataSessionOutput> HandleAsync(ShareDataConnectPartnerInput command)
        {
            var partner = await _partnerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (partner.Status == BaseEnums.StatusEnum.Disable)
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerDisabled);

            // Đang có phiên hoạt động thì trả lại luôn, không mở thêm phiên mới.
            var current = await _sessionRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.PartnerId == command.ID && u.State == BaseEnums.SessionFsmState.Active)
                .OrderByDescending(u => u.StartedAt)
                .FirstAsync();

            if (current != null)
            {
                await MarkSessionStateAsync(partner, BaseEnums.SessionState.Connected);
                return ToSessionOutput(current, partner);
            }

            var session = NewSession(partner);
            await _sessionRep.InsertAsync(session);
            await MarkSessionStateAsync(partner, BaseEnums.SessionState.Connected);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSession);

            await _activityLogger.LogActionAsync(BaseEnums.ActivityAction.Connect, partner,
                $"Thiết lập phiên kết nối với đối tác \"{partner.Name}\"", sessionId: session.ID);

            return ToSessionOutput(session, partner);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ngắt phiên kết nối với đối tác.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataDisconnectPartnerInput command)
        {
            var partner = await _partnerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            var reason = string.IsNullOrWhiteSpace(command.Reason)
                ? ShareDataConst.Reason.ClientRequested
                : command.Reason!;

            await CloseActiveSessionsAsync(command.ID, reason);
            await MarkSessionStateAsync(partner, BaseEnums.SessionState.Disconnected);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSession);

            await _activityLogger.LogActionAsync(BaseEnums.ActivityAction.Disconnect, partner,
                $"Ngắt kết nối với đối tác \"{partner.Name}\"", reason: reason);
            await PauseActiveSubscriptionsAsync(partner, null, $"Ngắt kết nối với đối tác \"{partner.Name}\"");
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Tự tạm dừng đăng ký đang chạy của đối tác khi đối tác không còn đáp ứng điều kiện chạy.
        /// Created date: 2026-10-30
        /// Modified date: 17/11/2026
        /// </summary>
        private async Task PauseActiveSubscriptionsAsync(ShareDataPartner partner, BaseEnums.Direction? direction, string reason)
        {
            var subscriptions = await _subscriptionRep.AsQueryable()
                .Where(u => u.IsDelete == null
                    && u.PartnerId == partner.ID
                    && u.State == BaseEnums.SubSubscriptionState.Active)
                .WhereIF(direction != null, u => u.Direction == direction || u.Direction == BaseEnums.Direction.Both)
                .ToListAsync();

            if (subscriptions.Count == 0) return;

            foreach (var subscription in subscriptions)
            {
                subscription.State = BaseEnums.SubSubscriptionState.Paused;
                // Chỉ ghi cột trạng thái: ghi cả dòng sẽ đè số thứ tự, khóa xử lý mà Worker vừa ghi
                await _subscriptionRep.AsUpdateable(subscription)
                    .UpdateColumns(u => new { u.State, u.UpdateTime, u.UpdateUId })
                    .ExecuteCommandAsync();
                await _activityLogger.LogActionAsync(BaseEnums.ActivityAction.Pause, subscription, reason: reason);
            }

            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Xóa mọi mốc gửi theo mã đối tác. Mốc gắn theo mã nên còn sót thì đối tác tạo sau cùng mã đi tiếp từ mốc cũ và mất dữ liệu trước mốc đó.
        /// Created date: 05/11/2026
        /// </summary>
        private async Task ClearCursorsAsync(string? partnerCode)
        {
            var rows = await _lastSendRep.AsQueryable()
                .Where(x => x.PartnerCode == partnerCode)
                .ToListAsync();

            foreach (var row in rows)
            {
                await _lastSendRep.AsDeleteable().Where(x => x.ID == row.ID).ExecuteCommandAsync();
                await _activityLogger.LogDeleteAsync(row);
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Có thay đổi tham số kết nối so với bản ghi gốc hay không
        /// Created date: 2026-08-04
        /// </summary>
        private static bool IsConnectionParamChanged(ShareDataPartner entity, ShareDataUpdatePartnerInput command)
        {
            return entity.Address != command.Address
                || entity.Port != command.Port
                || entity.EndPointApiUrl != command.EndPointApiUrl
                || entity.ConnectionType != command.ConnectionType
                || entity.ConnectionConfig != command.ConnectionConfig;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Còn đăng ký ở trạng thái "sống" (hoạt động ) hay không
        /// Created date: 2026-08-21
        /// </summary>
        private async Task<bool> HasAliveSubscriptionAsync(Expression<Func<ShareDataSubscription, bool>> scope)
        {
            return await _subscriptionRep.AsQueryable()
                .Where(u => u.IsDelete == null
                    && u.State != null
                    && ShareDataConst.SubStateGroup.Alive.Contains(u.State))
                .Where(scope)
                .AnyAsync();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đối tác có phiên đang hoạt động hay không
        /// Created date: 2026-08-04
        /// </summary>
        private async Task<bool> HasActiveSessionAsync(string? partnerId)
        {
            return await _sessionRep.IsAnyAsync(u => u.IsDelete == null
                && u.PartnerId == partnerId
                && u.State == BaseEnums.SessionFsmState.Active);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đóng toàn bộ phiên đang hoạt động của đối tác
        /// Created date: 2026-08-04
        /// </summary>
        private async Task CloseActiveSessionsAsync(string? partnerId, string reason)
        {
            var now = DateTime.Now;

            await _sessionRep.AsUpdateable()
                .SetColumns(u => new ShareDataSession
                {
                    State = BaseEnums.SessionFsmState.Closed,
                    EndedAt = now,
                    EndReason = reason
                })
                .Where(u => u.IsDelete == null
                    && u.PartnerId == partnerId
                    && u.State == BaseEnums.SessionFsmState.Active)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật cờ kết nối trên đối tác. 
        /// Created date: 2026-08-08
        /// </summary>
        private async Task MarkSessionStateAsync(ShareDataPartner partner, BaseEnums.SessionState state)
        {
            if (partner.SessionState == state) return;

            partner.SessionState = state;
            await _partnerRep.AsUpdateable()
                .SetColumns(u => new ShareDataPartner { SessionState = state })
                .Where(u => u.ID == partner.ID)
                .ExecuteCommandAsync();

            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPartner);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Dựng bản ghi phiên mới ở trạng thái ACTIVE cho đối tác
        /// Created date: 2026-08-21
        /// </summary>
        private static ShareDataSession NewSession(ShareDataPartner partner)
        {
            var now = DateTime.Now;

            return new ShareDataSession
            {
                PartnerId = partner.ID,
                State = BaseEnums.SessionFsmState.Active,
                StartedAt = now,
                AcceptedAt = now,
                PacketsSent = 0,
                PacketsRecv = 0,
                LastHeartbeatAt = now,
            };
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Dựng DTO phiên kèm thông tin đối tác
        /// Created date: 2026-08-04
        /// </summary>
        private static ShareDataSessionOutput ToSessionOutput(ShareDataSession session, ShareDataPartner partner)
        {
            var output = session.Adapt<ShareDataSessionOutput>();
            output.PartnerName = partner.Name;
            output.PartnerCode = partner.Code;
            return output;
        }

    }
}
