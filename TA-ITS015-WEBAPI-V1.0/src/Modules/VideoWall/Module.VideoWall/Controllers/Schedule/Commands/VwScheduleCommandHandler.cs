using Mapster;
using Modules.CfgSystem.Core.Dto.OpConfig;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Helper;
using Module.VideoWall.Core.Dto.Schedule;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Module.VideoWall.Infrastructure.Services.Layout;
using Module.VideoWall.Infrastructure.Services.Schedule;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.VideoWall.Controllers.Schedule.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwScheduleCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwSchedule> _vwScheduleRep;
        private readonly BaseRepository<VwScene> _vwSceneRep;
        private readonly BaseCacheService _cache;
        private readonly VwPermissionService _permission;
        private readonly VwScheduleJobService _jobService;
        private readonly IMessageBus _bus;
        private readonly VwWallLayoutService _layout;

        public VwScheduleCommandHandler(
            BaseRepository<VwSchedule> vwScheduleRep,
            BaseRepository<VwScene> vwSceneRep,
            BaseCacheService cache,
            VwPermissionService permission,
            VwScheduleJobService jobService,
            IMessageBus bus,
            VwWallLayoutService layout)
        {
            _layout = layout;
            _permission = permission;
            _jobService = jobService;
            _bus = bus;
            _vwScheduleRep = vwScheduleRep;
            _vwSceneRep = vwSceneRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 VwSchedule
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwAddScheduleInput command)
        {
            // 1. Kiểm tra trùng mã
            command.Code = command.Code?.Trim();
            if (await _vwScheduleRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // 2. Kiểm tra khóa ngoại
            await CheckReferenceAsync(command);
            command.CreateUId = null;

            // 2b. Phân quyền vùng màn hình: kịch bản đích phải nằm trọn trong vùng được cấp
            //     (lúc chạy tự động không kiểm tra lại ⇒ chặn ngay khi tạo lịch chạy)
            await _permission.EnsureSceneInsideUserAllowedAreaAsync(command.TargetSceneId);

            // 3. Mốc chạy do BE quản lý, không nhận từ client
            var entity = command.Adapt<VwSchedule>();
            entity.LastRun = null;
            entity.NextRun = null;

            // 3b. Không cho trùng thời điểm chạy với lịch đang bật khác
            await CheckTimeConflictAsync(entity);

            // 4. Ghi DB + đăng ký job Hangfire (mỗi lịch 1 recurring job) trong cùng transaction:
            //    đăng ký job lỗi thì rollback, không để lại lịch không chạy được.
            try
            {
                _vwScheduleRep.Context.Ado.BeginTran();

                await _vwScheduleRep.InsertWithDiffLogAsync(entity);
                entity.NextRun = _jobService.SyncJob(entity);
                await _jobService.SaveRunMarkersAsync(entity);

                _vwScheduleRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _vwScheduleRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                _jobService.RemoveJob(entity.ID);
                throw;
            }

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 VwSchedule
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwUpdateScheduleInput command)
        {
            // 1. Kiểm tra trùng mã với bản ghi khác
            command.Code = command.Code?.Trim();
            if (await _vwScheduleRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // 2. Lấy bản ghi gốc
            var entity = await _vwScheduleRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 2b. Phân quyền vùng màn hình: kịch bản CŨ và kịch bản MỚI đều phải nằm trong vùng được cấp
            //     (chặn đổi lịch chạy của vùng khác sang kịch bản của mình)
            await _permission.EnsureSceneInsideUserAllowedAreaAsync(entity.TargetSceneId);
            if (command.TargetSceneId != entity.TargetSceneId)
                await _permission.EnsureSceneInsideUserAllowedAreaAsync(command.TargetSceneId);

            // 4. Kiểm tra khóa ngoại
            await CheckReferenceAsync(command);

            // 5. Map dữ liệu từ command vào bản ghi gốc.
            //    THIẾU BƯỚC NÀY thì Update trả về thành công nhưng DB không đổi gì.
            //    LastRun do job / RunNow ghi ⇒ giữ nguyên giá trị trong DB, không nhận từ client.
            var lastRun = entity.LastRun;
            command.Adapt(entity);
            entity.LastRun = lastRun;

            // 5b. Không cho trùng thời điểm chạy với lịch đang bật khác (so trên dữ liệu sau khi map)
            await CheckTimeConflictAsync(entity);

            // 6. Ghi DB + cập nhật job Hangfire (đổi giờ, bật/tắt, đổi hành động...) + xóa cache
            try
            {
                _vwScheduleRep.Context.Ado.BeginTran();

                entity.NextRun = _jobService.SyncJob(entity);
                await _vwScheduleRep.UpdateWithDiffLogAsync(entity, false);

                _vwScheduleRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _vwScheduleRep.Context.Ado.RollbackTran(); } catch { }
                throw;
            }

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 VwSchedule (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwDeleteScheduleInput command)
        {
            var entity = await _vwScheduleRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Phân quyền vùng màn hình: không xoá được lịch chạy của kịch bản ngoài vùng được cấp
            await _permission.EnsureSceneInsideUserAllowedAreaAsync(entity.TargetSceneId);

            await _vwScheduleRep.SoftDeleteAsync<VwSchedule>(entity);
            _jobService.RemoveJob(entity.ID);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều VwSchedule (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(List<VwDeleteScheduleInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _vwScheduleRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            // Phân quyền vùng màn hình: mọi kịch bản đích phải nằm trong vùng được cấp
            foreach (var sceneId in entities.Select(e => e.TargetSceneId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
                await _permission.EnsureSceneInsideUserAllowedAreaAsync(sceneId);

            await _vwScheduleRep.SoftDeleteAsync<VwSchedule>(u => ids.Contains(u.ID));
            foreach (var id in ids)
                _jobService.RemoveJob(id);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chạy ngay 1 VwSchedule (nút "Chạy thử"): thực hiện hành động và ghi LastRun.
        ///              Không đụng tới job Hangfire / NextRun.
        /// Created date: 06/10/2026
        /// </summary>
        public async Task HandleAsync(VwRunNowScheduleInput command, CancellationToken ct = default)
        {
            var entity = await _vwScheduleRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Phân quyền vùng màn hình: chỉ chạy được lịch của kịch bản trong vùng được cấp
            await _permission.EnsureSceneInsideUserAllowedAreaAsync(entity.TargetSceneId);

            await _jobService.RunNowAsync(entity, ct);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kiểm tra các khóa ngoại của VwSchedule có tồn tại hay không
        /// Created date: 2026-08-01
        /// </summary>
        private async Task CheckReferenceAsync(VwAddScheduleInput command)
        {
            if (string.IsNullOrWhiteSpace(command.TargetSceneId))
                return;

            var scene = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.TargetSceneId)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.TargetSceneId]);

            // Kịch bản đích phải thuộc bố cục đang dùng (kịch bản bố cục cũ không kích hoạt được)
            await _layout.EnsureSceneOnCurrentLayoutAsync(scene);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chặn lịch đang bật chạy trùng thời điểm với lịch đang bật khác, kể cả cùng kịch bản đích
        ///              (2 job cùng phút chạy nối tiếp, kịch bản nào thắng không xác định; cùng kịch bản thì thành lịch thừa).
        ///              Lịch tắt / hành động chưa hỗ trợ / lịch một lần đã qua giờ không bao giờ chạy ⇒ bỏ qua.
        /// Created date: 20/10/2026
        /// </summary>
        private async Task CheckTimeConflictAsync(VwSchedule entity)
        {
            var now = BaseUtil.TimeServer.GetTimeServer();
            if (!WillRun(entity, now)) return;

            var id = entity.ID;
            var others = await _vwScheduleRep.AsQueryable()
                .Where(u => u.IsDelete == null
                         && u.Status == BaseEnums.StatusEnum.Enable
                         && u.ID != id)
                .ToListAsync();

            if (others.Count == 0) return;

            var bufferMinutes = await ReadConflictBufferMinutesAsync();
            var conflictCodes = others
                .Where(o => WillRun(o, now) && VwScheduleTimeHelper.IsOverlap(entity, o, bufferMinutes))
                .Select(o => o.Code)
                .ToList();

            if (conflictCodes.Count == 0) return;

            var codes = string.Join(", ", conflictCodes);
            throw bufferMinutes > 0
                ? Oops.Oh(BaseMsg.Vw.Exception.ScheduleTimeConflictBuffer, codes, bufferMinutes)
                : Oops.Oh(BaseMsg.Vw.Exception.ScheduleTimeConflict, codes);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đọc khoảng đệm chống trùng giờ (phút) từ SysOpConfig (vw_config / vw_schedule_conflict_buffer_minutes).
        ///              Chưa cấu hình / sai định dạng / số âm ⇒ dùng mặc định (0 = chỉ chặn trùng đúng phút).
        /// Created date: 20/10/2026
        /// </summary>
        private async Task<int> ReadConflictBufferMinutesAsync()
        {
            try
            {
                var configs = await _bus.InvokeAsync<List<SysOpConfigOutput>>(
                    new SysOpConfigInput { GroupCode = VwScheduleConst.OpConfig.GroupCode, Code = VwScheduleConst.OpConfig.ConflictBufferMinutes });

                // Query OpConfig lọc Contains ⇒ so khớp lại đúng mã
                var val = configs?.FirstOrDefault(c => c.GroupCode == VwScheduleConst.OpConfig.GroupCode
                                                    && c.Code == VwScheduleConst.OpConfig.ConflictBufferMinutes)?.Value;
                if (int.TryParse(val?.Trim(), out var minutes) && minutes >= 0)
                    return minutes;
            }
            catch
            {
            }
            return VwScheduleConst.OpConfig.DefaultConflictBufferMinutes;
        }

        private static bool WillRun(VwSchedule schedule, DateTime now)
            => schedule.Status == BaseEnums.StatusEnum.Enable
            && VwScheduleJobService.IsSupportedAction(schedule.Action)
            && VwScheduleTimeHelper.BuildCron(schedule, now) != null;
    }
}
