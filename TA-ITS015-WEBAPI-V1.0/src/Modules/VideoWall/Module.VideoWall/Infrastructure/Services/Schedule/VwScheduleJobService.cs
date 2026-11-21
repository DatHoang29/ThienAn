using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Module.VideoWall.Core.Helper;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Scene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.VideoWall.Infrastructure.Services.Schedule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Quản lý recurring job Hangfire của Lịch phát kịch bản (VwSchedule).
    /// Created date: 06/10/2026
    /// </summary>
    public class VwScheduleJobService : IScoped
    {
        private readonly BaseRepository<VwSchedule> _scheduleRep;
        private readonly BaseRepository<VwScene> _sceneRep;
        private readonly IMessageBus _bus;
        private readonly BaseCacheService _cache;
        private readonly ILogger<VwScheduleJobService> _logger;

        public VwScheduleJobService(
            BaseRepository<VwSchedule> scheduleRep,
            BaseRepository<VwScene> sceneRep,
            IMessageBus bus,
            BaseCacheService cache,
            ILogger<VwScheduleJobService> logger)
        {
            _scheduleRep = scheduleRep;
            _sceneRep = sceneRep;
            _bus = bus;
            _cache = cache;
            _logger = logger;
        }

        public static bool IsHangfireEnabled => App.Configuration["Hangfire:Enable"]?.ToLower() == "true";

        public static string GetJobId(string scheduleId) => VwScheduleConst.JobIdPrefix + scheduleId;

        public static bool IsSupportedAction(string? action)
            => !string.IsNullOrWhiteSpace(action) && VwScheduleConst.Action.Supported.Contains(action);

        #region Đồng bộ với Hangfire

        /// <summary>
        /// Author: HieuNV
        /// Description: Đăng ký / cập nhật / gỡ recurring job của 1 lịch theo dữ liệu hiện tại.
       
        /// Created date: 06/10/2026
        /// </summary>
        public DateTime? SyncJob(VwSchedule schedule)
        {
            if (!IsHangfireEnabled || string.IsNullOrWhiteSpace(schedule.ID)) return null;

            var jobId = GetJobId(schedule.ID);
            var cron = GetActiveCron(schedule, BaseUtil.TimeServer.GetTimeServer());

            if (cron == null)
            {
                RecurringJob.RemoveIfExists(jobId);
                return null;
            }

            var scheduleId = schedule.ID;
            try
            {
                RecurringJob.AddOrUpdate<VwScheduleJobService>(
                    recurringJobId: jobId,
                    methodCall: service => service.RunAsync(scheduleId, CancellationToken.None),
                    cronExpression: cron,
                    options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
            }
            catch (Exception ex) when (ex is not AppFriendlyException)
            {
                // Hangfire kiểm tra cú pháp cron ngay tại đây ⇒ báo lỗi cho người nhập thay vì lưu lịch hỏng
                _logger.LogWarning(ex, "Biểu thức lịch [{Cron}] của lịch {Code} không hợp lệ", cron, schedule.Code);
                throw Oops.Oh(BaseLocaleManager.BaseValidation.MustBeValidValue, $"Cron [{cron}]");
            }

            return GetNextRun(jobId);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Gỡ recurring job của 1 lịch (khi xoá lịch).
        /// Created date: 06/10/2026
        /// </summary>
        public void RemoveJob(string? scheduleId)
        {
            if (!IsHangfireEnabled || string.IsNullOrWhiteSpace(scheduleId)) return;
            RecurringJob.RemoveIfExists(GetJobId(scheduleId));
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Dựng lại job cho toàn bộ lịch + dọn job mồ côi (lịch đã xoá nhưng job còn).
        ///              Gọi lúc khởi động app (VwScheduleJobRegistrar). Một lịch lỗi không chặn các lịch khác.
        /// Created date: 06/10/2026
        /// </summary>
        public async Task SyncAllAsync()
        {
            if (!IsHangfireEnabled) return;

            var now = BaseUtil.TimeServer.GetTimeServer();
            var schedules = await _scheduleRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .ToListAsync();

            var activeJobIds = new HashSet<string>();

            foreach (var schedule in schedules)
            {
                try
                {
                    if (GetActiveCron(schedule, now) != null)
                        activeJobIds.Add(GetJobId(schedule.ID));

                    var nextRun = SyncJob(schedule);
                    if (schedule.NextRun != nextRun)
                    {
                        schedule.NextRun = nextRun;
                        await SaveRunMarkersAsync(schedule);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không đăng ký được job Hangfire cho lịch phát kịch bản {Code}", schedule.Code);
                }
            }

            using (var connection = JobStorage.Current.GetConnection())
            {
                var orphanIds = connection.GetRecurringJobs()
                    .Select(j => j.Id)
                    .Where(id => id.StartsWith(VwScheduleConst.JobIdPrefix, StringComparison.Ordinal)
                              && !activeJobIds.Contains(id))
                    .ToList();

                foreach (var id in orphanIds)
                    RecurringJob.RemoveIfExists(id);
            }

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);
        }

        #endregion

        #region Thực thi

        /// <summary>
        /// Author: HieuNV
        /// Description: Điểm vào của Hangfire cho 1 lịch — PHẢI public.
        /// Created date: 06/10/2026
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        public async Task RunAsync(string scheduleId, CancellationToken ct = default)
        {
            var now = BaseUtil.TimeServer.GetTimeServer();
            var jobId = GetJobId(scheduleId);

            var schedule = await _scheduleRep.GetFirstAsync(u => u.IsDelete == null && u.ID == scheduleId);

            // Job mồ côi: lịch đã xoá / tắt / đổi sang hành động chưa hỗ trợ mà job vẫn còn (VD sửa DB tay)
            if (schedule == null
                || schedule.Status != BaseEnums.StatusEnum.Enable
                || !IsSupportedAction(schedule.Action))
            {
                _logger.LogWarning("Gỡ job {JobId}: lịch không còn hiệu lực.", jobId);
                RecurringJob.RemoveIfExists(jobId);
                return;
            }

            // Lịch "Một lần": cron không có năm ⇒ tự chặn lệch năm / chạy bù quá trễ
            if (schedule.ScheduleType == VwScheduleConst.ScheduleType.Once)
            {
                if (!VwScheduleTimeHelper.TryParseOnceDate(schedule.OnceDate, out var at)
                    || now < at.AddMinutes(-1)
                    || (now - at).TotalMinutes > VwScheduleConst.OnceMaxDelayMinutes)
                {
                    _logger.LogWarning("Bỏ qua lịch một lần {Code}: thời điểm {OnceDate} không khớp lúc chạy {Now:yyyy-MM-dd HH:mm}.",
                        schedule.Code, schedule.OnceDate, now);
                    await FinishOnceAsync(schedule, lastRun: schedule.LastRun);
                    return;
                }
            }

            _logger.LogInformation("Chạy lịch phát kịch bản {Code} ({Action}).", schedule.Code, schedule.Action);
            await ExecuteActionAsync(schedule, ct);

            if (schedule.ScheduleType == VwScheduleConst.ScheduleType.Once)
            {
                await FinishOnceAsync(schedule, lastRun: now);
            }
            else
            {
                schedule.LastRun = now;
                schedule.NextRun = GetNextRun(jobId);
                await SaveRunMarkersAsync(schedule);
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chạy ngay 1 lịch (nút "Chạy thử" trên FE). Chỉ cập nhật LastRun,
        ///              không đụng tới job / NextRun / trạng thái.
        /// Created date: 06/10/2026
        /// </summary>
        public async Task RunNowAsync(VwSchedule schedule, CancellationToken ct = default)
        {
            if (!IsSupportedAction(schedule.Action))
                throw Oops.Oh(BaseLocaleManager.BaseValidation.MustBeValidValue, $"Action [{schedule.Action}]");

            await ExecuteActionAsync(schedule, ct);

            schedule.LastRun = BaseUtil.TimeServer.GetTimeServer();
            await SaveRunMarkersAsync(schedule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thực hiện hành động của lịch.
        /// Created date: 06/10/2026
        /// </summary>
        private async Task ExecuteActionAsync(VwSchedule schedule, CancellationToken ct)
        {
            switch (schedule.Action)
            {
                case VwScheduleConst.Action.ActivateScene:
                    {
                        var scene = await _sceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == schedule.TargetSceneId)
                            ?? throw Oops.Oh(BaseLocaleManager.BaseException.NotExist,
                                Furion.Localization.L.Text[BaseMsg.Vw.Entity.TargetSceneId]);

                        // Đi qua đúng luồng nghiệp vụ VwScene/Active (VwSceneWorkflowCommandHandler) — giống bấm Áp dụng:
                        //   - luôn gửi ACTIVATE_SCENE xuống Worker (Worker gọi lại ô kịch bản / dựng lại từ DB rồi lưu);
                        //   - ApplySceneOnServer = false: Worker ghi kịch bản đang phát + báo FE sau khi thiết bị xong;
                        //   - ghi VwEventTriggerLog.
                        await _bus.InvokeAsync<VwActiveSceneOutput>(new VwActiveSceneInput { Code = scene.Code }, ct);
                        break;
                    }

                default:
                    throw Oops.Oh(BaseLocaleManager.BaseValidation.MustBeValidValue, $"Action [{schedule.Action}]");
            }
        }

        #endregion

        #region Tiện ích

        /// <summary>
        /// Cron của lịch nếu lịch cần có job; null nếu không (tắt, hành động chưa hỗ trợ, thiếu dữ liệu, đã qua giờ).
        /// </summary>
        private static string? GetActiveCron(VwSchedule schedule, DateTime now)
        {
            if (schedule.IsDelete != null
                || schedule.Status != BaseEnums.StatusEnum.Enable
                || !IsSupportedAction(schedule.Action))
                return null;

            return VwScheduleTimeHelper.BuildCron(schedule, now);
        }

        /// <summary>
        /// Lần chạy kế tiếp do Hangfire tính (UTC) → đổi sang giờ server.
        /// </summary>
        private static DateTime? GetNextRun(string jobId)
        {
            using var connection = JobStorage.Current.GetConnection();
            var job = connection.GetRecurringJobs(new[] { jobId }).FirstOrDefault();
            if (job?.NextExecution == null) return null;

            return BaseUtil.TimeServer.GetTimeServer(DateTime.SpecifyKind(job.NextExecution.Value, DateTimeKind.Utc));
        }

        /// <summary>
        /// Kết thúc lịch "Một lần": gỡ job, tắt lịch để không lặp lại năm sau.
        /// </summary>
        private async Task FinishOnceAsync(VwSchedule schedule, DateTime? lastRun)
        {
            RecurringJob.RemoveIfExists(GetJobId(schedule.ID));
            schedule.LastRun = lastRun;
            schedule.NextRun = null;
            schedule.Status = BaseEnums.StatusEnum.Disable;
            await SaveRunMarkersAsync(schedule);
        }

        /// <summary>
        /// Chỉ ghi các cột mốc chạy — không qua DiffLog để bảng lịch sử sửa đổi không bị job làm phình.
        /// </summary>
        public async Task SaveRunMarkersAsync(VwSchedule schedule)
        {
            await _scheduleRep.Context.Updateable(schedule)
                .UpdateColumns(u => new { u.LastRun, u.NextRun, u.Status })
                .ExecuteCommandAsync();

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);
        }

        #endregion
    }
}
