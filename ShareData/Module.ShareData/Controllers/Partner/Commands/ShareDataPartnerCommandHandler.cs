using Mapster;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Dto.Session;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using System.Security.Cryptography;
using System.Text;
using Wolverine;

namespace Module.ShareData.Controllers.Partner.Commands
{
    /// <summary>
    /// Description: Xử lý thêm / sửa / xóa và thao tác kết nối của Đối tác chia sẻ (ShareDataPartner)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPartnerCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataPartner> _partnerRep;
        private readonly BaseRepository<ShareDataSession> _sessionRep;
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly BaseCacheService _cache;
        private readonly ShareDataActivityLogger _activityLogger;

        public ShareDataPartnerCommandHandler(
            BaseRepository<ShareDataPartner> partnerRep,
            BaseRepository<ShareDataSession> sessionRep,
            BaseRepository<ShareDataSubscription> subscriptionRep,
            BaseCacheService cache,
            ShareDataActivityLogger activityLogger)
        {
            _partnerRep = partnerRep;
            _sessionRep = sessionRep;
            _subscriptionRep = subscriptionRep;
            _cache = cache;
            _activityLogger = activityLogger;
        }

        /// <summary>
        /// Description: Thêm mới 1 đối tác. Trạng thái luôn khởi tạo là CONFIGURED.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataAddPartnerInput command)
        {
            // 1. Kiểm tra trùng mã
            command.Code = command.Code?.Trim();
            if (await _partnerRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, BaseLocaleManager.BaseEntity.Code);

            // 2. Chuẩn hóa giá trị mặc định + băm mật khẩu
            ApplyDefault(command);
            command.Status = ShareDataConst.PartnerStatus.Configured;
            command.PasswordHash = HashPassword(command.Password);
            command.CreateUId = null;

            // 3. Ghi DB + xóa cache
            var entity = command.Adapt<ShareDataPartner>();
            await _partnerRep.InsertWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPartner);

