using Mapster;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services;
using Newtonsoft.Json;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.ShareData.Controllers.Subscription.Commands
{
    /// <summary>
    /// Description: Xử lý thêm / sửa / xóa và chuyển trạng thái Đăng ký chia sẻ (ShareDataSubscription)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataSubscriptionCommandHandler : IWolverineHandler
    {
        private static readonly SemaphoreSlim SerialLock = new(1, 1);

        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly BaseRepository<ShareDataPartner> _partnerRep;
        private readonly BaseRepository<ShareDataMappingProfile> _mappingRep;
        private readonly BaseRepository<ShareDataEventSource> _eventSourceRep;
        private readonly BaseCacheService _cache;
        private readonly ShareDataActivityLogger _activityLogger;

        public ShareDataSubscriptionCommandHandler(
            BaseRepository<ShareDataSubscription> subscriptionRep,
            BaseRepository<ShareDataPartner> partnerRep,
            BaseRepository<ShareDataMappingProfile> mappingRep,
            BaseRepository<ShareDataEventSource> eventSourceRep,
            BaseCacheService cache,
            ShareDataActivityLogger activityLogger)
        {
            _subscriptionRep = subscriptionRep;
            _partnerRep = partnerRep;
            _mappingRep = mappingRep;
            _eventSourceRep = eventSourceRep;
            _cache = cache;
            _activityLogger = activityLogger;
        }

        /// <summary>
        /// Description: Thêm mới 1 đăng ký chia sẻ.
        ///              Backend tự sinh SerialNbr, đặt State = PENDING và tự suy lại hồ sơ ánh xạ.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataSubscriptionOutput> HandleAsync(ShareDataAddSubscriptionInput command)
        {
            ApplyDefault(command);
            await ValidateBusinessAsync(command);

            // 1. Chặn trùng (đối tác × gói tin × chiều) khi còn đăng ký đang sống
            if (await _subscriptionRep.IsAnyAsync(u => u.IsDelete == null
                    && u.PartnerId == command.PartnerId
                    && u.DatatypeId == command.DatatypeId
                    && u.Direction == command.Direction
                    && u.State != null
                    && ShareDataConst.SubState.Alive.Contains(u.State)))
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionDuplicated);

            // 2. Tự suy hồ sơ ánh xạ, không tin giá trị client gửi lên
            await ResolveMappingAsync(command);

            var entity = command.Adapt<ShareDataSubscription>();
            entity.ScheduleJson = SerializeSchedule(command);
            entity.State = ShareDataConst.SubState.Pending;
            entity.RequestedAt = DateTime.Now;
            entity.ResolvedAt = null;
            entity.ResolvedBy = null;
            entity.SessionId = null;
            entity.CreateUId = null;

            // 3. Sinh serial tăng dần theo đối tác
            await SerialLock.WaitAsync();
            try
            {
                entity.SerialNbr = await NextSerialAsync(command.PartnerId);
                await _subscriptionRep.InsertWithDiffLogAsync(entity);
            }
            finally
            {
                SerialLock.Release();
            }

            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            var output = await ToOutputAsync(entity);
            await _activityLogger.LogCreateAsync(ShareDataConst.TargetType.Subscription,
                entity.ID, SubscriptionLabel(entity), entity, entity.PartnerId, output.PartnerName);

            return output;
        }

        /// <summary>
        /// Description: Cập nhật 1 đăng ký chia sẻ.
        ///              Không cho đổi PartnerId / DatatypeId / Direction / SerialNbr / State.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataSubscriptionOutput> HandleAsync(ShareDataUpdateSubscriptionInput command)
        {
            var entity = await _subscriptionRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 1. Đăng ký đã kết thúc thì không sửa được
            if (entity.State != null && ShareDataConst.SubState.Finished.Contains(entity.State))
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionFinished);

            // 2. Đang chạy thì yêu cầu tắt trước (giai đoạn này chưa đàm phán lại với đối tác)
            if (entity.State == ShareDataConst.SubState.Active)
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionMustPauseBeforeUpdate);

            // 3. Chụp lại trạng thái cũ để so sánh khi ghi nhật ký
            var before = entity.Adapt<ShareDataSubscription>();

            // 4. Giữ nguyên các trường định danh, chỉ nhận phần cấu hình gửi
            command.PartnerId = entity.PartnerId;
            command.DatatypeId = entity.DatatypeId;
            command.Direction = entity.Direction;

            ApplyDefault(command);
            await ValidateBusinessAsync(command);
            await ResolveMappingAsync(command);

            entity.Mode = command.Mode;
            entity.ScheduleJson = SerializeSchedule(command);
            entity.Format = command.Format;
            entity.Priority = command.Priority;
            entity.Guaranteed = command.Guaranteed;
            entity.Persistent = command.Persistent;
            entity.EventSourceId = command.Mode == ShareDataConst.SubMode.Event ? command.EventSourceId : null;
            entity.DebounceSec = command.Mode == ShareDataConst.SubMode.Event ? command.DebounceSec : null;
            entity.DataSourceId = command.DataSourceId;
            entity.MappingProfileId = command.MappingProfileId;
            entity.Remark = command.Remark;

            await _subscriptionRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            var output = await ToOutputAsync(entity);
            await _activityLogger.LogUpdateAsync(ShareDataConst.TargetType.Subscription,
                entity.ID, SubscriptionLabel(entity), before, entity, entity.PartnerId, output.PartnerName);

            return output;
        }

        /// <summary>
        /// Description: Xóa 1 đăng ký (xóa mềm). Đang chạy thì hủy trước rồi mới xóa.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataDeleteSubscriptionInput command)
        {
            var entity = await _subscriptionRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var before = entity.Adapt<ShareDataSubscription>();

            if (entity.State != null && ShareDataConst.SubState.Alive.Contains(entity.State))
            {
                entity.State = ShareDataConst.SubState.Cancelled;
                entity.CancelReason = ShareDataConst.Reason.DeletedByUser;
                entity.ResolvedAt = DateTime.Now;
                await _subscriptionRep.UpdateWithDiffLogAsync(entity);
            }

            await _subscriptionRep.SoftDeleteAsync<ShareDataSubscription>(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            await _activityLogger.LogDeleteAsync(ShareDataConst.TargetType.Subscription,
                entity.ID, SubscriptionLabel(entity), before, entity.PartnerId, await PartnerNameAsync(entity.PartnerId));
        }

        /// <summary>
        /// Description: Xóa nhiều đăng ký (xóa mềm)
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(List<ShareDataDeleteSubscriptionInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _subscriptionRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            var now = DateTime.Now;
            await _subscriptionRep.AsUpdateable()
                .SetColumns(u => new ShareDataSubscription
                {
                    State = ShareDataConst.SubState.Cancelled,
                    CancelReason = ShareDataConst.Reason.DeletedByUser,
                    ResolvedAt = now
                })
                .Where(u => ids.Contains(u.ID) && u.State != null && ShareDataConst.SubState.Alive.Contains(u.State))
                .ExecuteCommandAsync();

            await _subscriptionRep.SoftDeleteAsync<ShareDataSubscription>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            foreach (var item in entities)
                await _activityLogger.LogDeleteAsync(ShareDataConst.TargetType.Subscription,
                    item.ID, SubscriptionLabel(item), item, item.PartnerId, await PartnerNameAsync(item.PartnerId));
        }

        /// <summary>
        /// Description: Tắt tạm 1 đăng ký (ACTIVE → PAUSED)
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataSubscriptionOutput> HandleAsync(ShareDataPauseSubscriptionInput command)
        {
            return await ChangeStateAsync(command.ID, ShareDataConst.SubState.Paused,
                new[] { ShareDataConst.SubState.Active }, ShareDataConst.ActivityAction.Pause);
        }

        /// <summary>
        /// Description: Bật lại 1 đăng ký (PAUSED → ACTIVE)
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataSubscriptionOutput> HandleAsync(ShareDataResumeSubscriptionInput command)
        {
            return await ChangeStateAsync(command.ID, ShareDataConst.SubState.Active,
                new[] { ShareDataConst.SubState.Paused }, ShareDataConst.ActivityAction.Resume);
        }

        /// <summary>
        /// Description: Duyệt 1 đăng ký INBOUND (PENDING → ACTIVE)
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataSubscriptionOutput> HandleAsync(ShareDataApproveSubscriptionInput command)
        {
            return await ChangeStateAsync(command.ID, ShareDataConst.SubState.Active,
                new[] { ShareDataConst.SubState.Pending }, ShareDataConst.ActivityAction.Approve, markResolved: true);
        }

        /// <summary>
        /// Description: Hủy 1 đăng ký chia sẻ
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataCancelSubscriptionInput command)
        {
            var entity = await GetForTransitionAsync(command.ID, ShareDataConst.SubState.Cancelled,
                ShareDataConst.SubState.Alive);

            entity.State = ShareDataConst.SubState.Cancelled;
            entity.CancelReason = string.IsNullOrWhiteSpace(command.Reason) ? ShareDataConst.Reason.Other : command.Reason;
            entity.ResolvedAt = DateTime.Now;

            await _subscriptionRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            await _activityLogger.LogActionAsync(ShareDataConst.ActivityAction.Cancel,
                ShareDataConst.TargetType.Subscription, entity.ID, SubscriptionLabel(entity),
                $"Hủy {SubscriptionLabel(entity)}",
                entity.PartnerId, await PartnerNameAsync(entity.PartnerId),
                subscriptionId: entity.ID, reason: entity.CancelReason);
        }

        /// <summary>
        /// Description: Từ chối 1 đăng ký INBOUND đang chờ duyệt
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataRejectSubscriptionInput command)
        {
            var entity = await GetForTransitionAsync(command.ID, ShareDataConst.SubState.Rejected,
                new[] { ShareDataConst.SubState.Pending });

            entity.State = ShareDataConst.SubState.Rejected;
            entity.RejectReason = string.IsNullOrWhiteSpace(command.Reason) ? ShareDataConst.Reason.Other : command.Reason;
            entity.ResolvedAt = DateTime.Now;

            await _subscriptionRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            await _activityLogger.LogActionAsync(ShareDataConst.ActivityAction.Reject,
                ShareDataConst.TargetType.Subscription, entity.ID, SubscriptionLabel(entity),
                $"Từ chối {SubscriptionLabel(entity)}",
                entity.PartnerId, await PartnerNameAsync(entity.PartnerId),
                subscriptionId: entity.ID, reason: entity.RejectReason);
        }

        /// <summary>
        /// Description: Gán giá trị mặc định cho các trường chưa nhập
        /// Created date: 2026-08-04
        /// </summary>
        private static void ApplyDefault(ShareDataAddSubscriptionInput command)
        {
            command.Direction ??= ShareDataConst.SubDirection.Outbound;
            command.Format ??= ShareDataConst.PublishFormat.Data;
            command.Priority ??= 5;
            command.Guaranteed ??= true;
            command.Persistent ??= true;

            if (command.Mode != ShareDataConst.SubMode.Event)
            {
                command.EventSourceId = null;
                command.DebounceSec = null;
            }
            else
            {
                command.DebounceSec ??= 30;
            }

            if (command.Mode != ShareDataConst.SubMode.Periodic)
                command.Schedule = null;
        }

        /// <summary>
        /// Description: Kiểm tra ràng buộc nghiệp vụ chung cho Add và Update
        /// Created date: 2026-08-04
        /// </summary>
        private async Task ValidateBusinessAsync(ShareDataAddSubscriptionInput command)
        {
            // 1. Đối tác phải tồn tại và còn sử dụng
            var partner = await _partnerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.PartnerId)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, BaseMsg.ShareData.Entity.PartnerId);

            if (partner.Status == ShareDataConst.PartnerStatus.Disabled)
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerDisabled);

            // 2. Loại dữ liệu chia sẻ phải nằm trong 101–111
            if (!ShareDataConst.Datatype.IsValid(command.DatatypeId))
                throw Oops.Oh(BaseMsg.ShareData.Validation.DatatypeIdOutOfRange);

            // 3. Chế độ EVENT bắt buộc có nguồn sự kiện hợp lệ
            if (command.Mode == ShareDataConst.SubMode.Event)
            {
                if (string.IsNullOrWhiteSpace(command.EventSourceId))
                    throw Oops.Oh(BaseMsg.ShareData.Validation.EventSourceRequired);

                if (!await _eventSourceRep.IsAnyAsync(u => u.IsDelete == null && u.ID == command.EventSourceId))
                    throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, BaseMsg.ShareData.Entity.EventSourceId);
            }

            // 4. Chế độ PERIODIC bắt buộc có lịch gửi
            if (command.Mode == ShareDataConst.SubMode.Periodic && command.Schedule == null)
                throw Oops.Oh(BaseMsg.ShareData.Validation.ScheduleRequired);
        }

        /// <summary>
        /// Description: Suy hồ sơ ánh xạ đang áp dụng theo (đối tác × gói tin × chiều).
        ///              Chiều OUTBOUND bắt buộc phải có ánh xạ mới xuất được dữ liệu.
        /// Created date: 2026-08-04
        /// </summary>
        private async Task ResolveMappingAsync(ShareDataAddSubscriptionInput command)
        {
            var mappingDirection = command.Direction == ShareDataConst.SubDirection.Inbound
                ? ShareDataConst.MappingDirection.In
                : ShareDataConst.MappingDirection.Out;

            var mapping = await _mappingRep.GetFirstAsync(u => u.IsDelete == null
                && u.VendorId == command.PartnerId
                && u.DatatypeId == command.DatatypeId
                && u.Direction == mappingDirection
                && u.IsActive == true);

            if (mapping == null)
            {
                if (mappingDirection == ShareDataConst.MappingDirection.Out)
                    throw Oops.Oh(BaseMsg.ShareData.Exception.MappingNotResolved);

                command.MappingProfileId = null;
                command.DataSourceId = null;
                return;
            }

            command.MappingProfileId = mapping.ID;
            command.DataSourceId = mapping.DataSourceId;
        }

        /// <summary>
        /// Description: Sinh serial number kế tiếp trong phạm vi 1 đối tác
        /// Created date: 2026-08-04
        /// </summary>
        private async Task<int> NextSerialAsync(string? partnerId)
        {
            var last = await _subscriptionRep.AsQueryable()
                .Where(u => u.PartnerId == partnerId)
                .OrderBy(u => u.SerialNbr, OrderByType.Desc)
                .Select(u => u.SerialNbr)
                .FirstAsync();

            return (last ?? 0) + 1;
        }

        /// <summary>
        /// Description: Chuyển trạng thái đăng ký, kiểm tra trạng thái nguồn hợp lệ
        /// Created date: 2026-08-04
        /// </summary>
        private async Task<ShareDataSubscriptionOutput> ChangeStateAsync(
            string? id, string target, string[] allowedFrom, string action, bool markResolved = false)
        {
            var entity = await GetForTransitionAsync(id, target, allowedFrom);

            entity.State = target;
            if (markResolved)
            {
                entity.ResolvedAt = DateTime.Now;
                entity.ResolvedBy = App.User == null ? "system" : App.User.GetUsername();
            }

            await _subscriptionRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            var output = await ToOutputAsync(entity);
            await _activityLogger.LogActionAsync(action,
                ShareDataConst.TargetType.Subscription, entity.ID, SubscriptionLabel(entity),
                $"{ActionLabel(action)} {SubscriptionLabel(entity)}",
                entity.PartnerId, output.PartnerName, subscriptionId: entity.ID);

            return output;
        }

        /// <summary>
        /// Description: Lấy bản ghi và kiểm tra trạng thái nguồn có được phép chuyển tiếp không
        /// Created date: 2026-08-04
        /// </summary>
        private async Task<ShareDataSubscription> GetForTransitionAsync(string? id, string target, string[] allowedFrom)
        {
            var entity = await _subscriptionRep.GetFirstAsync(u => u.IsDelete == null && u.ID == id)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            if (entity.State == null || !allowedFrom.Contains(entity.State))
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionStateInvalid, entity.State ?? string.Empty, target);

            return entity;
        }

        /// <summary>
        /// Description: Dựng DTO trả về kèm tên đối tác và lịch gửi đã giải mã
        /// Created date: 2026-08-04
        /// </summary>
        private async Task<ShareDataSubscriptionOutput> ToOutputAsync(ShareDataSubscription entity)
        {
            var output = entity.Adapt<ShareDataSubscriptionOutput>();

            var partner = await _partnerRep.GetFirstAsync(u => u.ID == entity.PartnerId);
            output.PartnerName = partner?.Name;
            output.PartnerCode = partner?.Code;

            if (!string.IsNullOrWhiteSpace(entity.ScheduleJson))
            {
                try
                {
                    output.Schedule = JsonConvert.DeserializeObject<ShareDataScheduleDto>(entity.ScheduleJson!);
                }
                catch
                {
                    output.Schedule = null;
                }
            }

            return output;
        }

        /// <summary>
        /// Description: Nhãn hiển thị đăng ký trong nhật ký — dạng "đăng ký gửi #12 loại 101"
        /// Created date: 2026-08-05
        /// </summary>
        private static string SubscriptionLabel(ShareDataSubscription entity)
        {
            var direction = entity.Direction == ShareDataConst.SubDirection.Inbound ? "nhận" : "gửi";
            return $"đăng ký {direction} #{entity.SerialNbr} loại {entity.DatatypeId}";
        }

        /// <summary>
        /// Description: Nhãn tiếng Việt của hành động, dùng dựng câu mô tả nhật ký
        /// Created date: 2026-08-05
        /// </summary>
        private static string ActionLabel(string action) => action switch
        {
            ShareDataConst.ActivityAction.Pause => "Tắt",
            ShareDataConst.ActivityAction.Resume => "Bật lại",
            ShareDataConst.ActivityAction.Approve => "Duyệt",
            ShareDataConst.ActivityAction.Reject => "Từ chối",
            ShareDataConst.ActivityAction.Cancel => "Hủy",
            _ => "Thao tác trên"
        };

        /// <summary>
        /// Description: Lấy tên đối tác để ghi kèm nhật ký, tránh phải join khi tra cứu
        /// Created date: 2026-08-05
        /// </summary>
        private async Task<string?> PartnerNameAsync(string? partnerId)
        {
            if (string.IsNullOrWhiteSpace(partnerId)) return null;

            var partner = await _partnerRep.GetFirstAsync(u => u.ID == partnerId);
            return partner?.Name;
        }

        /// <summary>
        /// Description: Chuyển lịch gửi thành chuỗi JSON để lưu DB
        /// Created date: 2026-08-04
        /// </summary>
        private static string? SerializeSchedule(ShareDataAddSubscriptionInput command)
        {
            if (command.Mode != ShareDataConst.SubMode.Periodic || command.Schedule == null)
                return null;

            command.Schedule.Kind ??= ShareDataConst.ScheduleKind.Continuous;
            command.Schedule.UpdateDelaySec ??= 30;

            return JsonConvert.SerializeObject(command.Schedule);
        }
    }
}
