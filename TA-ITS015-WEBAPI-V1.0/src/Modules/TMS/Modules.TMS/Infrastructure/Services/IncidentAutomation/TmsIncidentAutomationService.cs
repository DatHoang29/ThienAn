using Shared.Utility.Utilities;
using Modules.TMS.Infrastructure.Services.Incident.Classification;
using Modules.TMS.Infrastructure.Services.Incident.Configuration;
using Modules.TMS.Infrastructure.Services.Incident.Mapping;
using Modules.TMS.Infrastructure.Services.Incident.Messaging;
using Modules.TMS.Infrastructure.Services.Incident.Snapshot;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Microsoft.Extensions.Logging;
using Modules.CfgSystem.Core.Dto.OpConfig;
using Modules.TMS.Core.Dto.IncidentControl;
using Modules.TMS.Core.Dto.WorkExecute;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Controllers.EquipmentType.Dto;
using Modules.TMS.Infrastructure.Services.Incident;
using Modules.TMS.Core.Dto.IncidentAutomationRun;
using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.EventDefault.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Module;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using System.Diagnostics;
using Modules.TMS.Core.Helper;
using Modules.TMS.Constants;

namespace Modules.TMS.Infrastructure.Services.IncidentAutomation
{
    /// <summary>
    /// Author: SonTH
    /// Description: Orchestration chính cho luồng TỰ ĐỘNG xử lý sự kiện/sự cố, bao gồm tích hợp
    ///              điều khiển VMS/LCS qua các handler/pipeline hiện hữu của luồng manual.
    ///              Chịu trách nhiệm: tạo lịch cảnh báo, bắt đầu tự động (verify + chuyển InProgress dùng
    ///              chung state machine incident), hủy trong thời gian cảnh báo, và tự động kết thúc
    ///              (dùng chung UpdateState Finished với luồng thủ công).
    ///
    ///              KHÔNG dùng Hangfire: trạng thái/lịch đã bền vững trong TmsIncidentAutomationRun
    ///              (Status + ExecuteDueAt + AutoFinishAt), việc "đánh thức đúng lúc" do
    ///              TmsIncidentAutomationWorker (BackgroundService) quét DB đảm nhiệm. Nhờ vậy chỉ có một
    ///              nguồn sự thật duy nhất và hủy lịch chỉ cần đổi Status.
    ///              Xem: docs/THIET-KE-KICH-BAN-XU-LY-SU-CO-TU-DONG.md mục 8.3-8.5, 8.8-8.9.
    /// Created date: 24/07/2026
    /// </summary>
    public class TmsIncidentAutomationService : IScoped
    {
        private BaseRepository<TmsIncidentAutomationRun> _runRep;
        private BaseRepository<TmsIncidentAutomationConfig> _configRep;
        private BaseRepository<TmsTrafficPlans> _trafficPlanRep;
        private BaseRepository<TmsIncident> _incidentRepo;
        private BaseRepository<TmsWorkExecute> _workExecuteRep;
        private BaseRepository<TmsWorkDetail> _workDetailRep;
        private BaseRepository<TmsTrafficPlanWorkDetails> _trafficPlanWorkDetailRep;
        private BaseRepository<TmsEquipmentEventRule> _equipmentEventRuleRep;
        private BaseCacheService _cache;
        private IMessageBus _messageBus;
        private IncidentBroadcaster _broadcaster;
        private ILogger<TmsIncidentAutomationService> _logger;

        private IncidentGroupResolver _groupResolver;
        private readonly EventCompletionPolicy _completionPolicy;
        private readonly BaseRepository<TmsEventType> _eventTypes;
        private readonly VdsAutomationImagePolicy _imagePolicy;

        public TmsIncidentAutomationService(
            BaseRepository<TmsIncidentAutomationRun> runRep,
            BaseRepository<TmsIncidentAutomationConfig> configRep,
            BaseRepository<TmsTrafficPlans> trafficPlanRep,
            BaseRepository<TmsIncident> incidentRepo,
            BaseRepository<TmsWorkExecute> workExecuteRep,
            BaseRepository<TmsWorkDetail> workDetailRep,
            BaseRepository<TmsTrafficPlanWorkDetails> trafficPlanWorkDetailRep,
            BaseRepository<TmsEquipmentEventRule> equipmentEventRuleRep,
            BaseCacheService cache,
            IMessageBus messageBus,
            IncidentBroadcaster broadcaster,
            ILogger<TmsIncidentAutomationService> logger, IncidentGroupResolver groupResolver,
            EventCompletionPolicy completionPolicy, BaseRepository<TmsEventType> eventTypes, VdsAutomationImagePolicy imagePolicy)
        {
            _completionPolicy = completionPolicy;
            _eventTypes = eventTypes;
            _imagePolicy = imagePolicy;
            this._groupResolver = groupResolver;
            _runRep = runRep;
            _configRep = configRep;
            _trafficPlanRep = trafficPlanRep;
            _incidentRepo = incidentRepo;
            _workExecuteRep = workExecuteRep;
            _workDetailRep = workDetailRep;
            _trafficPlanWorkDetailRep = trafficPlanWorkDetailRep;
            _equipmentEventRuleRep = equipmentEventRuleRep;
            _cache = cache;
            _messageBus = messageBus;
            _broadcaster = broadcaster;
            _logger = logger;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Được gọi ngay sau khi incident đạt một state (Registration/Verified). Nếu có cấu hình
        ///              tự động enable + đúng TriggerState + chưa có active run, tạo run WarningPending với
        ///              ExecuteDueAt = now + WarningDelaySeconds. Không cần đăng ký job ở đâu khác:
        ///              TmsIncidentAutomationWorker sẽ tự phát hiện run tới hạn khi quét DB.
        ///              Không throw nếu không có cấu hình - đây là no-op hợp lệ cho các EventType chưa bật auto.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task<TmsIncidentAutomationRun?> TryScheduleAsync(string incidentId, string triggerState)
        {
            var (run, _) = await TryScheduleWithReasonAsync(incidentId, triggerState);
            return run;
        }

        /// <summary>Only explicitly enrolled new events may expire. Scheduling is handled on delivery.</summary>
        public async Task<string?> ProcessRegisteredEventsAsync(int batchSize, string? afterId, CancellationToken ct)
        {
            var rows = await RegisteredEventQuery(_incidentRepo.AsQueryable(), afterId, batchSize).ToListAsync(ct);
            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                if (!EventCompletionPolicy.IsDue(row.Metadata, DateTimeOffset.UtcNow)) continue;
                try { await _messageBus.InvokeAsync<bool>(new CompleteUnclaimedEvent(row.ID, row.TenantId)); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex) { _logger.LogError(ex, "[EventLifecycle] Không hoàn thành được {IncidentId}", row.ID); }
            }
            return rows.Count == batchSize ? rows[^1].ID : null;
        }

        internal static ISugarQueryable<TmsIncident> RegisteredEventQuery(
            ISugarQueryable<TmsIncident> query, string? afterId, int batchSize) =>
            query.Where(x => x.IsDelete == null && (x.EventGroup == IncidentEventGroupConst.Event || x.EventGroup == IncidentEventGroupConst.Incident) && x.State == "1")
                .Where(x => x.Metadata != null && x.Metadata.Contains(EventCompletionPolicy.DeadlineMarker)
                    && !x.Metadata.Contains(IncidentRecordingConst.SuppressedAutomationMarker))
                .WhereIF(afterId != null, x => SqlFunc.GreaterThan(x.ID, afterId))
                .Where(x => !SqlFunc.Subqueryable<TmsIncidentAutomationRun>()
                    .Where(r => r.IncidentId == x.ID && r.TenantId == x.TenantId).Any())
                .OrderBy(x => x.ID).Take(batchSize);