            // 4. Ghi nhật ký cấu hình
            await _activityLogger.LogCreateAsync(ShareDataConst.TargetType.Partner,
                entity.ID, PartnerLabel(entity), entity, entity.ID, entity.Name);
        }

        /// <summary>
        /// Description: Cập nhật 1 đối tác. Password để trống nghĩa là giữ nguyên mật khẩu cũ.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataUpdatePartnerInput command)
        {
            // 1. Kiểm tra trùng mã với bản ghi khác
            command.Code = command.Code?.Trim();
            if (await _partnerRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, BaseLocaleManager.BaseEntity.Code);

            // 2. Lấy bản ghi gốc
            var entity = await _partnerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 3. Đang có phiên hoạt động thì không cho đổi tham số kết nối
            if (IsConnectionParamChanged(entity, command) && await HasActiveSessionAsync(command.ID))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerSessionActive);

            // 4. Chụp lại trạng thái cũ để so sánh khi ghi nhật ký
            var before = entity.Adapt<ShareDataPartner>();

            // 5. Gán giá trị mới lên bản ghi gốc, giữ nguyên mật khẩu nếu không nhập
            ApplyDefault(command);
            entity.Code = command.Code;
            entity.Name = command.Name;
            entity.Address = command.Address;
            entity.Port = command.Port;
            entity.ProtocolProfile = command.ProtocolProfile;
            entity.Username = command.Username;
            entity.InitiatorMode = command.InitiatorMode;
            entity.HeartbeatMaxSec = command.HeartbeatMaxSec;
            entity.DatagramSize = command.DatagramSize;
            entity.ResponseTimeoutSec = command.ResponseTimeoutSec;
            entity.UseTls = command.UseTls;
            entity.OrderNo = command.OrderNo;
            entity.Remark = command.Remark;

            // Không cho set DISABLED qua Update — dùng Delete.
            if (!string.IsNullOrWhiteSpace(command.Status) && command.Status != ShareDataConst.PartnerStatus.Disabled)
                entity.Status = command.Status;

            if (!string.IsNullOrWhiteSpace(command.Password))
                entity.PasswordHash = HashPassword(command.Password);

            // 6. Ghi DB + xóa cache
            await _partnerRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPartner);

            // 7. Ghi nhật ký cấu hình
            await _activityLogger.LogUpdateAsync(ShareDataConst.TargetType.Partner,
                entity.ID, PartnerLabel(entity), before, entity, entity.ID, entity.Name);
        }

        /// <summary>
        /// Description: Xóa 1 đối tác — xóa mềm và đặt trạng thái DISABLED.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataDeletePartnerInput command)
        {
            var entity = await _partnerRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 1. Còn đăng ký đang sống thì không cho xóa
            if (await _subscriptionRep.IsAnyAsync(u => u.IsDelete == null
                    && u.PartnerId == command.ID
                    && u.State != null
                    && ShareDataConst.SubState.Alive.Contains(u.State)))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerHasActiveSubscription);

            // 2. Đang có phiên hoạt động thì đóng phiên lại
            await CloseActiveSessionsAsync(command.ID, ShareDataConst.Reason.PartnerDisabled);

            // 3. Đánh dấu ngừng sử dụng rồi xóa mềm
            var before = entity.Adapt<ShareDataPartner>();
            entity.Status = ShareDataConst.PartnerStatus.Disabled;
            await _partnerRep.UpdateWithDiffLogAsync(entity);
            await _partnerRep.SoftDeleteAsync<ShareDataPartner>(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPartner);

            // 4. Ghi nhật ký cấu hình
            await _activityLogger.LogDeleteAsync(ShareDataConst.TargetType.Partner,
                entity.ID, PartnerLabel(entity), before, entity.ID, entity.Name);
        }

        /// <summary>
        /// Description: Xóa nhiều đối tác (xóa mềm)
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(List<ShareDataDeletePartnerInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _partnerRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            if (await _subscriptionRep.IsAnyAsync(u => u.IsDelete == null
                    && u.PartnerId != null && ids.Contains(u.PartnerId)
                    && u.State != null
                    && ShareDataConst.SubState.Alive.Contains(u.State)))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerHasActiveSubscription);

            foreach (var id in ids)
                await CloseActiveSessionsAsync(id, ShareDataConst.Reason.PartnerDisabled);

            await _partnerRep.AsUpdateable()
                .SetColumns(u => u.Status == ShareDataConst.PartnerStatus.Disabled)
                .Where(u => ids.Contains(u.ID))
                .ExecuteCommandAsync();

            await _partnerRep.SoftDeleteAsync<ShareDataPartner>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataPartner);

            foreach (var item in entities)
                await _activityLogger.LogDeleteAsync(ShareDataConst.TargetType.Partner,
                    item.ID, PartnerLabel(item), item, item.ID, item.Name);
        }

        /// <summary>
        /// Description: Mở phiên kết nối tới đối tác.
        ///              GIAI ĐOẠN NÀY LÀ BẢN MÔ PHỎNG — chưa mở socket TCP/ASN.1 thật,
        ///              chỉ ghi bản ghi phiên ở trạng thái ACTIVE để màn hình vận hành chạy được.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataSessionOutput> HandleAsync(ShareDataConnectPartnerInput command)
        {
            var partner = await _partnerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            if (partner.Status == ShareDataConst.PartnerStatus.Disabled)
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerDisabled);

            // Đang có phiên hoạt động thì trả lại luôn, không mở thêm phiên mới.
            var current = await _sessionRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.PartnerId == command.ID && u.State == ShareDataConst.SessionState.Active)
                .OrderByDescending(u => u.StartedAt)
                .FirstAsync();

            if (current != null)
                return ToSessionOutput(current, partner);

            var now = DateTime.Now;
            var session = new ShareDataSession
            {
                PartnerId = partner.ID,
                Direction = partner.InitiatorMode == ShareDataConst.InitiatorMode.ServerInitiated
                    ? ShareDataConst.SessionDirection.In
                    : ShareDataConst.SessionDirection.Out,
                State = ShareDataConst.SessionState.Active,
                StartedAt = now,
                AcceptedAt = now,
                PacketsSent = 0,
                PacketsRecv = 0,
                LastHeartbeatAt = now,
                HeartbeatRttMs = 0,
                NegotiatedDatagramSize = partner.DatagramSize
            };

            await _sessionRep.InsertAsync(session);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSession);

            await _activityLogger.LogActionAsync(ShareDataConst.ActivityAction.Connect,
                ShareDataConst.TargetType.Partner, partner.ID, PartnerLabel(partner),
                $"Thiết lập phiên kết nối với đối tác \"{partner.Name}\"",
                partner.ID, partner.Name, sessionId: session.ID);

            return ToSessionOutput(session, partner);
        }

        /// <summary>
        /// Description: Ngắt phiên kết nối với đối tác.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataDisconnectPartnerInput command)
        {
            var partner = await _partnerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var reason = string.IsNullOrWhiteSpace(command.Reason)
                ? ShareDataConst.Reason.ClientRequested
                : command.Reason!;

            await CloseActiveSessionsAsync(command.ID, reason);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSession);

            await _activityLogger.LogActionAsync(ShareDataConst.ActivityAction.Disconnect,
                ShareDataConst.TargetType.Partner, partner.ID, PartnerLabel(partner),
                $"Ngắt kết nối với đối tác \"{partner.Name}\"",
                partner.ID, partner.Name, reason: reason);
        }

        /// <summary>
        /// Description: Thử kết nối tới đối tác (Initiate + Login + Logout).
        ///              GIAI ĐOẠN NÀY LÀ BẢN MÔ PHỎNG — chỉ kiểm tra cấu hình, chưa bắt tay giao thức thật.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<List<ShareDataTestConnectionStepOutput>> HandleAsync(ShareDataTestConnectionPartnerInput command)
        {
            var partner = await _partnerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var hasEndpoint = !string.IsNullOrWhiteSpace(partner.Address) && partner.Port.HasValue;
            var hasCredential = !string.IsNullOrWhiteSpace(partner.Username) && !string.IsNullOrWhiteSpace(partner.PasswordHash);

            return new List<ShareDataTestConnectionStepOutput>
            {
                new() { Step = "initiate", Ok = hasEndpoint },
                new() { Step = "login", Ok = hasEndpoint && hasCredential },
                new() { Step = "logout", Ok = hasEndpoint && hasCredential }
            };
        }

        /// <summary>
        /// Description: Nhãn hiển thị đối tác trong nhật ký — dạng "QG-01 - Trung tâm QLĐHGT Quốc gia"
        /// Created date: 2026-08-05
        /// </summary>
        private static string PartnerLabel(ShareDataPartner partner)
        {
            if (string.IsNullOrWhiteSpace(partner.Code)) return partner.Name ?? partner.ID ?? string.Empty;
            return string.IsNullOrWhiteSpace(partner.Name) ? partner.Code! : $"{partner.Code} - {partner.Name}";
        }

        /// <summary>
        /// Description: Gán giá trị mặc định cho các tham số kết nối chưa nhập
        /// Created date: 2026-08-04
        /// </summary>
        private static void ApplyDefault(ShareDataAddPartnerInput command)
        {
            command.Name = command.Name?.Trim();
            command.Address = command.Address?.Trim();
            command.Username = command.Username?.Trim();
            command.ProtocolProfile ??= ShareDataConst.ProtocolProfile.Asn;
            command.InitiatorMode ??= ShareDataConst.InitiatorMode.ClientInitiated;
            command.HeartbeatMaxSec ??= 30;
            command.DatagramSize ??= 4096;
            command.ResponseTimeoutSec ??= 10;
            command.UseTls ??= false;
            command.OrderNo ??= 100;
        }

        /// <summary>
        /// Description: Có thay đổi tham số kết nối so với bản ghi gốc hay không
        /// Created date: 2026-08-04
        /// </summary>
        private static bool IsConnectionParamChanged(ShareDataPartner entity, ShareDataUpdatePartnerInput command)
        {
            return entity.Address != command.Address
                || entity.Port != command.Port
                || entity.Username != command.Username
                || entity.ProtocolProfile != command.ProtocolProfile
                || !string.IsNullOrWhiteSpace(command.Password);
        }

        /// <summary>
        /// Description: Đối tác có phiên đang hoạt động hay không
        /// Created date: 2026-08-04
        /// </summary>
        private async Task<bool> HasActiveSessionAsync(string? partnerId)
        {
            return await _sessionRep.IsAnyAsync(u => u.IsDelete == null
                && u.PartnerId == partnerId
                && u.State == ShareDataConst.SessionState.Active);
        }

        /// <summary>
        /// Description: Đóng toàn bộ phiên đang hoạt động của đối tác
        /// Created date: 2026-08-04
        /// </summary>
        private async Task CloseActiveSessionsAsync(string? partnerId, string reason)
        {
            var now = DateTime.Now;

            await _sessionRep.AsUpdateable()
                .SetColumns(u => new ShareDataSession
                {
                    State = ShareDataConst.SessionState.Closed,
                    EndedAt = now,
                    EndReason = reason
                })
                .Where(u => u.IsDelete == null
                    && u.PartnerId == partnerId
                    && u.State == ShareDataConst.SessionState.Active)
                .ExecuteCommandAsync();
        }

        /// <summary>
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

        /// <summary>
        /// Description: Băm mật khẩu đối tác bằng SHA-256 (Base64)
        /// Created date: 2026-08-04
        /// </summary>
        private static string? HashPassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return null;

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
