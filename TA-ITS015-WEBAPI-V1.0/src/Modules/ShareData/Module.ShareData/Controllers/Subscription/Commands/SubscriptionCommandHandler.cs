using Mapster;
using Microsoft.Extensions.Localization;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataActivityService;
using Module.ShareData.Infrastructure.Services.ShareDataConfig;
using Module.ShareData.Infrastructure.Services.ShareDataMappingResolverService;
using Newtonsoft.Json;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.ShareData.Controllers.Subscription.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa và chuyển trạng thái Đăng ký chia sẻ (ShareDataSubscription)
    /// Created date: 2026-08-04
    /// </summary>
    public class SubscriptionCommandHandler : IWolverineHandler
    {
        private static readonly SemaphoreSlim SerialLock = new(1, 1);
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly BaseRepository<ShareDataPartner> _partnerRep;
        private readonly BaseRepository<ShareDataSession> _sessionRep;
        private readonly BaseCacheService _cache;
        private readonly ShareDataActivityLogger _activityLogger;
        private readonly ShareDataMappingResolver _mappingResolver;
        private readonly ConfigService _configService;
        private readonly IStringLocalizer _localizer;

        public SubscriptionCommandHandler(
            BaseRepository<ShareDataSubscription> subscriptionRep,
            BaseRepository<ShareDataPartner> partnerRep,
            BaseRepository<ShareDataSession> sessionRep,
            BaseCacheService cache,
            ShareDataActivityLogger activityLogger,
            ShareDataMappingResolver mappingResolver,
            ConfigService configService,
            IStringLocalizer localizer)
        {
            _subscriptionRep = subscriptionRep;
            _partnerRep = partnerRep;
            _sessionRep = sessionRep;
            _cache = cache;
            _activityLogger = activityLogger;
            _mappingResolver = mappingResolver;
            _configService = configService;
            _localizer = localizer;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 đăng ký chia sẻ.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataAddSubscriptionInput command)
        {
            command.ScheduleJson = SerializeSchedule(command);
            await ValidateDynamicLimits(
                command.IntervalSeconds,
                command.Priority,
                command.DebounceSec);

            var partner = await _partnerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.PartnerId)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PartnerId]);

            if (partner.Status == BaseEnums.StatusEnum.Disable)
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerDisabled);

            if (await _subscriptionRep.IsAnyAsync(u => u.IsDelete == null
                    && u.PartnerId == command.PartnerId
                    && u.Direction == command.Direction
                    && u.DatatypeId == command.DatatypeId
                    && u.State != null
                    && ShareDataConst.SubStateGroup.Alive.Contains(u.State)))
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionDuplicated);

            var activeSession = await _sessionRep.GetFirstAsync(u => u.IsDelete == null
                && u.PartnerId == command.PartnerId
                && u.State == BaseEnums.SessionFsmState.Active);

            var partnerConnected = activeSession != null
                || await IsPartnerConnectedAsync(command.PartnerId);

            // Đăng ký chỉ được chạy khi đã có hồ sơ ánh xạ đang bật. Tạo mới thì KHÔNG báo lỗi —
            // luồng nghiệp vụ là có đăng ký trước, tạo hồ sơ sau — nên đặt luôn trạng thái Tạm dừng.
            // Người dùng sang màn Ánh xạ tạo và bật hồ sơ, xong quay lại bật đăng ký.
            var mappingState = (await _mappingResolver.ResolveAsync(command.PartnerId, command.DatatypeId, command.Direction)).State;
            var mappingReady = mappingState == BaseEnums.SubMappingState.Ready;

            command.Mode ??= BaseEnums.SubMode.Periodic;
            command.State = !mappingReady
                ? BaseEnums.SubSubscriptionState.Paused
                : partnerConnected
                    ? BaseEnums.SubSubscriptionState.Active
                    : BaseEnums.SubSubscriptionState.Pending;
            command.RequestedAt = DateTime.Now;
            command.CreateUId = null;
            if (mappingReady && partnerConnected)
            {
                command.SessionId = activeSession?.ID;
                command.ResolvedAt = DateTime.Now;
            }
            if (command.Mode != BaseEnums.SubMode.Event)
            {
                command.EventSourceId = null;
                command.DebounceSec = null;
            }

            // Chiều nhận không chủ động gửi nên cờ "gửi ngay khi có dữ liệu mới" không có nghĩa.
            if (command.Direction == BaseEnums.Direction.Inbound) command.SendOnNewData = null;

            ShareDataSubscription entity;
            await SerialLock.WaitAsync();
            try
            {
                command.SerialNbr = await NextSerialAsync(command.PartnerId);
                entity = command.Adapt<ShareDataSubscription>();
                await _subscriptionRep.InsertWithDiffLogAsync(entity);
            }
            finally
            {
                SerialLock.Release();
            }
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            await _activityLogger.LogCreateAsync(entity);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 đăng ký chia sẻ.
        /// Created date: 2026-08-04
        /// Modified date: 05/11/2026
        /// </summary>
        public async Task HandleAsync(ShareDataUpdateSubscriptionInput command)
        {
            command.ScheduleJson = SerializeSchedule(command);
            await ValidateDynamicLimits(
                command.IntervalSeconds,
                command.Priority,
                command.DebounceSec);

            var entity = await _subscriptionRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            if (entity.State != null && ShareDataConst.SubStateGroup.Finished.Contains(entity.State))
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionFinished);

            if (entity.State == BaseEnums.SubSubscriptionState.Active)
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionMustPauseBeforeUpdate);

            var before = entity.Adapt<ShareDataSubscription>();
            command.PartnerId = entity.PartnerId;
            command.Direction = entity.Direction;
            command.DatatypeId = entity.DatatypeId;

            var partner = await _partnerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.PartnerId)
              ?? throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PartnerId]);

            if (partner.Status == BaseEnums.StatusEnum.Disable)
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerDisabled);

            entity.ScheduleJson = command.ScheduleJson;
            // Lịch vừa đổi thì mốc chạy kế tiếp cũ do worker ghi không còn đúng nữa:
            // xóa đi để worker tính lại theo lịch mới ngay nhịp quét gần nhất,
            // nếu không đăng ký sẽ nằm im tới mốc cũ (ví dụ đẩy sang 06:00 hôm sau).
            entity.NextTimeRun = null;
            entity.IntervalSeconds = command.IntervalSeconds;
            entity.Mode = command.Mode;
            entity.Format = command.Format;
            entity.Priority = command.Priority;
            entity.EventSourceId = command.Mode == BaseEnums.SubMode.Event ? command.EventSourceId : null;
            entity.DebounceSec = command.Mode == BaseEnums.SubMode.Event ? command.DebounceSec : null;
            entity.SendOnNewData = command.Direction == BaseEnums.Direction.Inbound ? null : command.SendOnNewData;
            entity.Remark = command.Remark;

            // Ghi cả cột null: mặc định cột null bị bỏ qua thì NextTimeRun không được xóa
            await _subscriptionRep.UpdateWithDiffLogAsync(entity, false);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            await _activityLogger.LogUpdateAsync(before, entity);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 đăng ký (xóa mềm)
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataDeleteSubscriptionInput command)
        {
            var entity = await _subscriptionRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Đăng ký đang chạy thì phải tắt trước rồi mới xóa — tránh xóa mất đăng ký mà
            // service vẫn đang gửi/nhận theo nó.
            if (entity.State == BaseEnums.SubSubscriptionState.Active)
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionMustPauseBeforeDelete);

            var before = entity.Adapt<ShareDataSubscription>();

            if (entity.State != null && ShareDataConst.SubStateGroup.Alive.Contains(entity.State))
            {
                entity.State = BaseEnums.SubSubscriptionState.Cancelled;
                entity.ResolvedAt = DateTime.Now;
                await _subscriptionRep.UpdateWithDiffLogAsync(entity);
            }

            await _subscriptionRep.SoftDeleteAsync<ShareDataSubscription>(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            await _activityLogger.LogDeleteAsync(before);
        }

        /// <summary>
        /// Author: HieuNV
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

            // Cùng luật với xóa một bản ghi: đăng ký đang chạy phải tắt trước.
            if (entities.Any(u => u.State == BaseEnums.SubSubscriptionState.Active))
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionMustPauseBeforeDelete);

            var now = DateTime.Now;
            await _subscriptionRep.AsUpdateable()
                .SetColumns(u => new ShareDataSubscription
                {
                    State = BaseEnums.SubSubscriptionState.Cancelled,
                    ResolvedAt = now
                })
                .Where(u => ids.Contains(u.ID) && u.State != null && ShareDataConst.SubStateGroup.Alive.Contains(u.State))
                .ExecuteCommandAsync();

            await _subscriptionRep.SoftDeleteAsync<ShareDataSubscription>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            foreach (var item in entities)
                await _activityLogger.LogDeleteAsync(item);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Tắt tạm 1 đăng ký
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataPauseSubscriptionInput command)
        {
            await ChangeStateAsync(command.ID, BaseEnums.SubSubscriptionState.Paused,
                new BaseEnums.SubSubscriptionState?[] { BaseEnums.SubSubscriptionState.Active, BaseEnums.SubSubscriptionState.Pending },
                BaseEnums.ActivityAction.Pause);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Bật lại 1 đăng ký
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataResumeSubscriptionInput command)
        {
            var entity = await _subscriptionRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            if (!await IsPartnerConnectedAsync(entity.PartnerId))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerNotConnected);

            // Cổng chặn: không có hồ sơ ánh xạ đang bật thì bật đăng ký lên cũng vô nghĩa — worker
            // sẽ huỷ kết xuất và ghi cảnh báo ESH-1304. Phải tra SỐNG tại đây, không đọc bản chụp.
            await EnsureMappingReadyAsync(entity);

            await ChangeStateAsync(command.ID, BaseEnums.SubSubscriptionState.Active,
                new BaseEnums.SubSubscriptionState?[] { BaseEnums.SubSubscriptionState.Paused, BaseEnums.SubSubscriptionState.Pending },
                BaseEnums.ActivityAction.Resume, markResolved: true);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Duyệt 1 đăng ký INBOUND
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataApproveSubscriptionInput command)
        {
            var entity = await _subscriptionRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Duyệt cũng là một đường đưa đăng ký sang Hoạt động nên phải qua cùng cổng chặn.
            await EnsureMappingReadyAsync(entity);

            await ChangeStateAsync(command.ID, BaseEnums.SubSubscriptionState.Active,
                new BaseEnums.SubSubscriptionState?[] { BaseEnums.SubSubscriptionState.Pending },
                BaseEnums.ActivityAction.Approve, markResolved: true);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Hủy 1 đăng ký chia sẻ
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataCancelSubscriptionInput command)
        {
            var entity = await GetForTransitionAsync(command.ID, BaseEnums.SubSubscriptionState.Cancelled,
                ShareDataConst.SubStateGroup.Alive);

            entity.State = BaseEnums.SubSubscriptionState.Cancelled;
            entity.ResolvedAt = DateTime.Now;

            await UpdateResolvedStateAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            await _activityLogger.LogActionAsync(BaseEnums.ActivityAction.Cancel, entity);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Từ chối 1 đăng ký INBOUND đang chờ duyệt
        /// Created date: 2026-08-04
        /// </summary>
        public async Task HandleAsync(ShareDataRejectSubscriptionInput command)
        {
            var entity = await GetForTransitionAsync(command.ID, BaseEnums.SubSubscriptionState.Rejected,
                new BaseEnums.SubSubscriptionState?[] { BaseEnums.SubSubscriptionState.Pending });

            entity.State = BaseEnums.SubSubscriptionState.Rejected;
            entity.ResolvedAt = DateTime.Now;

            await UpdateResolvedStateAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            await _activityLogger.LogActionAsync(BaseEnums.ActivityAction.Reject, entity);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghi trạng thái kết thúc (Hủy, Từ chối): chỉ ghi cột trạng thái, ghi cả dòng đọc từ trước
        ///              sẽ đè số thứ tự, khóa xử lý mà Worker vừa ghi.
        /// Created date: 17/11/2026
        /// </summary>
        private async Task UpdateResolvedStateAsync(ShareDataSubscription entity)
        {
            await _subscriptionRep.AsUpdateable(entity)
                .UpdateColumns(u => new { u.State, u.ResolvedAt, u.UpdateTime, u.UpdateUId })
                .ExecuteCommandAsync();
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Cổng chặn bật đăng ký — đòi phải có hồ sơ ánh xạ ĐANG BẬT cho
        ///              (đối tác × gói tin × chiều) của đăng ký.
        ///
        ///              Luôn tra sống qua ShareDataMappingResolver. Một cổng chặn không được đọc
        ///              bản chụp: hồ sơ có thể vừa bị tắt hoặc xoá ở màn Ánh xạ mà đăng ký không
        ///              biết, và khi đó cổng sẽ cho qua đúng trường hợp nó sinh ra để chặn.
        /// Created date: 21/09/2026
        /// </summary>
        private async Task EnsureMappingReadyAsync(ShareDataSubscription entity)
        {
            var resolution = await _mappingResolver.ResolveAsync(entity.PartnerId, entity.DatatypeId, entity.Direction);
            if (resolution.State == BaseEnums.SubMappingState.Ready) return;

            throw Oops.Oh(BaseMsg.ShareData.Exception.MappingNotResolved);
        }

        /// <summary>
        /// Description: Cổng chặn bật đăng ký — mỗi (đối tác × gói tin × chiều) chỉ được có một đăng ký đang chạy.
        ///              Thêm mới đã chặn trùng với đăng ký đang chạy; bật lại và duyệt cũng phải qua cùng luật,
        ///              nếu không hai đăng ký tạm dừng cùng bộ ba có thể cùng được bật và cùng gửi một gói tin.
        /// Created date: 22/10/2026
        /// </summary>
        private async Task EnsureNoOtherActiveAsync(ShareDataSubscription entity)
        {
            if (await _subscriptionRep.IsAnyAsync(u => u.IsDelete == null
                    && u.ID != entity.ID
                    && u.PartnerId == entity.PartnerId
                    && u.Direction == entity.Direction
                    && u.DatatypeId == entity.DatatypeId
                    && u.State == BaseEnums.SubSubscriptionState.Active))
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionDuplicated);
        }

        /// <summary>
        /// Author: HieuNV
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
        /// Author: HieuNV
        /// Description: Đối tác có đang kết nối hay không
        /// Created date: 2026-08-08
        /// </summary>
        private async Task<bool> IsPartnerConnectedAsync(string? partnerId)
        {
            if (string.IsNullOrWhiteSpace(partnerId)) return false;

            var partner = await _partnerRep.GetFirstAsync(u => u.ID == partnerId);
            if (partner?.SessionState == BaseEnums.SessionState.Connected) return true;

            return await _sessionRep.IsAnyAsync(u => u.IsDelete == null
                && u.PartnerId == partnerId
                && u.State == BaseEnums.SessionFsmState.Active);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chuyển trạng thái đăng ký, kiểm tra trạng thái nguồn hợp lệ
        /// Created date: 2026-08-04
        /// Modified date: 17/11/2026
        /// </summary>
        private async Task ApplyStateAsync(
            string? id,
            BaseEnums.SubSubscriptionState target,
            BaseEnums.SubSubscriptionState?[] allowedFrom,
            BaseEnums.ActivityAction action,
            bool markResolved = false)
        {
            var entity = await GetForTransitionAsync(id, target, allowedFrom);

            entity.State = target;
            // Vào lại trạng thái Active thì xóa mốc chạy kế tiếp: vừa cho chạy ngay nhịp đầu,
            // vừa giải phóng lease còn treo nếu worker chết giữa nhịp kết xuất trước đó.
            if (target == BaseEnums.SubSubscriptionState.Active)
            {
                await EnsureNoOtherActiveAsync(entity);
                entity.NextTimeRun = null;
            }
            if (markResolved)
            {
                entity.ResolvedAt = DateTime.Now;
            }

            // Chỉ ghi cột trạng thái: ghi cả dòng đọc từ trước sẽ đè số thứ tự, khóa xử lý mà Worker vừa ghi
            var columns = new List<string> { nameof(ShareDataSubscription.State), nameof(ShareDataSubscription.UpdateTime), nameof(ShareDataSubscription.UpdateUId) };
            if (target == BaseEnums.SubSubscriptionState.Active)
            {
                columns.Add(nameof(ShareDataSubscription.NextTimeRun));
            }
            if (markResolved)
            {
                columns.Add(nameof(ShareDataSubscription.ResolvedAt));
            }
            var columnNames = columns.ToArray();
            await _subscriptionRep.AsUpdateable(entity).UpdateColumns(columnNames).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataSubscription);

            await _activityLogger.LogActionAsync(action, entity);
        }

        /// <summary>
        /// Description: Chuyển trạng thái đăng ký. Chuyển sang Active phải nối tiếp qua khóa chung với thêm mới,
        ///              để hai đăng ký cùng bộ ba không cùng qua cổng chặn trùng rồi cùng được bật.
        /// Created date: 22/10/2026
        /// </summary>
        private async Task ChangeStateAsync(
            string? id,
            BaseEnums.SubSubscriptionState target,
            BaseEnums.SubSubscriptionState?[] allowedFrom,
            BaseEnums.ActivityAction action,
            bool markResolved = false)
        {
            if (target != BaseEnums.SubSubscriptionState.Active)
            {
                await ApplyStateAsync(id, target, allowedFrom, action, markResolved);
                return;
            }

            await SerialLock.WaitAsync();
            try
            {
                await ApplyStateAsync(id, target, allowedFrom, action, markResolved);
            }
            finally
            {
                SerialLock.Release();
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy bản ghi và kiểm tra trạng thái nguồn có được phép chuyển tiếp không
        /// Created date: 2026-08-04
        /// </summary>
        private async Task<ShareDataSubscription> GetForTransitionAsync(
            string? id,
            BaseEnums.SubSubscriptionState target,
            BaseEnums.SubSubscriptionState?[] allowedFrom)
        {
            var entity = await _subscriptionRep.GetFirstAsync(u => u.IsDelete == null && u.ID == id)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            if (entity.State == null || !allowedFrom.Contains(entity.State))
                // Thông báo lỗi cần chuỗi nên đưa tên enum vào, không đưa số cho người dùng đọc.
                throw Oops.Oh(BaseMsg.ShareData.Exception.SubscriptionStateInvalid,
                    entity.State?.ToString() ?? string.Empty, target.ToString());

            return entity;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chuyển lịch gửi thành chuỗi JSON để lưu DB.
        /// Created date: 2026-08-04
        /// </summary>
        private static string? SerializeSchedule(ShareDataAddSubscriptionInput command)
        {
            if (command.Schedule == null)
                return null;

            command.Schedule.Kind ??= ShareDataConst.ScheduleKind.Continuous;
            command.Schedule.IntervalSeconds = command.IntervalSeconds ?? command.Schedule.IntervalSeconds;
            command.IntervalSeconds = command.Schedule.IntervalSeconds;

            return JsonConvert.SerializeObject(command.Schedule);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm chu kỳ, độ ưu tiên và thời gian chống dội nằm trong giới hạn khai ở sharedata_config.
        /// Created date: 19/10/2026
        /// </summary>
        private async Task ValidateDynamicLimits(
            int? intervalSeconds,
            int? priority,
            int? debounceSec)
        {
            var limit = await _configService.Get();
            var (intervalMin, intervalMax) = (limit.SubscriptionInterval.Min, limit.SubscriptionInterval.Max);
            var (priorityMin, priorityMax) = (limit.SubscriptionPriority.Min, limit.SubscriptionPriority.Max);
            var (debounceMin, debounceMax) = (limit.SubscriptionDebounceSec.Min, limit.SubscriptionDebounceSec.Max);

            CheckRange(
                intervalSeconds,
                intervalMin,
                intervalMax,
                BaseMsg.ShareData.Entity.IntervalSeconds);

            CheckRange(
                priority,
                priorityMin,
                priorityMax,
                BaseMsg.ShareData.Entity.Priority);

            CheckRange(
                debounceSec,
                debounceMin,
                debounceMax,
                BaseMsg.ShareData.Entity.DebounceSec);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Giá trị có nhập mà ngoài khoảng cho phép thì ném lỗi; không nhập thì bỏ qua.
        /// Created date: 19/10/2026
        /// </summary>
        private void CheckRange(
            int? value,
            int min,
            int max,
            string fieldNameKey)
        {
            if (!value.HasValue) return;

            if (value.Value < min || value.Value > max)
            {
                throw Oops.Oh(
                    BaseMsg.ShareData.Validation.ValueOutOfRange,
                    _localizer[fieldNameKey].Value,
                    min,
                    max);
            }
        }
    }
}