        /// <summary>Serialize external retries on the incident row and commit the receipt with the run.
        /// The receipt survives cancellation/completion and NoConfig responses.</summary>
        public async Task<(TmsIncidentAutomationRun? Run, string ReasonCode)> TryScheduleOnceWithReasonAsync(string incidentId, string triggerState)
        {
            if (string.IsNullOrWhiteSpace(incidentId) || triggerState is not ("register" or "verify"))
                return (null, "InvalidInput");
            // Resolve through repository scope before taking a raw SQL lock.
            var scopedIncident = await _incidentRepo.GetFirstAsync(x => x.ID == incidentId && x.IsDelete == null);
            if (scopedIncident == null) return (null, "IncidentNotFound");
            var db = _incidentRepo.Context;
            await db.Ado.BeginTranAsync();
            try
            {
                await db.Ado.SqlQueryAsync<string>("SELECT ID FROM TmsIncident WITH (UPDLOCK,HOLDLOCK) WHERE ID=@id AND TenantId=@tenant",
                    new SugarParameter("@id", incidentId), new SugarParameter("@tenant", scopedIncident.TenantId));
                var incident = await _incidentRepo.GetFirstAsync(x => x.ID == incidentId && x.TenantId == scopedIncident.TenantId && x.IsDelete == null);
                if (incident == null) { await db.Ado.CommitTranAsync(); return (null, "IncidentNotFound"); }
                var key = "VdsAutomationReceipt_" + triggerState;
                var metadata = string.IsNullOrWhiteSpace(incident.Metadata) ? new JObject() : JObject.Parse(incident.Metadata);
                if (metadata[key] != null)
                {
                    await db.Ado.CommitTranAsync();
                    return (null, "AlreadyDispatched");
                }
                var hadRun = await _runRep.IsAnyAsync(x => x.IncidentId == incidentId && x.TenantId == incident.TenantId && x.TriggerState == triggerState);
                var result = hadRun ? ((TmsIncidentAutomationRun?)null, "AlreadyDispatched")
                    : await TryScheduleCoreAsync(incidentId, triggerState);
                if (result.Item2 is "InsertFailed" or "UnexpectedError"
                    or IncidentAutomationConst.ScheduleReason.IncidentStateMismatch)
                {
                    await db.Ado.RollbackTranAsync();
                    return result;
                }
                metadata[key] = result.Item2;
                if (!hadRun && triggerState == IncidentAutomationConst.TriggerState.Registration
                    && result.Item2 == IncidentAutomationConst.ScheduleReason.NoConfig
                    && !await _runRep.IsAnyAsync(x => x.IncidentId == incidentId && x.TenantId == incident.TenantId))
                    await _completionPolicy.AddDeadlineAsync(incident, metadata);
                var json = metadata.ToString(Formatting.None);
                if (json.Length > EntityConst.Length512)
                    throw new InvalidOperationException("Incident metadata cannot hold the automation receipt.");
                await _incidentRepo.AsUpdateable().SetColumns(x => new TmsIncident { Metadata = json })
                    .Where(x => x.ID == incidentId && x.TenantId == incident.TenantId).ExecuteCommandAsync();
                await db.Ado.CommitTranAsync();
                return result;
            }
            catch { await db.Ado.RollbackTranAsync(); throw; }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Bản đầy đủ của TryScheduleAsync, trả về kèm MÃ LÝ DO để biết vì sao không lập lịch được.
        ///              Tách ra vì cùng chuỗi điều kiện này phục vụ ba nhu cầu: lập lịch thật, trả kết quả cho
        ///              hệ thống ngoài khi tạo sự cố qua mediator, và ghi log chẩn đoán. Cài riêng từng nơi thì
        ///              chắc chắn sẽ lệch nhau theo thời gian.
        ///
        ///              Mọi nhánh không lập lịch được đều ghi log ở mức Information kèm mã lý do - đây là
        ///              trạng thái hợp lệ và rất phổ biến (phần lớn loại sự kiện không bật tự động), nên không
        ///              dùng Warning để tránh làm nhiễu log.
        /// Created date: 24/07/2026
        /// </summary>
        public Task<(TmsIncidentAutomationRun? Run, string ReasonCode)> TryScheduleWithReasonAsync(string incidentId, string triggerState)
            => TryScheduleOnceWithReasonAsync(incidentId, triggerState);

        private async Task<(TmsIncidentAutomationRun? Run, string ReasonCode)> TryScheduleCoreAsync(string incidentId, string triggerState)
        {
            if (string.IsNullOrWhiteSpace(incidentId) || string.IsNullOrWhiteSpace(triggerState))
                return LogSkip(null, incidentId, triggerState, null, IncidentAutomationConst.ScheduleReason.InvalidInput);

            var incident = await _incidentRepo.GetFirstAsync(u => u.ID == incidentId && u.IsDelete == null);
            if (incident != null) await this._groupResolver.PopulateGroupsAsync(new[] { incident });
            if (incident != null && !IncidentEventGroupConst.AllowsProcessing(incident.EventGroup, incident.State))
                return LogSkip(incident, incidentId, triggerState, null, "EventDoesNotRequireProcessing");
            if (incident == null || string.IsNullOrWhiteSpace(incident.EventTypeId))
                return LogSkip(null, incidentId, triggerState, null, IncidentAutomationConst.ScheduleReason.IncidentNotFound);

            var expectedState = triggerState switch {
                IncidentAutomationConst.TriggerState.Registration => ((int)BaseEnums.TmsIncidentStateEnum.Registration).ToString(),
                IncidentAutomationConst.TriggerState.Verified => ((int)BaseEnums.TmsIncidentStateEnum.Verified).ToString(),
                _ => null
            };
            if (expectedState == null || incident.State != expectedState)
                return LogSkip(incident, incidentId, triggerState, null, IncidentAutomationConst.ScheduleReason.IncidentStateMismatch);
            if (incident.Metadata?.Contains(IncidentRecordingConst.SuppressedAutomationMarker, StringComparison.Ordinal) == true)
                return (null, "RecoveryAutomationSuppressed");

            var config = await _configRep.GetFirstAsync(u =>
                u.EventTypeId == incident.EventTypeId
                && u.TenantId == incident.TenantId
                && u.IsDelete == null
                && u.IsAutoExecuteEnabled
                && u.Status == BaseEnums.StatusEnum.Enable);
            if (config == null)
                return LogSkip(incident, incidentId, triggerState, null, IncidentAutomationConst.ScheduleReason.NoConfig);
            if (config.TriggerState != triggerState)
                return LogSkip(incident, incidentId, triggerState, config.ID, IncidentAutomationConst.ScheduleReason.TriggerStateMismatch);

            if (string.IsNullOrWhiteSpace(config.TrafficPlanId))
                return LogSkip(incident, incidentId, triggerState, config.ID, IncidentAutomationConst.ScheduleReason.NoTrafficPlan);

            // Lọc theo nguồn phát hiện sự cố. Kiểm tra trong bộ nhớ chứ không đưa vào truy vấn vì cấu hình
            // là duy nhất theo EventTypeId nên truy vấn trên đã trả về đúng một dòng - không cần LIKE hay index.
            if (!IncidentAutomationSourceConst.IsAllowed(config.AllowedSources, incident.Source))
                return LogSkip(incident, incidentId, triggerState, config.ID, IncidentAutomationConst.ScheduleReason.SourceNotAllowed);

            // Chỉ một run active cho một incident tại một thời điểm.
            var hasActiveRun = await _runRep.IsAnyAsync(u =>
                u.IncidentId == incidentId
                && u.TenantId == incident.TenantId
                && u.IsDelete == null
                && !IncidentAutomationConst.TerminalRunStatuses.Contains(u.Status));
            if (hasActiveRun)
                return LogSkip(incident, incidentId, triggerState, config.ID, IncidentAutomationConst.ScheduleReason.ActiveRunExists);

            var plan = await _trafficPlanRep.GetFirstAsync(u => u.ID == config.TrafficPlanId && u.TenantId == incident.TenantId && u.IsDelete == null);
            var isValidPlan = plan != null
                && plan.Status == BaseEnums.StatusEnum.Enable
                && plan.EventTypeId == incident.EventTypeId
                && (plan.ExecutionMode == TrafficPlanConst.ExecutionMode.Automatic || plan.ExecutionMode == TrafficPlanConst.ExecutionMode.Both);
            if (!isValidPlan)
                return LogSkip(incident, incidentId, triggerState, config.ID, IncidentAutomationConst.ScheduleReason.PlanNotAutomatic);

            var runNo = await _runRep.AsQueryable().Where(u => u.IncidentId == incidentId).CountAsync() + 1;
            var now = BaseUtil.TimeServer.GetTimeServer();

            var run = new TmsIncidentAutomationRun
            {
                TenantId = incident.TenantId,
                IncidentId = incidentId,
                RunNo = runNo,
                EventTypeId = incident.EventTypeId,
                AutomationConfigId = config.ID,
                ConfigVersion = config.ConfigVersion,
                TrafficPlanId = plan!.ID,
                PlanVersion = plan.PlanVersion,
                ConfigSnapshotJson = await _imagePolicy.FreezeAsync(JsonConvert.SerializeObject(config), incident),
                TriggerState = triggerState,
                Status = IncidentAutomationConst.RunStatus.WarningPending,
                WarningStartedAt = now,
                ExecuteDueAt = now.AddSeconds(config.WarningDelaySeconds),
                StartedBy = IncidentAutomationConst.SystemActor
            };
            run.IdempotencyKey = string.Format(
                IncidentAutomationMessageConst.IdempotencyKeyFormat,
                incidentId,
                runNo);

            var insertedCount = await _runRep.InsertWithDiffLogAsync(run);
            if (insertedCount == 0)
                return LogSkip(incident, incidentId, triggerState, config.ID, IncidentAutomationConst.ScheduleReason.InsertFailed);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncidentAutomationConfig);

            // KHÔNG đánh dấu ProcessMode ở đây. Lập lịch chỉ là ý định, tự động chưa chạm vào sự cố.
            // Nếu người trực hủy hoặc bỏ qua trong khi còn đếm ngược thì sự cố được xử lý thủ công hoàn toàn,
            // đánh dấu ở mốc này sẽ ghi sai. Dấu được đặt trong ExecuteAsync sau khi áp kịch bản thành công.
            _logger.LogInformation(
                IncidentAutomationMessageConst.Log.ScheduledFormat,
                incidentId, run.ID, plan.ID, run.ExecuteDueAt);

            // KHÔNG phát realtime ở đây: cả hai nơi gọi TryScheduleAsync đều gọi IncidentBroadcaster.Notify
            // ngay sau đó trong cùng request (IncidentCommandHandler.Add -> Registration,
            // IncidentWorkflowCommandHandler -> Verified). Frontend nhận broadcast đó rồi nạp lại
            // ListByAccount, lúc này run đã tồn tại nên banner hiện lên đúng.
            return (run, IncidentAutomationConst.ScheduleReason.Scheduled);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Worker gọi hàm này khi ExecuteDueAt đã tới. Atomic claim run sang Starting,
        ///              tái kiểm tra incident, tự verify nếu cần (AutoVerifyIncident), rồi chuyển incident
        ///              sang InProgress bằng message nội bộ AutomationIncidentStateChange, dùng chung
        ///              logic chuyển trạng thái và sinh TmsWorkExecute với luồng thủ công.
        ///              Chưa gọi VMS/LCS - phần điều khiển thiết bị tạm hoãn.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task ExecuteAsync(string runId)
        {
            Debug.WriteLine(IncidentAutomationMessageConst.ExecuteDebugMarker);
            var run = await _runRep.GetFirstAsync(u => u.ID == runId && u.IsDelete == null);
            if (run == null)
                return;

            // Idempotent: nhịp quét lặp lại hoặc nhiều instance cùng chạy không được xử lý lại run này.
            if (run.Status != IncidentAutomationConst.RunStatus.WarningPending)
                return;

            var claimed = await ClaimRunStatusAsync(run, IncidentAutomationConst.RunStatus.Starting, u => u.StartedAt = BaseUtil.TimeServer.GetTimeServer());
            if (!claimed)
                return;

            TmsIncidentAutomationConfig? config;
            try
            {
                config = await ResolveRunConfigAsync(run);
            }
            catch (JsonException)
            {
                await FailRunAsync(run, IncidentAutomationMessageConst.ErrorCode.ConfigSnapshotInvalid,
                    IncidentAutomationMessageConst.ErrorMessage.ConfigSnapshotInvalid);
                return;
            }

            var incident = await _incidentRepo.GetFirstAsync(u => u.ID == run.IncidentId && u.IsDelete == null);
            if (incident != null) await this._groupResolver.PopulateGroupsAsync(new[] { incident });
            var incidentState = ConvertIncidentState(incident?.State);
            var isIncidentUsable = incident != null && IncidentEventGroupConst.AllowsProcessing(incident.EventGroup, incident.State)
                && incidentState != (int)BaseEnums.TmsIncidentStateEnum.Finished
                && incidentState != (int)BaseEnums.TmsIncidentStateEnum.Canceled
                && incidentState != (int)BaseEnums.TmsIncidentStateEnum.Skipped
                && incident.EventTypeId == run.EventTypeId;

            if (!isIncidentUsable)
            {
                await FailRunAsync(
                    run,
                    IncidentAutomationMessageConst.ErrorCode.IncidentNotUsable,
                    IncidentAutomationMessageConst.ErrorMessage.IncidentNotUsable);
                return;
            }

            // Nếu trigger từ Registration, tự verify trước khi chuyển InProgress (dùng chung UpdateState).
            if (run.TriggerState == IncidentAutomationConst.TriggerState.Registration
                && incidentState == (int)BaseEnums.TmsIncidentStateEnum.Registration)
            {
                var verifyResult = await _messageBus.InvokeAsync<bool>(new AutomationIncidentStateChange(
                    run.ID, run.TenantId, run.IncidentId!, (int)BaseEnums.TmsIncidentStateEnum.Verified));

                if (!verifyResult)
                {
                    await FailRunAsync(
                        run,
                        IncidentAutomationMessageConst.ErrorCode.AutoVerifyFailed,
                        IncidentAutomationMessageConst.ErrorMessage.AutoVerifyFailed);
                    return;
                }
            }

            if (!await ClaimRunStatusAsync(run, IncidentAutomationConst.RunStatus.Running)) return;

            // KienLT - 16/10/2026 (V08): từ đây run đã Running. Trước đây lỗi ngoài dự kiến (exception khi chuyển
            // Đang xử lý, khi ghi trạng thái...) thoát ra worker, run kẹt Running tới StuckTimeout (5 phút) mà không
            // để lại lý do. Nay bắt lại, đánh Failed kèm lý do ngay và bàn giao nếu sự cố đã Đang xử lý.
            try
            {
                var inProgressResult = await _messageBus.InvokeAsync<bool>(new AutomationIncidentStateChange(
                    run.ID, run.TenantId, run.IncidentId!, (int)BaseEnums.TmsIncidentStateEnum.InProgress));

                if (!inProgressResult)
                {
                    await FailRunAsync(
                        run,
                        IncidentAutomationMessageConst.ErrorCode.InProgressFailed,
                        IncidentAutomationMessageConst.ErrorMessage.InProgressFailed);
                    return;
                }

                // Đây là mốc tự động hóa THỰC SỰ áp kịch bản lên sự cố, nên mới đánh dấu chế độ tại đây.
                // Kết quả cuối cùng của sự cố vẫn do luồng thủ công ghi qua State/FinishedBy/FinishedDate.
                await SetProcessModeAsync(run.IncidentId, IncidentConst.ProcessMode.Auto);

                // Sau InProgress, auto xử lý từng TmsWorkExecute theo WorkType và
                // TmsTrafficPlanWorkDetail.State; chỉ work control có State=2 mới gọi
                // pipeline VMS/LCS và chỉ đánh Executed sau khi pipeline hoàn tất.
                bool workResult;
                try
                {
                    workResult = await ExecuteScenarioWorksAsync(run, incident!, config);
                }
                catch (Exception ex)
                {
                    await FailRunAsync(
                        run,
                        IncidentAutomationMessageConst.ErrorCode.AutoWorkExecuteFailed,
                        string.Format(IncidentAutomationMessageConst.ErrorMessage.AutoWorkExecuteFailedFormat, ex.Message),
                        handOver: true,
                        resetDevices: ShouldResetOnStop(config, null));
                    return;
                }

                if (!workResult)
                    return;

                var now = BaseUtil.TimeServer.GetTimeServer();

                if (config != null && config.IsAutoFinishEnabled && config.AutoFinishAfterSeconds > 0)
                {
                    // Chỉ ghi mốc AutoFinishAt vào DB; worker sẽ tự phát hiện khi tới hạn.
                    var autoFinishAt = now.AddSeconds(config.AutoFinishAfterSeconds!.Value);
                    await ClaimRunStatusAsync(run, IncidentAutomationConst.RunStatus.FinishPending, u =>
                    {
                        u.ActivatedAt = now;
                        u.AutoFinishAt = autoFinishAt;
                    });
                }
                else
                {
                    await ClaimRunStatusAsync(run, IncidentAutomationConst.RunStatus.Active, u => u.ActivatedAt = now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, IncidentAutomationMessageConst.Log.UnexpectedRunFailure, run.ID, run.IncidentId);

                // ClaimRunStatusAsync đổi Status trong bộ nhớ trước khi ghi DB: nạp lại để claim Failed so đúng
                // trạng thái đang nằm trong DB (nếu run đã sang trạng thái kết thúc thì bỏ qua).
                var current = await _runRep.GetFirstAsync(u => u.ID == run.ID && u.IsDelete == null);
                if (current == null || IncidentAutomationConst.TerminalRunStatuses.Contains(current.Status))
                    return;

                var inProgress = await IsIncidentInProgressAsync(current);
                await FailRunAsync(
                    current,
                    IncidentAutomationMessageConst.ErrorCode.UnexpectedError,
                    ex.Message,
                    handOver: inProgress,
                    resetDevices: inProgress && ShouldResetOnStop(config, null));
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Worker gọi hàm này khi AutoFinishAt đã tới. Auto chỉ dispatch command nội bộ
        ///              để gọi lại đúng VmsProcessService.ResetIncidentToDefault của luồng manual,
        ///              sau đó dùng chung UpdateState Finished; không chứa business logic reset riêng.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task FinishAsync(string runId)
        {
            var run = await _runRep.GetFirstAsync(u => u.ID == runId && u.IsDelete == null);
            if (run == null)
                return;

            if (run.Status != IncidentAutomationConst.RunStatus.FinishPending
                && run.Status != IncidentAutomationConst.RunStatus.Active)
                return;

            var claimed = await ClaimRunStatusAsync(run, IncidentAutomationConst.RunStatus.ResettingDefault);
            if (!claimed)
                return;

            TmsIncidentAutomationConfig? config;
            try
            {
                config = await ResolveRunConfigAsync(run);
            }
            catch (JsonException)
            {
                await FailRunAsync(run, IncidentAutomationMessageConst.ErrorCode.ConfigSnapshotInvalid,
                    IncidentAutomationMessageConst.ErrorMessage.ConfigSnapshotInvalid);
                return;
            }
            if (!string.Equals(config?.ResetPolicy, IncidentAutomationConst.ResetPolicy.NoReset, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Không gọi HTTP/controller và không tạo service reset thứ hai: command này chỉ là
                    // adapter message-bus trỏ thẳng vào hàm reset manual hiện hữu ở module VMS.
                    await _messageBus.InvokeAsync(new ResetIncidentToDefaultInput
                    {
                        IncidentId = run.IncidentId!
                    });
                }
                catch (Exception ex)
                {
                    await FailRunAsync(
                        run,
                        IncidentAutomationMessageConst.ErrorCode.ResetIncidentToDefaultFailed,
                        string.Format(
                            IncidentAutomationMessageConst.ErrorMessage.ResetIncidentToDefaultFailedFormat,
                            ex.Message));
                    return;
                }
            }

            // KienLT - 16/10/2026: reset có thể chạy lâu; run đã bị dọn treo/bàn giao trong lúc đó thì không tự đóng sự cố.
            var resettingStatus = IncidentAutomationConst.RunStatus.ResettingDefault;
            if (!await _runRep.IsAnyAsync(u => u.ID == run.ID && u.Status == resettingStatus))
                return;

            var finishResult = await _messageBus.InvokeAsync<bool>(new TmsIncidentStateInput
            {
                Id = run.IncidentId,
                State = ((int)BaseEnums.TmsIncidentStateEnum.Finished).ToString(),
                Actor = IncidentAutomationConst.SystemActor
            });

            if (!finishResult)
            {
                await FailRunAsync(
                    run,
                    IncidentAutomationMessageConst.ErrorCode.FinishIncidentFailed,
                    IncidentAutomationMessageConst.ErrorMessage.FinishIncidentFailed);
                return;
            }

            await ClaimRunStatusAsync(
                run,
                IncidentAutomationConst.RunStatus.Completed,
                u => u.FinishedAt = BaseUtil.TimeServer.GetTimeServer());
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Hủy run đang trong thời gian cảnh báo (WarningPending) hoặc chờ auto-finish
        ///              (Active/FinishPending). Không đụng tới incident - chỉ đóng run.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task<bool> CancelAsync(string runId, string? reason = null)
        {
            var run = await _runRep.GetFirstAsync(u => u.ID == runId && u.IsDelete == null);
            if (run == null || run.Status is not (IncidentAutomationConst.RunStatus.WarningPending
                or IncidentAutomationConst.RunStatus.Active or IncidentAutomationConst.RunStatus.FinishPending))
                return false;

            var now = BaseUtil.TimeServer.GetTimeServer();

            // Chỉ cần đổi Status sang Canceled: worker chỉ quét các run ở WarningPending/FinishPending
            // nên run đã hủy sẽ không bao giờ được chọn nữa. Không có job ngoài DB cần dọn.
            var canceled = await ClaimRunStatusAsync(run, IncidentAutomationConst.RunStatus.Canceled, u =>
            {
                u.CancelReason = reason;
                u.FinishedAt = now;
            });

            // KHÔNG ghi gì lên sự cố ở đây. Nếu việc hủy đến từ một thao tác thủ công (bỏ qua, hủy, tự chọn
            // kịch bản) thì luồng thủ công đã ghi State/FinishedBy/FinishedDate đúng rồi; ghi thêm một dấu
            // kết quả riêng của tự động hóa chỉ tạo ra bộ từ vựng song song, mâu thuẫn với State.
            //
            // Vẫn phải phát broadcast: hủy run không làm đổi state của sự cố nên luồng thủ công không phát,
            // không phát ở đây thì banner ở các máy trạm khác sẽ treo.
            if (canceled)
                BroadcastRunChange(run, IncidentAutomationConst.RunStatus.Canceled);

            return canceled;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Hủy idempotent toàn bộ run chưa kết thúc của một incident. Được gọi từ workflow
        ///              thủ công khi người dùng tự thực hiện transition (Skip/Cancel/chọn plan thủ công...)
        ///              để tránh automation run cũ chạy song song và ghi đè quyết định của người dùng.
        ///              Không throw nếu không có run nào cần hủy.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task CancelActiveRunsByIncidentAsync(string incidentId, string reason)
        {
            if (string.IsNullOrWhiteSpace(incidentId))
                return;

            var incident = await _incidentRepo.GetFirstAsync(x => x.ID == incidentId);
            if (incident == null) return;

            var activeRuns = await _runRep.GetListAsync(u =>
                u.IncidentId == incidentId
                && u.TenantId == incident.TenantId
                && u.IsDelete == null
                && !IncidentAutomationConst.TerminalRunStatuses.Contains(u.Status));

            foreach (var run in activeRuns)
                await CancelAsync(run.ID, reason);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Cho phép người vận hành bỏ qua thời gian cảnh báo còn lại và chạy ngay.
        ///              Kéo ExecuteDueAt về hiện tại rồi thực thi luôn trong request để phản hồi tức thì.
        ///              An toàn khi worker cũng vừa tới hạn: ClaimRunStatusAsync là compare-and-set nên chỉ
        ///              một bên claim được, bên còn lại thoát sớm mà không gây tác dụng phụ.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task<bool> ExecuteNowAsync(string runId)
        {
            var run = await _runRep.GetFirstAsync(u => u.ID == runId && u.IsDelete == null);
            if (run == null || run.Status != IncidentAutomationConst.RunStatus.WarningPending)
                return false;

            var now = BaseUtil.TimeServer.GetTimeServer();
            await _runRep.AsUpdateable()
                .SetColumns(u => new TmsIncidentAutomationRun { ExecuteDueAt = now })
                .Where(u => u.ID == run.ID)
                .ExecuteCommandAsync();

            await ExecuteAsync(run.ID);

            return true;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Lấy ID các run đã tới hạn cần worker xử lý. Tách riêng khỏi ExecuteAsync/FinishAsync
        ///              để worker chỉ giữ scope trong thời gian ngắn khi truy vấn, đồng thời giới hạn batch
        ///              tránh một nhịp quét xử lý quá nhiều run.
        /// Created date: 24/07/2026
        /// </summary>
        /// <summary>Restore only completion schedules already authorized by a run's frozen policy.</summary>
        public async Task<string?> RestoreFrozenFinishSchedulesAsync(int batchSize, string? afterId, CancellationToken ct)
        {
            var rows = await _runRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.Status == IncidentAutomationConst.RunStatus.Active
                    && u.AutoFinishAt == null && u.ActivatedAt != null && u.ConfigSnapshotJson != null)
                .WhereIF(afterId != null, u => SqlFunc.GreaterThan(u.ID, afterId))
                .OrderBy(u => u.ID).Take(batchSize).ToListAsync(ct);
            foreach (var run in rows)
            {
                ct.ThrowIfCancellationRequested();
                DateTime? finishAt;
                try { finishAt = IncidentAutomationConfigSnapshot.MissingFinishAt(run); }
                catch (JsonException)
                {
                    _logger.LogWarning("[TmsIncidentAutomationService] Cannot restore finish schedule for run {RunId}: invalid snapshot.", run.ID);
                    continue;
                }
                if (finishAt is null) continue;
                var due = finishAt.Value;
                var updated = await _runRep.AsUpdateable()
                    .SetColumns(u => new TmsIncidentAutomationRun
                    {
                        Status = IncidentAutomationConst.RunStatus.FinishPending,
                        AutoFinishAt = due, UpdateTime = BaseUtil.TimeServer.GetTimeServer()
                    })
                    .Where(u => u.ID == run.ID && u.TenantId == run.TenantId && u.IsDelete == null
                        && u.Status == IncidentAutomationConst.RunStatus.Active && u.AutoFinishAt == null
                        && u.ActivatedAt == run.ActivatedAt && u.ConfigSnapshotJson == run.ConfigSnapshotJson)
                    .Where(u => SqlFunc.Subqueryable<TmsIncident>()
                        .Where(i => i.ID == u.IncidentId && i.TenantId == u.TenantId && i.EventTypeId == u.EventTypeId
                            && i.IsDelete == null && i.State == "3" && i.ProcessMode == IncidentConst.ProcessMode.Auto).Any())
                    .ExecuteCommandAsync(ct);
                if (updated == 0) continue;
                run.AutoFinishAt = due;
                run.Status = IncidentAutomationConst.RunStatus.FinishPending;
                BroadcastRunChange(run, run.Status);
                _logger.LogInformation("[TmsIncidentAutomationService] Restored frozen finish schedule for run {RunId}, due {FinishAt}.", run.ID, due);
            }
            return rows.Count == batchSize ? rows[^1].ID : null;
        }

        public async Task<(List<string> ExecuteRunIds, List<string> FinishRunIds)> GetDueRunIdsAsync(int batchSize)
        {
            var now = BaseUtil.TimeServer.GetTimeServer();

            var executeRunIds = await _runRep.AsQueryable()
                .Where(u => u.IsDelete == null
                    && u.Status == IncidentAutomationConst.RunStatus.WarningPending
                    && u.ExecuteDueAt != null
                    && u.ExecuteDueAt <= now)
                .OrderBy(u => u.ExecuteDueAt)
                .Take(batchSize)
                .Select(u => u.ID)
                .ToListAsync();

            var finishRunIds = await _runRep.AsQueryable()
                .Where(u => u.IsDelete == null
                    && u.Status == IncidentAutomationConst.RunStatus.FinishPending
                    && u.AutoFinishAt != null
                    && u.AutoFinishAt <= now)
                .OrderBy(u => u.AutoFinishAt)
                .Take(batchSize)
                .Select(u => u.ID)
                .ToListAsync();

            return (executeRunIds, finishRunIds);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Cưỡng chế đóng các run bị treo ở trạng thái CHUYỂN TIẾP quá lâu.
        ///
        ///              Vì sao cần: nếu tiến trình chết đúng lúc đang chuyển trạng thái (deploy, IIS recycle,
        ///              crash), run đứng lại ở Starting/Running/Canceling/ResettingDefault. Khi đó:
        ///                - GetDueRunIdsAsync chỉ quét WarningPending và FinishPending nên worker KHÔNG bao giờ
        ///                  nhìn tới run này;
        ///                - TryScheduleWithReasonAsync lại coi nó là "đang chạy" (không thuộc TerminalRunStatuses)
        ///                  nên TỪ CHỐI lập lịch mới;
        ///                - job dọn cũng bỏ qua vì chỉ xoá trạng thái kết thúc.
        ///              Kết quả: sự cố đó bị khoá khỏi tự động hóa vĩnh viễn và không có gì tự gỡ.
        ///
        ///              Mốc so sánh dùng UpdateTime (thời điểm chuyển trạng thái gần nhất) chứ không phải
        ///              StartedAt, vì run có thể treo ở ResettingDefault - lúc đó StartedAt đã rất cũ và sẽ
        ///              làm hiểu sai độ "treo".
        /// Created date: 24/07/2026
        /// </summary>
        /// <param name="stuckThreshold">Ngưỡng thời gian coi là treo</param>
        /// <param name="batchSize">Giới hạn số run xử lý trong một nhịp</param>
        /// <returns>Số run đã bị cưỡng chế đóng</returns>
        public async Task<int> RecoverStuckRunsAsync(TimeSpan stuckThreshold, int batchSize)
        {
            var deadline = BaseUtil.TimeServer.GetTimeServer().Subtract(stuckThreshold);

            var stuckRuns = await _runRep.AsQueryable()
                .Where(u => u.IsDelete == null
                    && IncidentAutomationConst.TransitionalRunStatuses.Contains(u.Status)
                    && u.UpdateTime != null
                    && u.UpdateTime <= deadline)
                .OrderBy(u => u.UpdateTime)
                .Take(batchSize)
                .ToListAsync();

            if (stuckRuns == null || stuckRuns.Count == 0)
                return 0;

            var recovered = 0;
            foreach (var run in stuckRuns)
            {
                var previousStatus = run.Status;

                // Dùng ClaimRunStatusAsync để so-sánh-rồi-ghi: nếu một tiến trình khác vừa kịp chuyển
                // trạng thái run này thì lần cưỡng chế ở đây phải thất bại, không được ghi đè.
                var claimed = await ClaimRunStatusAsync(run, IncidentAutomationConst.RunStatus.Failed, u =>
                {
                    u.ErrorCode = IncidentAutomationMessageConst.ErrorCode.StuckTimeout;
                    u.ErrorMessage = string.Format(
                        IncidentAutomationMessageConst.ErrorMessage.StuckTimeoutFormat,
                        previousStatus,
                        stuckThreshold.TotalMinutes);
                    u.FinishedAt = BaseUtil.TimeServer.GetTimeServer();
                });

                if (!claimed)
                    continue;

                recovered++;

                // Không ghi dấu lên sự cố: ErrorCode = StuckTimeout trên run là đủ, và sự cố phải giữ nguyên
                // State để người trực tiếp tục xử lý thủ công.
                // KienLT - 16/10/2026: sự cố đã Đang xử lý thì bàn giao như run lỗi thường (bước lỗi về Chờ, reset
                // thiết bị theo FailurePolicy của cấu hình) - trước đây run treo để sự cố lơ lửng và giữ thiết bị.
                try
                {
                    if (await IsIncidentInProgressAsync(run))
                    {
                        TmsIncidentAutomationConfig? config = null;
                        try
                        {
                            config = await ResolveRunConfigAsync(run);
                        }
                        catch (JsonException)
                        {
                            // Snapshot hỏng: vẫn bàn giao, reset theo mặc định stop-reset.
                        }

                        await HandOverToManualAsync(run, ShouldResetOnStop(config, null));
                    }
                }
                catch (Exception ex)
                {
                    // Bước trả công việc sẽ được RepairPendingHandOversAsync làm nốt; vẫn phải phát realtime.
                    _logger.LogError(ex, IncidentAutomationMessageConst.Log.HandOverFailed, run.IncidentId, run.ID);
                }

                BroadcastRunChange(run, IncidentAutomationConst.RunStatus.Failed);

                _logger.LogWarning(
                    IncidentAutomationMessageConst.Log.StuckRecoveredFormat,
                    run.ID, run.IncidentId, previousStatus, run.UpdateTime);
            }

            return recovered;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Dọn (XOÁ CỨNG) các run đã kết thúc và cũ hơn số ngày cấu hình.
        ///
        ///              Vì sao xoá được: tự động hóa là lớp bổ trợ cho luồng thủ công. Dấu vết thao tác thật
        ///              đã nằm ở TmsWorkExecute / TmsWorkExecuteHistory (ExecuteBy, ExecuteDate, State,
        ///              ExecutionSource), còn kết quả ở cấp sự cố nằm ở TmsIncident.ProcessMode
        ///              (auto / auto-failed / auto-canceled). Bảng run chỉ là dữ liệu vận hành tạm.
        ///
        ///              Xoá cứng chứ không xoá mềm: xoá mềm vẫn giữ nguyên số dòng nên không đạt mục đích.
        ///
        ///              CHỈ xoá trạng thái đã kết thúc. Hệ quả: run treo ở trạng thái chuyển tiếp không bị
        ///              dọn ở đây - đó là việc của RecoverStuckRunsAsync, phải đưa chúng về Failed trước.
        /// Created date: 24/07/2026
        /// </summary>
        /// <param name="batchSize">Giới hạn số dòng xoá mỗi lần, tránh khoá bảng lâu</param>
        /// <returns>Số run đã xoá</returns>
        public async Task<int> PurgeFinishedRunsAsync(int batchSize)
        {
            var retentionDays = await GetRunRetentionDaysAsync();

            // 0 hoặc âm = tắt tính năng dọn.
            if (retentionDays <= 0)
                return 0;

            var cutoff = BaseUtil.TimeServer.GetTimeServer().AddDays(-retentionDays);

            var runIds = await _runRep.AsQueryable()
                .Where(u => IncidentAutomationConst.TerminalRunStatuses.Contains(u.Status)
                    && u.FinishedAt != null
                    && u.FinishedAt <= cutoff)
                .OrderBy(u => u.FinishedAt)
                .Take(batchSize)
                .Select(u => u.ID)
                .ToListAsync();

            if (runIds == null || runIds.Count == 0)
                return 0;

            var deleted = await _runRep.AsDeleteable()
                .Where(u => runIds.Contains(u.ID))
                .ExecuteCommandAsync();

            if (deleted > 0)
                _logger.LogInformation(
                    IncidentAutomationMessageConst.Log.PurgedFormat,
                    deleted, cutoff, retentionDays);

            return deleted;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Số ngày giữ lại lần chạy tự động đã kết thúc, đọc từ SysOpConfig.
        ///              GroupCode tms_incident_action_group, Code automation_run_retention_days.
        ///              Thiếu cấu hình hoặc giá trị không hợp lệ => mặc định DefaultRunRetentionDays.
        ///              Lỗi khi đọc cấu hình => trả 0 để TẮT dọn, an toàn hơn là xoá theo giá trị đoán.
        /// Created date: 24/07/2026
        /// </summary>
        private async Task<int> GetRunRetentionDaysAsync()
        {
            try
            {
                var configs = await _messageBus.InvokeAsync<List<SysOpConfigOutput>>(
                    new SysOpConfigInput { GroupCode = IncidentAutomationConst.ActionGroupConfigCode });

                var value = configs?.FirstOrDefault(config =>
                    string.Equals(config.Code, IncidentAutomationConst.RunRetentionDaysConfigCode, StringComparison.OrdinalIgnoreCase))?.Value;

                if (int.TryParse(value?.Trim(), out var days))
                    return days;

                return IncidentAutomationConst.DefaultRunRetentionDays;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, IncidentAutomationMessageConst.Log.RetentionReadFailed);
                return 0;
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Lấy run gần nhất (theo RunNo giảm dần) của một incident, dùng cho FE hiển thị
        ///              countdown/trạng thái hiện tại.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task<TmsIncidentAutomationRun?> GetLatestByIncidentAsync(string incidentId)
        {
            return await _runRep.AsQueryable()
                .Where(u => u.IncidentId == incidentId && u.IsDelete == null)
                .OrderBy(u => u.RunNo, OrderByType.Desc)
                .FirstAsync();
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Lấy các run đang chờ thực thi (WarningPending) hoặc đang chờ tự kết thúc
        ///              (Active/FinishPending), kèm thông tin sự cố/loại sự kiện/kịch bản.
        ///              Dùng cho banner cảnh báo hiển thị trên mọi màn hình.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task<List<IncidentAutomationRunViewOutput>> GetActiveRunViewsAsync()
        {
            var activeStatuses = new[]
            {
                IncidentAutomationConst.RunStatus.WarningPending,
                IncidentAutomationConst.RunStatus.Active,
                IncidentAutomationConst.RunStatus.FinishPending
            };

            var list = await _runRep.AsQueryable()
                .LeftJoin<TmsIncident>((r, i) => r.IncidentId == i.ID)
                .LeftJoin<TmsEventType>((r, i, ev) => r.EventTypeId == ev.ID)
                .LeftJoin<TmsTrafficPlans>((r, i, ev, p) => r.TrafficPlanId == p.ID)
                .Where((r, i, ev, p) => r.IsDelete == null && activeStatuses.Contains(r.Status))
                .Where((r, i, ev, p) => i.IsDelete == null)
                .OrderBy((r, i, ev, p) => r.ExecuteDueAt)
                .Select((r, i, ev, p) => new IncidentAutomationRunViewOutput
                {
                    IncidentCode = i.Code,
                    IncidentName = i.Name,
                    IncidentState = i.State,
                    IncidentStartDate = i.StartDate,
                    KmNumber = i.KmNumber,
                    MetNumber = i.MetNumber,
                    EventTypeName = ev.Name,
                    EventTypeUrl = ev.Url,
                    TrafficPlanName = p.Name
                }, true)
                .ToListAsync();

            var now = BaseUtil.TimeServer.GetTimeServer();
            list.ForEach(item => item.ServerTime = now);

            return list;
        }

        #region Private Functions ====================

        /// <summary>
        /// Author: SonTH
        /// Description: Compare-and-set theo ID + Status hiện tại (đọc từ tham số run) để tránh hai worker
        ///              cùng xử lý một run. Trả false nếu record đã bị người khác cập nhật trước.
        /// </summary>
        private async Task<bool> ClaimRunStatusAsync(TmsIncidentAutomationRun run, string newStatus, Action<TmsIncidentAutomationRun>? mutate = null)
        {
            var previousStatus = run.Status;
            run.Status = newStatus;
            mutate?.Invoke(run);

            int updatedCount = 0;
            // ChuongLP - 17/10/2026: kết nối SqlSugar dùng chung theo async-context có thể bị đóng ngang (vd ghi
            // AppDiffLog lỗi ngay trước đó) -> "The connection is closed". Thử lại tối đa 3 lần; claim là compare-and-set
            // nên an toàn khi thử lại, và lần trước lỡ ghi được thì nhận ra qua trạng thái đang nằm trong DB.
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    updatedCount = await _runRep.AsUpdateable(run)
                        .Where(u => u.ID == run.ID && u.Status == previousStatus)
                        .ExecuteCommandAsync();
                    if (updatedCount == 0 && attempt > 1)
                        updatedCount = await _runRep.IsAnyAsync(u => u.ID == run.ID && u.Status == newStatus) ? 1 : 0;
                    break;
                }
                catch (Exception ex) when (attempt < 3 && IsTransientDbError(ex))
                {
                    await Task.Delay(200 * attempt);
                }
                catch
                {
                    // KienLT - 16/10/2026: ghi DB lỗi thì trả lại Status trong bộ nhớ, nếu không lần claim sau (vd FailRunAsync
                    // trong catch) sẽ so với trạng thái chưa từng được ghi và trả 0 - run kẹt tới StuckTimeout.
                    run.Status = previousStatus;
                    throw;
                }
            }

            // KHÔNG phát realtime ở đây. Luồng thủ công là trung tâm: automation tái dùng UpdateState nên
            // IncidentBroadcaster.Notify của luồng thủ công đã tự phát ở mọi bước làm đổi state của incident
            // (Verified / InProgress / Finished). Phát thêm tại đây chỉ tạo message trùng.
            // Chỉ những chuyển trạng thái KHÔNG chạm tới incident mới cần phát riêng - xem CancelAsync/FailRunAsync.
            return updatedCount > 0;
        }

        /// <summary>Lỗi kết nối tạm thời (kết nối bị đóng / đang mở dở) - thử lại được.</summary>
        private static bool IsTransientDbError(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                var msg = e.Message ?? string.Empty;
                if (msg.Contains("connection is closed", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("connection was not closed", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("current state is connecting", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private async Task<TmsIncidentAutomationConfig?> ResolveRunConfigAsync(TmsIncidentAutomationRun run)
            => IncidentAutomationConfigSnapshot.Read(run)
                ?? await _configRep.GetFirstAsync(u => u.ID == run.AutomationConfigId);

        /// <param name="handOver">
        /// KienLT - 16/10/2026: sự cố đã ở Đang xử lý thì bàn giao cho người (xem <see cref="HandOverToManualAsync"/>).
        /// Lỗi trước khi sang Đang xử lý thì không cần: sự cố còn ở Đăng ký/Đã xác minh, người trực tự chọn kịch bản.
        /// </param>
        /// <param name="resetDevices">Có đưa thiết bị sự cố này đã điều khiển về mặc định không (xem <see cref="ShouldResetOnStop"/>).</param>
        private async Task FailRunAsync(TmsIncidentAutomationRun run, string errorCode, string errorMessage,
            bool handOver = false, bool resetDevices = false)
        {
            var failed = await ClaimRunStatusAsync(run, IncidentAutomationConst.RunStatus.Failed, u =>
            {
                u.ErrorCode = errorCode;
                u.ErrorMessage = errorMessage.Length <= EntityConst.Length512 ? errorMessage : errorMessage[..EntityConst.Length512];
                u.FinishedAt = BaseUtil.TimeServer.GetTimeServer();
            });

            // KHÔNG ghi dấu thất bại lên sự cố. Sự cố giữ nguyên State để người trực tiếp tục xử lý thủ công,
            // còn nguyên nhân lỗi nằm ở ErrorCode/ErrorMessage của run. Sau khi run bị dọn thì vẫn nhận ra
            // được tự động dừng giữa đường: ProcessMode = auto nhưng FinishedBy không phải SYSTEM_AUTOMATION.
            //
            // Run lỗi thì incident không đổi state (hoặc lệnh đổi state đã thất bại), nên luồng thủ công
            // không phát gì. Phải phát tại đây để banner đang đếm ngược được tắt.
            if (!failed)
                return;

            // Bàn giao TRƯỚC khi phát: FE nhận tin AutomationRun thì nạp lại ListByAccount, lúc đó công việc
            // đã về Chờ thì tổ chức được giao mới thấy.
            if (handOver)
                await HandOverToManualAsync(run, resetDevices);

            BroadcastRunChange(run, IncidentAutomationConst.RunStatus.Failed);
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Trả sự cố cho người trực sau khi run lỗi lúc sự cố đã Đang xử lý. Trước đây run chỉ bị đánh
        ///              Failed: bước điều khiển lỗi nằm ở Bỏ qua nên không tổ chức nào thấy để làm tay, supervisor
        ///              mất sự cố khỏi danh sách (chỉ hiện sự cố còn công việc Chờ), thiết bị đã gửi bị giữ mãi
        ///              trong TmsIncidentControl và chặn mọi sự cố sau (IncidentControlOccupied).
        ///              - Bước tự động bị lỗi của run này (Remark bắt đầu bằng AutoDeviceControlFailed) về Chờ, kèm
        ///                ghi chú; bước chưa chạy vốn đã ở Chờ; bước thủ công (State=1) và bước đã chạy giữ nguyên.
        ///              - resetDevices: gọi lại đúng luồng reset của Hoàn thành (ResetIncidentToDefault) - đưa thiết bị
        ///                sự cố này đã điều khiển về mặc định và xoá quyền giữ của nó.
        ///              Đổi trạng thái công việc trực tiếp vì handler TmsWorkExecuteStateInput chỉ nhận công việc đang
        ///              Chờ (luật cho thao tác của người dùng), còn đây là hệ thống hoàn tác việc chính nó vừa ghi.
        ///              Lỗi ở đây chỉ ghi log: run đã Failed, không được làm hỏng thêm.
        /// Created date: 16/10/2026
        /// </summary>
        private async Task HandOverToManualAsync(TmsIncidentAutomationRun run, bool resetDevices)
        {
            // Hai bước tách riêng để lỗi bước này không bỏ luôn bước kia. Bước trả công việc là luỹ đẳng và được
            // RepairPendingHandOversAsync thử lại; reset chỉ chạy một lần (thử lại sau khi người trực đã điều khiển tay
            // sẽ xoá nội dung của họ) - reset lỗi thì thiết bị vẫn thuộc sự cố đang sống, Hoàn thành sẽ reset.
            var reverted = 0;
            try
            {
                reverted = await RevertFailedWorksAsync(run);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, IncidentAutomationMessageConst.Log.HandOverFailed, run.IncidentId, run.ID);
            }

            if (resetDevices)
            {
                try
                {
                    await _messageBus.InvokeAsync(new ResetIncidentToDefaultInput { IncidentId = run.IncidentId! });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, IncidentAutomationMessageConst.Log.HandOverResetFailed, run.IncidentId, run.ID);
                }
            }

            _logger.LogInformation(IncidentAutomationMessageConst.Log.HandedOver, run.IncidentId, run.ID, reverted, resetDevices);
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Trả các bước tự động bị lỗi của run về Chờ. Cập nhật TỪNG dòng có điều kiện (đúng run, còn Bỏ qua,
        ///              Remark chưa đổi so với lúc đọc) để không ghi đè thao tác tay chen vào giữa lúc đọc và lúc ghi.
        ///              Luỹ đẳng: chạy lại chỉ còn chạm dòng chưa trả. Remark mới bắt đầu bằng AutoHandOver (không còn tiền
        ///              tố AutoDeviceControlFailed) nên người trực tự đánh Bỏ qua sau đó sẽ không bị trả lại lần nữa.
        /// Created date: 16/10/2026
        /// </summary>
        private async Task<int> RevertFailedWorksAsync(TmsIncidentAutomationRun run)
        {
            var incidentId = run.IncidentId;
            var skippedState = ((int)BaseEnums.TmsWorkExecuteStateEnum.Skipped).ToString();
            var waitingState = ((int)BaseEnums.TmsWorkExecuteStateEnum.Waiting).ToString();
            var failedPrefix = IncidentAutomationMessageConst.ErrorCode.AutoDeviceControlFailed;
            var failedWorks = await _workExecuteRep.AsQueryable()
                .Where(u => u.IncidentId == incidentId && u.AutomationRunId == run.ID
                    && u.State == skippedState && u.Remark != null && u.Remark.StartsWith(failedPrefix))
                .ToListAsync();

            var reverted = 0;
            var now = BaseUtil.TimeServer.GetTimeServer();
            var actor = IncidentAutomationConst.SystemActor;
            foreach (var work in failedWorks)
            {
                var workId = work.ID;
                var originalRemark = work.Remark;
                var remark = string.Format(IncidentAutomationMessageConst.Remark.HandOverFormat, originalRemark);
                remark = remark.Length <= EntityConst.RemarkFieldLength ? remark : remark[..EntityConst.RemarkFieldLength];

                reverted += await _workExecuteRep.AsUpdateable()
                    .SetColumns(u => new TmsWorkExecute
                    {
                        State = waitingState,
                        ExecuteBy = null,
                        ExecuteDate = null,
                        Remark = remark,
                        UpdateTime = now,
                        UpdateUId = actor
                    })
                    .Where(u => u.ID == workId && u.AutomationRunId == run.ID && u.State == skippedState
                        && u.Remark == originalRemark)
                    .ExecuteCommandAsync();
            }

            return reverted;
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Worker gọi định kỳ: run Failed mà sự cố còn Đang xử lý nhưng vẫn còn bước tự động bị lỗi chưa trả về
        ///              Chờ (bàn giao bị lỗi DB / tiến trình chết ngay sau khi claim Failed / run lỗi trước khi có bàn giao)
        ///              thì trả nốt và phát lại realtime. Không giới hạn tuổi run: điều kiện dừng là dữ liệu (bước đã trả có
        ///              tiền tố AutoHandOver nên tự rời tập ứng viên). Lỗi của một run không chặn các run khác.
        ///              Không reset thiết bị (xem HandOverToManualAsync).
        /// Created date: 16/10/2026
        /// </summary>
        public async Task<int> RepairPendingHandOversAsync(int batchSize)
        {
            var failedStatus = IncidentAutomationConst.RunStatus.Failed;
            var inProgress = ((int)BaseEnums.TmsIncidentStateEnum.InProgress).ToString();
            var skippedState = ((int)BaseEnums.TmsWorkExecuteStateEnum.Skipped).ToString();
            var failedPrefix = IncidentAutomationMessageConst.ErrorCode.AutoDeviceControlFailed;

            var runIds = await _workExecuteRep.AsQueryable()
                .Where(w => w.State == skippedState && w.AutomationRunId != null
                    && w.Remark != null && w.Remark.StartsWith(failedPrefix)
                    && SqlFunc.Subqueryable<TmsIncidentAutomationRun>()
                        .Where(r => r.ID == w.AutomationRunId && r.IsDelete == null && r.Status == failedStatus)
                        .Any()
                    && SqlFunc.Subqueryable<TmsIncident>()
                        .Where(i => i.ID == w.IncidentId && i.IsDelete == null && i.State == inProgress)
                        .Any())
                .Select(w => w.AutomationRunId)
                .Distinct()
                .Take(batchSize)
                .ToListAsync();

            var repaired = 0;
            foreach (var runId in runIds)
            {
                try
                {
                    var run = await _runRep.GetFirstAsync(u => u.ID == runId && u.IsDelete == null);
                    if (run == null)
                        continue;

                    if (await RevertFailedWorksAsync(run) > 0)
                    {
                        repaired++;
                        BroadcastRunChange(run, IncidentAutomationConst.RunStatus.Failed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, IncidentAutomationMessageConst.Log.HandOverFailed, null, runId);
                }
            }

            return repaired;
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Dừng vì lỗi thì có reset thiết bị về mặc định không. FailurePolicy rỗng/Inherit lấy của cấu hình,
        ///              cấu hình cũng rỗng thì theo mặc định stop-reset. continue không dừng nên không reset; cấu hình
        ///              ResetPolicy = no-reset thì không reset (giống luồng tự hoàn thành).
        /// Created date: 16/10/2026
        /// </summary>
        private static bool ShouldResetOnStop(TmsIncidentAutomationConfig? config, string? failurePolicy)
        {
            var policy = string.IsNullOrWhiteSpace(failurePolicy)
                || string.Equals(failurePolicy, IncidentAutomationConst.FailurePolicy.Inherit, StringComparison.OrdinalIgnoreCase)
                ? config?.FailurePolicy
                : failurePolicy;

            if (string.Equals(policy, IncidentAutomationConst.FailurePolicy.Continue, StringComparison.OrdinalIgnoreCase))
                return false;

            return !string.Equals(config?.ResetPolicy, IncidentAutomationConst.ResetPolicy.NoReset, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Sự cố của run đang ở Đang xử lý (còn sống) hay không - quyết định có bàn giao khi run lỗi.</summary>
        private Task<bool> IsIncidentInProgressAsync(TmsIncidentAutomationRun run)
        {
            var inProgress = ((int)BaseEnums.TmsIncidentStateEnum.InProgress).ToString();
            return _incidentRepo.IsAnyAsync(u => u.ID == run.IncidentId && u.IsDelete == null && u.State == inProgress);
        }

        private int ConvertIncidentState(string? state)
        {
            if (string.IsNullOrWhiteSpace(state) || !int.TryParse(state, out var value))
                return -1;
            return value;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Ghi log lý do KHÔNG lập lịch được rồi trả về kết quả rỗng kèm mã lý do.
        ///              Mức Information vì đây là trạng thái hợp lệ và rất phổ biến - phần lớn loại sự kiện
        ///              không bật tự động nên nhánh NoConfig chạy liên tục; dùng Warning sẽ làm nhiễu log.
        /// Created date: 24/07/2026
        /// </summary>
        private (TmsIncidentAutomationRun?, string) LogSkip(
            TmsIncident? incident, string? incidentId, string? triggerState, string? configId, string reasonCode)
        {
            _logger.LogInformation(
                IncidentAutomationMessageConst.Log.SkipScheduleFormat,
                reasonCode, incidentId, incident?.EventTypeId, incident?.Source, triggerState, configId);

            return (null, reasonCode);
        }

        /// <summary>
        /// Applies the work rows created by the existing InProgress state transition.
        /// Manual remains the source of truth for creating and changing work rows; this
        /// method only integrates the automatic run after those rows exist.
        /// </summary>
        private async Task<bool> ExecuteScenarioWorksAsync(
            TmsIncidentAutomationRun run,
            TmsIncident incident,
            TmsIncidentAutomationConfig? config)
        {
            var waitingState = ((int)BaseEnums.TmsWorkExecuteStateEnum.Waiting).ToString();
            var works = await _workExecuteRep.AsQueryable()
                .Where(u => u.IncidentId == incident.ID
                    && u.TrafficPlanId == run.TrafficPlanId
                    && u.State == waitingState)
                .ToListAsync();

            if (works == null || works.Count == 0)
                return true;

            // Keep the automatic audit on the same TmsWorkExecute rows that the manual
            // state machine created. No second action table or second work is created.
            await _workExecuteRep.AsUpdateable()
                .SetColumns(u => new TmsWorkExecute
                {
                    ExecutionSource = IncidentAutomationMessageConst.AutomaticExecutionSource,
                    AutomationRunId = run.ID
                })
                .Where(u => u.IncidentId == incident.ID
                    && u.TrafficPlanId == run.TrafficPlanId
                    && u.State == waitingState)
                .ExecuteCommandAsync();

            var detailIds = works
                .Where(u => !string.IsNullOrWhiteSpace(u.WorkDetailId))
                .Select(u => u.WorkDetailId!)
                .Distinct()
                .ToList();

            var details = detailIds.Count == 0
                ? new List<TmsWorkDetail>()
                : await _workDetailRep.AsQueryable()
                    .Where(u => detailIds.Contains(u.ID) && u.IsDelete == null)
                    .ToListAsync();

            var planLinks = detailIds.Count == 0 || string.IsNullOrWhiteSpace(run.TrafficPlanId)
                ? new List<TmsTrafficPlanWorkDetails>()
                : await _trafficPlanWorkDetailRep.AsQueryable()
                    .Where(u => u.TrafficPlanId == run.TrafficPlanId
                        && detailIds.Contains(u.WorkDetailId)
                        && u.IsDelete == null)
                    .ToListAsync();

            var detailsById = details
                .Where(u => !string.IsNullOrWhiteSpace(u.ID))
                .GroupBy(u => u.ID)
                .ToDictionary(g => g.Key, g => g.First());

            var orderedWorks = works
                .OrderBy(u => u.Step ?? int.MaxValue)
                .ThenBy(u => GetWorkOrderNo(u, planLinks))
                .ThenBy(u => u.CreateTime)
                .ToList();

            var runningStatus = IncidentAutomationConst.RunStatus.Running;
            foreach (var work in orderedWorks)
            {
                // KienLT - 16/10/2026: run đã bị nơi khác chuyển trạng thái (dọn treo StuckTimeout, huỷ) thì dừng ngay,
                // không điều khiển tiếp thiết bị của sự cố đã được bàn giao cho người.
                if (!await _runRep.IsAnyAsync(u => u.ID == run.ID && u.Status == runningStatus))
                    return false;

                detailsById.TryGetValue(work.WorkDetailId ?? string.Empty, out var detail);
                var workType = NormalizeWorkValue(detail?.WorkType);
                var planLink = FindPlanLink(work, planLinks);

                // TmsTrafficPlanWorkDetail.State is the work-level mode selected in the
                // scenario: 1 = Manual (skip in an automatic run), 2 = Auto (execute).
                // WorkMode on TmsWorkDetail/TmsWorkExecute describes the manual flow and
                // must not override this per-plan decision.
                if (!IsAutomaticPlanWork(planLink))
                {
                    var stateRemark = planLink == null
                        ? IncidentAutomationMessageConst.Remark.PlanStateMissing
                        : string.Format(IncidentAutomationMessageConst.Remark.PlanStateFormat, planLink.State);
                    await UpdateAutomaticWorkStateAsync(
                        work.ID,
                        BaseEnums.TmsWorkExecuteStateEnum.Skipped,
                        stateRemark);
                    continue;
                }

                // Non-device work has no device API to call. It is still an automatic
                // work item, so mark it executed through the same work state handler.
                if (!string.Equals(workType, IncidentAutomationMessageConst.AutomaticWorkType, StringComparison.OrdinalIgnoreCase))
                {
                    await UpdateAutomaticWorkStateAsync(
                        work.ID,
                        BaseEnums.TmsWorkExecuteStateEnum.Executed,
                        IncidentAutomationMessageConst.Remark.WorkCompleted);
                    continue;
                }

                var controlType = NormalizeWorkValue(detail?.ControlType);
                if (string.IsNullOrWhiteSpace(controlType))
                    controlType = NormalizeWorkValue(work.ControlType);

                try
                {
                    string appliedRemark;
                    if (controlType == VmsDeviceTypeConst.VMS)
                    {
                        appliedRemark = await ApplyVmsWorkAsync(incident, planLink);
                    }
                    else if (controlType == VmsDeviceTypeConst.LCS)
                    {
                        appliedRemark = await ApplyLcsWorkAsync(incident, planLink);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                IncidentAutomationMessageConst.ReasonFormat.UnsupportedControlType,
                                IncidentAutomationMessageConst.ErrorCode.UnsupportedControlType,
                                controlType,
                                work.ID));
                    }

                    // Only mark Executed after the existing VMS/LCS pipeline has returned for at least one device.
                    await UpdateAutomaticWorkStateAsync(
                        work.ID,
                        BaseEnums.TmsWorkExecuteStateEnum.Executed,
                        appliedRemark);
                }
                catch (NoRecommendedEquipmentException ex) when (controlType == VmsDeviceTypeConst.LCS)
                {
                    // Đoạn tuyến không có LCS trong phạm vi là chuyện bình thường: chỉ bỏ qua bước LCS,
                    // không làm hỏng cả lượt chạy (VMS và các bước khác vẫn tiếp tục).
                    await UpdateAutomaticWorkStateAsync(
                        work.ID,
                        BaseEnums.TmsWorkExecuteStateEnum.Skipped,
                        ex.Message);
                    _logger.LogWarning(IncidentAutomationMessageConst.Log.SkipLcsNoEquipment, work.ID, ex.Message);
                    continue;
                }
                catch (Exception ex)
                {
                    var reason = string.Format(
                        IncidentAutomationMessageConst.ReasonFormat.AutoDeviceControlFailed,
                        IncidentAutomationMessageConst.ErrorCode.AutoDeviceControlFailed,
                        ex.Message);
                    await UpdateAutomaticWorkStateAsync(
                        work.ID,
                        BaseEnums.TmsWorkExecuteStateEnum.Skipped,
                        reason);

                    var failurePolicy = planLink?.FailurePolicy;
                    if (string.IsNullOrWhiteSpace(failurePolicy)
                        || string.Equals(failurePolicy, IncidentAutomationConst.FailurePolicy.Inherit, StringComparison.OrdinalIgnoreCase))
                        failurePolicy = config?.FailurePolicy;

                    if (string.Equals(
                        failurePolicy,
                        IncidentAutomationConst.FailurePolicy.Continue,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            ex,
                            IncidentAutomationMessageConst.Log.ContinueWorkFailure,
                            work.ID);
                        continue;
                    }

                    // KienLT - 16/10/2026: dừng thì bàn giao cho người; stop-reset/retry-stop còn reset thiết bị
                    // đã gửi về mặc định (trước đây tên chính sách có "reset" nhưng không reset gì).
                    await FailRunAsync(
                        run,
                        IncidentAutomationMessageConst.ErrorCode.AutoDeviceControlFailed,
                        reason,
                        handOver: true,
                        resetDevices: ShouldResetOnStop(config, failurePolicy));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Áp lệnh điều khiển VMS tự động cho một bước công việc của kịch bản.
        /// Note: planLink là bước kịch bản (TmsTrafficPlanWorkDetail) đang chạy - nơi cấu hình
        ///       EquipmentEventRuleId dùng để chọn thiết bị. Tham số TmsWorkExecute đã bỏ vì không dùng.
        /// </summary>
        private async Task<string> ApplyVmsWorkAsync(
            TmsIncident incident,
            TmsTrafficPlanWorkDetails? planLink)
        {
            var (equipmentTypeId, recommended) = await GetRecommendedEquipmentAsync(
                incident,
                VmsDeviceTypeConst.VMS,
                planLink);

            var eventDefault = await GetEventDefaultAsync(incident.EventTypeId, equipmentTypeId);
            var defaultConfig = ParseJsonObject(eventDefault?.DefaultConfig);
            var rowData = ParseJsonObject(eventDefault?.RowData);

            var content1 = ReadJsonString(
                    rowData,
                    IncidentAutomationMessageConst.ContentField.Row1,
                    IncidentAutomationMessageConst.ContentField.Row1Lower)
                ?? ReadJsonString(
                    defaultConfig,
                    IncidentAutomationMessageConst.ContentField.Row1,
                    IncidentAutomationMessageConst.ContentField.Row1Lower);
            var content2 = ReadJsonString(
                    rowData,
                    IncidentAutomationMessageConst.ContentField.Row2,
                    IncidentAutomationMessageConst.ContentField.Row2Lower)
                ?? ReadJsonString(
                    defaultConfig,
                    IncidentAutomationMessageConst.ContentField.Row2,
                    IncidentAutomationMessageConst.ContentField.Row2Lower);
            var url = eventDefault?.Url ?? ReadJsonString(
                defaultConfig,
                IncidentAutomationMessageConst.ContentField.ImageUrl,
                IncidentAutomationMessageConst.ContentField.Url,
                IncidentAutomationMessageConst.ContentField.UrlLower);

            var (playMode, ttl) = await ResolveVmsPlayModeAsync(incident, eventDefault);

            var input = new AddControlInput
            {
                DeviceType = VmsDeviceTypeConst.VMS,
                IncidentId = incident.ID,
                PlayMode = playMode,
                TimeToLive = ttl,
                Brightness = IncidentAutomationMessageConst.DefaultBrightness,
                Priority = IncidentAutomationMessageConst.DefaultPriority,
                Size = ReadJsonString(
                           defaultConfig,
                           IncidentAutomationMessageConst.ContentField.Size,
                           IncidentAutomationMessageConst.ContentField.SizeLower)
                       ?? IncidentAutomationMessageConst.DefaultVmsSize,
                Color = eventDefault?.Color ?? ReadJsonString(
                    defaultConfig,
                    IncidentAutomationMessageConst.ContentField.Color,
                    IncidentAutomationMessageConst.ContentField.ColorLower),
                BackColor = eventDefault?.BackColor ?? ReadJsonString(
                    defaultConfig,
                    IncidentAutomationMessageConst.ContentField.BackColor,
                    IncidentAutomationMessageConst.ContentField.BackColorLower),
                Font = eventDefault?.Font ?? ReadJsonString(
                    defaultConfig,
                    IncidentAutomationMessageConst.ContentField.Font,
                    IncidentAutomationMessageConst.ContentField.FontLower),
                FontSize = ReadJsonString(
                    defaultConfig,
                    IncidentAutomationMessageConst.ContentField.FontSize,
                    IncidentAutomationMessageConst.ContentField.FontSizeLower),
                EquipmentList = ToControlEquipmentList(recommended)
            };

            // VMS event-defaults created by FE are text (RowData). If an image-only
            // default was configured, use the image branch so ControlVmsCommandHandler
            // receives the correct content ControlType.
            if (!string.IsNullOrWhiteSpace(content1) && !string.IsNullOrWhiteSpace(content2))
            {
                input.ControlType = VmsControlTypeConst.Text;
                input.Content1 = ReplaceIncidentTokens(content1, incident.Metadata);
                input.Content2 = ReplaceIncidentTokens(content2, incident.Metadata);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(url))
                    throw new InvalidOperationException(
                        string.Format(
                            IncidentAutomationMessageConst.ReasonFormat.CodeMessage,
                            IncidentAutomationMessageConst.ErrorCode.NoRecommendedContent,
                            string.Format(
                                IncidentAutomationMessageConst.ErrorMessage.NoRecommendedContentFormat,
                                VmsDeviceTypeConst.VMS,
                                incident.EventTypeId)));

                input.ControlType = VmsControlTypeConst.Image;
                input.Url = url;
            }

            // Chốt quyền sở hữu thiết bị trước khi gửi: thiết bị đang bị sự cố KHÁC chiếm thì bỏ qua thiết bị đó.
            // Trước đây dòng này bị comment ở nhánh VMS nhưng vẫn bật ở nhánh LCS, dẫn tới VMS tự động
            // có thể ghi đè nội dung của sự cố khác.
            // ChuongLP: Append thì không chặn; Override chỉ bỏ thiết bị đang bị sự cố ưu tiên cao hơn giữ (DEC-09).
            return await SendToFreeEquipmentAsync(incident.ID, input);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Áp lệnh điều khiển LCS tự động cho một bước công việc của kịch bản.
        /// Note: LCS chỉ hỗ trợ nội dung hình ảnh.
        /// </summary>
        /// <summary>
        /// Author: ChuongLP
        /// Description: Chế độ phát VMS của sự cố (dùng chung bước VMS và LCS để LCS đồng nhất với VMS).
        ///              Thứ tự: VmsEventDefault (loại VMS) -> TmsEventType -> SysOpConfig (vms_config) -> override / 10s.
        /// Created date: 19/10/2026
        /// </summary>
        private async Task<(string PlayMode, int Ttl)> ResolveVmsPlayModeAsync(TmsIncident incident, VmsEventDefaultOutput? vmsEventDefault)
        {
            string? playMode = vmsEventDefault?.DefaultPlayMode;
            int? ttl = vmsEventDefault?.TimeToLive;

            if (string.IsNullOrEmpty(playMode))
            {
                var eventType = await _eventTypes.GetFirstAsync(e => e.ID == incident.EventTypeId);
                playMode = eventType?.DefaultPlayMode;
            }

            if (string.IsNullOrEmpty(playMode) || ttl == null)
            {
                try
                {
                    var vmsConfigs = await _messageBus.InvokeAsync<List<SysOpConfigOutput>>(
                        new SysOpConfigInput { GroupCode = VmsSysOpConfigConst.GroupCode });
                    if (string.IsNullOrEmpty(playMode))
                    {
                        playMode = vmsConfigs?.FirstOrDefault(c => string.Equals(c.Code, VmsSysOpConfigConst.DefaultPlayMode, StringComparison.OrdinalIgnoreCase))?.Value;
                    }
                    if (ttl == null)
                    {
                        var ttlVal = vmsConfigs?.FirstOrDefault(c => string.Equals(c.Code, VmsSysOpConfigConst.DefaultTimeToLive, StringComparison.OrdinalIgnoreCase))?.Value;
                        if (int.TryParse(ttlVal?.Trim(), out var parsedTtl))
                            ttl = parsedTtl;
                    }
                }
                catch
                {
                    // Fallback to default
                }
            }

            if (string.IsNullOrEmpty(playMode)) playMode = VmsPlayModeConst.Override;
            if (ttl == null || ttl <= 0) ttl = VmsSysOpConfigConst.DefaultTimeToLiveValue;

            return (playMode, ttl.Value);
        }

        /// <summary>
        /// Chế độ phát VMS của sự cố khi đang ở bước LCS: đọc VmsEventDefault của loại thiết bị VMS. Loại sự kiện chưa
        /// cấu hình nội dung VMS thì vẫn rơi về TmsEventType / SysOpConfig (không làm hỏng bước LCS).
        /// </summary>
        private async Task<string> ResolveVmsPlayModeForLcsAsync(TmsIncident incident)
        {
            VmsEventDefaultOutput? vmsEventDefault = null;
            try
            {
                var vmsEquipmentTypeId = await ResolveEquipmentTypeIdByDeviceTypeAsync(VmsDeviceTypeConst.VMS);
                vmsEventDefault = await GetEventDefaultAsync(incident.EventTypeId, vmsEquipmentTypeId);
            }
            catch (InvalidOperationException)
            {
            }

            var (playMode, _) = await ResolveVmsPlayModeAsync(incident, vmsEventDefault);
            return playMode;
        }

        private async Task<string> ApplyLcsWorkAsync(
            TmsIncident incident,
            TmsTrafficPlanWorkDetails? planLink)
        {
            var (equipmentTypeId, recommended) = await GetRecommendedEquipmentAsync(
                incident,
                VmsDeviceTypeConst.LCS,
                planLink);

            var eventDefault = await GetEventDefaultAsync(incident.EventTypeId, equipmentTypeId);
            var url = eventDefault?.Url
                ?? ReadJsonString(
                    ParseJsonObject(eventDefault?.DefaultConfig),
                    IncidentAutomationMessageConst.ContentField.ImageUrl,
                    IncidentAutomationMessageConst.ContentField.Url,
                    IncidentAutomationMessageConst.ContentField.UrlLower);
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException(
                    string.Format(
                        IncidentAutomationMessageConst.ReasonFormat.CodeMessage,
                        IncidentAutomationMessageConst.ErrorCode.NoRecommendedContent,
                        string.Format(
                            IncidentAutomationMessageConst.ErrorMessage.NoRecommendedContentFormat,
                            VmsDeviceTypeConst.LCS,
                            incident.EventTypeId)));

            var input = new AddControlInput
            {
                // LCS always uses image content; it must never be routed to VMS text.
                ControlType = VmsControlTypeConst.Image,
                DeviceType = VmsDeviceTypeConst.LCS,
                IncidentId = incident.ID,
                // LCS đồng nhất với VMS: dùng chế độ phát VMS của sự cố (append giữ quyền sự cố khác, override thì dọn)
                PlayMode = await ResolveVmsPlayModeForLcsAsync(incident),
                Url = url,
                Size = IncidentAutomationMessageConst.DefaultLcsSize,
                Brightness = IncidentAutomationMessageConst.DefaultBrightness,
                Priority = IncidentAutomationMessageConst.DefaultPriority,
                EquipmentList = ToControlEquipmentList(recommended)
            };

            // ChuongLP - 19/10/2026: LCS theo cùng quy tắc VMS. Override: bỏ LCS đang bị sự cố ưu tiên cao hơn giữ (DEC-09).
            // Append: không chặn ở đây - LcsImageCommandHandler ghi nhận chờ cho LCS đang hiện sự cố ưu tiên cao hơn.
            return await SendToFreeEquipmentAsync(incident.ID, input);
        }

        /// <summary>
        /// Author: SonTH, ChuongLP
        /// Description: Chọn danh sách thiết bị cho một bước điều khiển TỰ ĐỘNG.
        ///
        /// Thứ tự quyết định:
        ///   1. Đọc rule chọn thiết bị từ bước kịch bản (TmsTrafficPlanWorkDetail.EquipmentEventRuleId).
        ///      Rule quyết định LOẠI THIẾT BỊ, phía TRƯỚC/SAU và SỐ LƯỢNG TỐI ĐA.
        ///      Chưa cấu hình rule -> suy loại thiết bị theo ControlType (VMS/LCS) và để endpoint gợi ý
        ///      tự tra rule theo cặp EventType + EquipmentType (tương thích dữ liệu kịch bản cũ).
        ///   2. Phạm vi tích sẵn đọc từ SysOpConfig - CÙNG nguồn với FE, để tự động và thủ công
        ///      luôn chọn ra cùng một tập thiết bị.
        ///   3. Chỉ nhận thiết bị IsRecommended = true (đã thỏa: trong phạm vi gần + đúng hướng
        ///      + thuộc tuyến + đúng Before/After), sắp xếp gần -> xa rồi cắt theo MaxTargetCount.
        /// </summary>
        private async Task<(string EquipmentTypeId, List<TmsEquipmentDataOutput> Recommended)> GetRecommendedEquipmentAsync(
            TmsIncident incident,
            string deviceType,
            TmsTrafficPlanWorkDetails? planLink)
        {
            var rule = await ResolveEquipmentEventRuleAsync(incident, planLink);

            // Loại thiết bị: ưu tiên cấu hình trên rule, không có thì suy theo ControlType của bước.
            var equipmentTypeId = string.IsNullOrWhiteSpace(rule?.EquipmentTypeId)
                ? await ResolveEquipmentTypeIdByDeviceTypeAsync(deviceType)
                : rule!.EquipmentTypeId!;

            var suggestionSmallRange = await GetEquipmentSuggestionSmallRangeAsync();

            // IsExpand/IsWholeRoute = false: luồng tự động KHÔNG mở rộng phạm vi. Mở rộng chỉ để người
            // vận hành nhìn thêm ứng viên rồi tự tick, không phải căn cứ để máy tự bắn lệnh.
            var candidates = await _messageBus.InvokeAsync<List<TmsEquipmentDataOutput>>(
                new TmsEquipmentSuggestionInput
                {
                    KmNumber = incident.KmNumber,
                    MetNumber = incident.MetNumber,
                    DirectionId = incident.InfluenceScope,
                    Location = incident.Location,
                    EventTypeId = incident.EventTypeId,
                    EquipmentTypeId = equipmentTypeId,
                    // Rule đã chỉ rõ phía trước/sau -> khoá luôn để rule khác của cùng cặp không nới lỏng.
                    ProcessType = rule?.ProcessType,
                    SuggestionSmallRange = suggestionSmallRange,
                    SuggestionLongRange = suggestionSmallRange,
                    IsExpand = false,
                    IsWholeRoute = false
                });

            var recommended = candidates?
                .Where(u => u.IsRecommended == true && !string.IsNullOrWhiteSpace(u.ID))
                .GroupBy(u => u.ID)
                .Select(g => g.First())
                .OrderBy(u => u.DistanceMeters ?? int.MaxValue)
                .ToList() ?? new List<TmsEquipmentDataOutput>();

            // Giới hạn số thiết bị theo rule: giữ N thiết bị GẦN sự cố nhất.
            if (rule?.MaxTargetCount is int maxTargetCount
                && maxTargetCount > 0
                && recommended.Count > maxTargetCount)
            {
                recommended = recommended.Take(maxTargetCount).ToList();
            }

            if (recommended.Count == 0)
                throw new NoRecommendedEquipmentException(
                    string.Format(
                        IncidentAutomationMessageConst.ReasonFormat.NoRecommendedEquipment,
                        IncidentAutomationMessageConst.ErrorCode.NoRecommendedEquipment,
                        deviceType,
                        incident.ID,
                        suggestionSmallRange));

            return (equipmentTypeId, recommended);
        }

        /// <summary>
        /// Không có thiết bị nào được gợi ý trong phạm vi. Tách riêng để bước LCS được bỏ qua thay vì làm hỏng lượt chạy.
        /// </summary>
        private sealed class NoRecommendedEquipmentException : InvalidOperationException
        {
            public NoRecommendedEquipmentException(string message) : base(message) { }
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Đọc rule chọn thiết bị gắn trên bước kịch bản. Trả null khi bước chưa cấu hình rule
        ///              (dữ liệu kịch bản cũ) để giữ tương thích.
        /// Throw khi: rule đã cấu hình nhưng không dùng được (bị xóa / Disable / khác loại sự kiện với sự cố).
        ///            Đây là lỗi cấu hình, phải fail rõ ràng chứ không âm thầm chọn thiết bị theo cách khác.
        /// </summary>
        private async Task<TmsEquipmentEventRule?> ResolveEquipmentEventRuleAsync(
            TmsIncident incident,
            TmsTrafficPlanWorkDetails? planLink)
        {
            var ruleId = planLink?.EquipmentEventRuleId;
            if (string.IsNullOrWhiteSpace(ruleId))
                return null;

            var rule = await _equipmentEventRuleRep.GetFirstAsync(u =>
                u.ID == ruleId
                && u.IsDelete == null
                && u.Status == BaseEnums.StatusEnum.Enable);

            if (rule == null)
                throw new InvalidOperationException(
                    string.Format(
                        IncidentAutomationMessageConst.ReasonFormat.CodeMessage,
                        IncidentAutomationMessageConst.ErrorCode.EquipmentEventRuleNotUsable,
                        string.Format(
                            IncidentAutomationMessageConst.ErrorMessage.EquipmentEventRuleNotUsableFormat,
                            ruleId)));

            // Rule phải cùng loại sự kiện với sự cố, nếu không sẽ chọn thiết bị theo chính sách của
            // một loại sự kiện khác.
            if (!string.IsNullOrWhiteSpace(rule.EventTypeId)
                && !string.Equals(rule.EventTypeId, incident.EventTypeId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    string.Format(
                        IncidentAutomationMessageConst.ReasonFormat.CodeMessage,
                        IncidentAutomationMessageConst.ErrorCode.EquipmentEventRuleNotUsable,
                        string.Format(
                            IncidentAutomationMessageConst.ErrorMessage.EquipmentEventRuleEventTypeMismatchFormat,
                            ruleId,
                            rule.EventTypeId,
                            incident.EventTypeId)));
            }

            return rule;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Suy TmsEquipmentType.ID từ ControlType của bước (VMS/LCS) khi rule chưa cấu hình
        ///              loại thiết bị.
        /// </summary>
        private async Task<string> ResolveEquipmentTypeIdByDeviceTypeAsync(string deviceType)
        {
            var equipmentTypeCode = string.Equals(deviceType, VmsDeviceTypeConst.LCS, StringComparison.OrdinalIgnoreCase)
                ? IncidentAutomationConst.DeviceType.Lcs
                : IncidentAutomationConst.DeviceType.Vms;

            var equipmentTypes = await _messageBus.InvokeAsync<List<EquipmentTypeOutput>>(
                new EquipmentTypeInput
                {
                    Code = equipmentTypeCode,
                    IncludeDelete = false
                });

            var equipmentTypeId = equipmentTypes?
                .FirstOrDefault(u => string.Equals(u.Code?.Trim(), equipmentTypeCode, StringComparison.OrdinalIgnoreCase))?.ID
                ?? equipmentTypes?.FirstOrDefault()?.ID;

            if (string.IsNullOrWhiteSpace(equipmentTypeId))
                throw new InvalidOperationException(
                    string.Format(
                        IncidentAutomationMessageConst.ReasonFormat.CodeMessage,
                        IncidentAutomationMessageConst.ErrorCode.EquipmentTypeNotConfigured,
                        string.Format(
                            IncidentAutomationMessageConst.ErrorMessage.EquipmentTypeNotConfiguredFormat,
                            equipmentTypeCode)));

            return equipmentTypeId;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Đọc phạm vi gần (mét) từ SysOpConfig - ĐÚNG mã cấu hình mà FE dùng, để tự động và
        ///              thủ công chọn ra cùng một tập thiết bị.
        /// Note: thiếu/sai cấu hình thì fallback về EquipmentSuggestionConst.DefaultSmallRangeMeters,
        ///       cùng giá trị với endpoint gợi ý và với FE (trước đây backend fallback 15000 nên lệch).
        /// </summary>
        private async Task<int> GetEquipmentSuggestionSmallRangeAsync()
        {
            try
            {
                var configs = await _messageBus.InvokeAsync<List<SysOpConfigOutput>>(
                    new SysOpConfigInput { GroupCode = EquipmentSuggestionConst.OpConfig.GroupCode });

                var value = configs?.FirstOrDefault(config =>
                    string.Equals(
                        config.Code,
                        EquipmentSuggestionConst.OpConfig.SmallRangeCode,
                        StringComparison.OrdinalIgnoreCase))?.Value;

                if (int.TryParse(value?.Trim(), out var range) && range > 0)
                    return range;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    IncidentAutomationMessageConst.Log.SuggestionRangeReadFailed,
                    EquipmentSuggestionConst.DefaultSmallRangeMeters);
            }

            return EquipmentSuggestionConst.DefaultSmallRangeMeters;
        }

        private async Task<VmsEventDefaultOutput?> GetEventDefaultAsync(
            string? eventTypeId,
            string equipmentTypeId)
        {
            if (string.IsNullOrWhiteSpace(eventTypeId))
                throw new InvalidOperationException(
                    $"{IncidentAutomationMessageConst.ErrorCode.IncidentEventTypeRequired}: "
                    + IncidentAutomationMessageConst.ErrorMessage.IncidentEventTypeRequired);

            var defaults = await _messageBus.InvokeAsync<List<VmsEventDefaultOutput>>(
                new VmsEventDefaultListInput { EventTypeId = eventTypeId });

            var result = defaults?.FirstOrDefault(u => u.EquipmentTypeId == equipmentTypeId);
            if (result == null)
                throw new InvalidOperationException(
                    string.Format(
                        IncidentAutomationMessageConst.ReasonFormat.NoEventDefault,
                        IncidentAutomationMessageConst.ErrorCode.NoEventDefault,
                        eventTypeId,
                        equipmentTypeId));

            return result;
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Bỏ riêng các thiết bị đang bị sự cố KHÁC giữ (TmsIncidentControl) rồi gửi phần còn lại bằng
        ///              MỘT lệnh như cũ. Trước đây chỉ cần một thiết bị bị giữ là cả bước bị bỏ, kể cả các thiết bị rảnh.
        ///              Không tách gửi lẻ từng thiết bị: pipeline VMS (ChangeVmsContentMulti) đã gom một gói tin, tự xử
        ///              lý kết quả từng thiết bị (UpdateVmsCurrentSuccess/Error) và không ném lỗi khi thiết bị gửi hỏng.
        ///              Bước chỉ lỗi (để FailurePolicy quyết định) khi mọi thiết bị đều bị giữ.
        /// Created date: 14/10/2026
        /// Update: ChuongLP - 16/10/2026 - "Bị giữ" theo luồng Append/Override: Append không chặn; Override chỉ chặn khi
        ///         sự cố khác còn hoạt động và ưu tiên CAO hơn (DEC-09). checkOccupied=false cho LCS (không có playlist).
        /// </summary>
        private async Task<string> SendToFreeEquipmentAsync(string incidentId, AddControlInput input, bool checkOccupied = true)
        {
            var equipmentList = input.EquipmentList
                .Where(u => !string.IsNullOrWhiteSpace(u.ID))
                .GroupBy(u => u.ID)
                .Select(g => g.First())
                .ToList();

            if (equipmentList.Count == 0)
                throw new InvalidOperationException(
                    $"{IncidentAutomationMessageConst.ErrorCode.NoEquipmentSelected}: "
                    + IncidentAutomationMessageConst.ErrorMessage.NoEquipmentSelected);

            var isAppend = string.Equals(input.PlayMode, VmsPlayModeConst.Append, StringComparison.OrdinalIgnoreCase);
            var occupiedIds = checkOccupied && !isAppend
                ? await GetOccupiedEquipmentIdsAsync(incidentId, equipmentList.Select(u => u.ID).ToList(), input.Priority)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var occupied = equipmentList.Where(u => occupiedIds.Contains(u.ID)).ToList();
            var free = equipmentList.Where(u => !occupiedIds.Contains(u.ID)).ToList();

            if (free.Count == 0)
                throw new InvalidOperationException(
                    string.Format(
                        IncidentAutomationMessageConst.ReasonFormat.IncidentControlOccupied,
                        IncidentAutomationMessageConst.ErrorCode.IncidentControlOccupied,
                        string.Join(IncidentAutomationMessageConst.ListSeparator, occupied.Select(EquipmentLabel))));

            input.EquipmentList = free;
            // Truyền actor tự động để handler không gọi UserManager.Account (HttpContext = null trong background).
            input.ExecuteBy = IncidentAutomationConst.SystemActor;
            await _messageBus.InvokeAsync(input);

            var remark = string.Format(
                IncidentAutomationMessageConst.Remark.DeviceControlAppliedToFormat,
                input.DeviceType?.ToUpperInvariant(),
                string.Join(IncidentAutomationMessageConst.ListSeparator, free.Select(EquipmentLabel)));
            if (occupied.Count > 0)
                remark += string.Format(
                    IncidentAutomationMessageConst.Remark.DeviceControlOccupiedSuffixFormat,
                    string.Join(IncidentAutomationMessageConst.ListSeparator, occupied.Select(EquipmentLabel)));

            // Remark của TmsWorkExecute giới hạn EntityConst.RemarkFieldLength.
            return remark.Length <= EntityConst.RemarkFieldLength ? remark : remark[..EntityConst.RemarkFieldLength];
        }

        /// <summary>
        /// Thiết bị đang bị sự cố KHÁC giữ (TmsIncidentControl là cổng sở hữu; vị trí Before/After do API gợi ý lo).
        /// Chỉ tính sự cố còn hoạt động và có ưu tiên CAO hơn sự cố mới (số Priority nhỏ hơn) - DEC-09.
        /// </summary>
        private async Task<HashSet<string>> GetOccupiedEquipmentIdsAsync(string incidentId, List<string> equipmentIds, int? currentPriority)
        {
            // ChuongLP - 19/10/2026: tách 2 bước. (1) Lấy sự cố KHÁC đang giữ từng thiết bị - truy vấn đã lọc sẵn dòng
            // không gắn sự cố và sự cố đã kết thúc/hủy/bỏ qua/xóa. (2) Áp quy tắc DEC-09 (hàm thuần).
            var occupancy = await _messageBus.InvokeAsync<List<TmsIncidentControlDataOutput>>(
                new GetIncidentControlOccupancyInput
                {
                    EquipmentIds = equipmentIds,
                    ExcludeIncidentId = incidentId
                });

            return IncidentControlOccupancyHelper.GetOverrideBlockedEquipmentIds(
                occupancy, currentPriority, IncidentAutomationMessageConst.DefaultPriority);
        }

        private static string EquipmentLabel(ControlEquipmentInput equipment)
            => string.IsNullOrWhiteSpace(equipment.Code) ? equipment.ID : equipment.Code;

        private async Task UpdateAutomaticWorkStateAsync(
            string workId,
            BaseEnums.TmsWorkExecuteStateEnum state,
            string? remark)
        {
            var updated = await _messageBus.InvokeAsync<bool>(new TmsWorkExecuteStateInput
            {
                Id = workId,
                State = ((int)state).ToString(),
                Remark = remark,
                Actor = IncidentAutomationConst.SystemActor
            });

            if (!updated)
                throw new InvalidOperationException(
                    $"{IncidentAutomationMessageConst.ErrorCode.WorkExecuteStateUpdateFailed}: "
                    + string.Format(
                        IncidentAutomationMessageConst.ErrorMessage.WorkExecuteStateUpdateFailedFormat,
                        workId,
                        state));
        }

        private static List<ControlEquipmentInput> ToControlEquipmentList(
            IEnumerable<TmsEquipmentDataOutput> equipmentList)
        {
            return equipmentList.Select(u => new ControlEquipmentInput
            {
                ID = u.ID,
                Code = u.Code ?? string.Empty,
                Name = u.Name ?? string.Empty
            }).ToList();
        }

        private static int GetWorkOrderNo(
            TmsWorkExecute work,
            IReadOnlyCollection<TmsTrafficPlanWorkDetails> planLinks)
        {
            return FindPlanLink(work, planLinks)?.OrderNo ?? int.MaxValue;
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Tìm đúng bước kịch bản đã sinh ra công việc: khớp (WorkDetailId, Step) và tổ chức thực hiện
        ///              (ExecuteOrg chép từ ExecuteOrgId khi sinh công việc); không khớp tổ chức thì rơi về cặp
        ///              (WorkDetailId, Step) như trước. Khớp riêng (WorkDetailId, Step) sai khi cùng công việc ở cùng
        ///              Step được giao cho nhiều tổ chức (vd một dòng thủ công, một dòng tự động).
        /// Created date: 14/10/2026
        /// </summary>
        private static TmsTrafficPlanWorkDetails? FindPlanLink(
            TmsWorkExecute work,
            IReadOnlyCollection<TmsTrafficPlanWorkDetails> planLinks)
        {
            var sameStep = planLinks
                .Where(u => u.WorkDetailId == work.WorkDetailId && u.Step == work.Step)
                .ToList();
            return sameStep.FirstOrDefault(u => u.ExecuteOrgId == work.ExecuteOrg) ?? sameStep.FirstOrDefault();
        }

        private static bool IsAutomaticPlanWork(TmsTrafficPlanWorkDetails? planLink)
            => planLink != null
                && NormalizeWorkValue(planLink.State) == IncidentAutomationMessageConst.AutomaticPlanState;

        private static string NormalizeWorkValue(string? value)
            => value?.Trim().ToLowerInvariant() ?? string.Empty;

        private static JObject? ParseJsonObject(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            try
            {
                return JObject.Parse(raw);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? ReadJsonString(JObject? source, params string[] names)
        {
            if (source == null || names == null || names.Length == 0)
                return null;

            foreach (var name in names)
            {
                var property = source.Properties().FirstOrDefault(u =>
                    string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
                if (property == null || property.Value.Type == JTokenType.Null)
                    continue;

                var value = property.Value.Type == JTokenType.String
                    ? property.Value.Value<string>()
                    : property.Value.ToString(Formatting.None);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static string ReplaceIncidentTokens(string? content, string? metadata)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            var metadataObject = ParseJsonObject(metadata);
            return System.Text.RegularExpressions.Regex.Replace(
                content,
                IncidentAutomationMessageConst.TokenPattern,
                match => ReadJsonString(metadataObject, match.Groups["key"].Value) ?? string.Empty);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Cập nhật chế độ xử lý ở cấp sự cố. Chỉ ghi đúng một cột để không đụng tới dữ liệu
        ///              nghiệp vụ khác của luồng thủ công đang chạy song song.
        ///              Không throw: đây là dấu vết phụ trợ, lỗi ở đây không được làm hỏng luồng automation.
        /// Created date: 24/07/2026
        /// </summary>
        private async Task SetProcessModeAsync(string? incidentId, string processMode)
        {
            if (string.IsNullOrWhiteSpace(incidentId))
                return;

            try
            {
                await _incidentRepo.AsUpdateable()
                    .SetColumns(u => u.ProcessMode == processMode)
                    .Where(u => u.ID == incidentId)
                    .ExecuteCommandAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    IncidentAutomationMessageConst.Log.ProcessModeUpdateFailed,
                    processMode,
                    incidentId);
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Phát realtime biến động của run. Luôn truyền Actor = SystemActor vì phần lớn lời gọi
        ///              đến từ background worker (không có HttpContext).
        ///              Bọc try/catch: hạ tầng messaging lỗi KHÔNG được làm hỏng luồng xử lý sự cố.
        /// Created date: 24/07/2026
        /// </summary>
        private void BroadcastRunChange(TmsIncidentAutomationRun run, string runStatus)
        {
            try
            {
                _broadcaster.NotifyAutomationRun(run.ID, run.IncidentId, runStatus, IncidentAutomationConst.SystemActor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, IncidentAutomationMessageConst.Log.BroadcastFailed, run.ID);
            }
        }

        #endregion Private Functions ====================
    }
}
