using Modules.VDS.Core.Dto;
using Modules.TMS.Infrastructure.Services.Incident.Snapshot;
using Modules.TMS.Infrastructure.Services.Incident.Classification;
using Modules.TMS.Infrastructure.Services.Incident.Messaging;
using Modules.TMS.Infrastructure.Services.Incident.Configuration;
using Microsoft.Extensions.Logging;
using Module.VideoWall.Core.Dto.Scene;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Dto.IncidentControl;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.Incident;
using Modules.TMS.Infrastructure.Services.IncidentAutomation;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using System.Linq.Expressions;
using static Shared.DTO.Enums.BaseEnums;
using Modules.TMS.Constants;

namespace Modules.TMS.Controllers.Incident.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Handler xử lý chuyển trạng thái sự cố (workflow): UpdateState / BatchUpdateState.
    /// Tách từ IncidentCommandHandler để phân tách rõ CRUD và nghiệp vụ chuyển trạng thái.
    /// Created date: 19/08/2024
    /// </summary>
    public class IncidentWorkflowCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsIncident> _incidentRepo;
        private BaseRepository<TmsWorkExecute> _workExecuteRepo;
        private BaseRepository<TmsWorkExecuteHistory> _workExecuteHistoryRepo;
        private BaseRepository<TmsTrafficPlanWorkDetails> _trafficPlanWorkDetailRepo;
        private BaseCacheService _cache;
        private IncidentBroadcaster _broadcaster;
        private TmsIncidentAutomationService _automationService;
        private IncidentSnapshotQueue _snapshotQueue;
        private IMessageBus _messageBus;
        private ILogger<IncidentWorkflowCommandHandler> _logger;

        private IncidentGroupResolver _groupResolver;
        private readonly EventCompletionPolicy _completionPolicy;
        private readonly BaseRepository<TmsIncidentAutomationRun> _automationRuns;
        private readonly VdsAutomationImagePolicy _imagePolicy;

        public IncidentWorkflowCommandHandler(BaseRepository<TmsIncident> incidentRepo, BaseRepository<TmsWorkExecute> workExecuteRepo,
            BaseRepository<TmsWorkExecuteHistory> workExecuteHistoryRepo, BaseRepository<TmsTrafficPlanWorkDetails> trafficPlanWorkDetailRepo,
            BaseCacheService cache, IncidentBroadcaster broadcaster, TmsIncidentAutomationService automationService,
            IncidentSnapshotQueue snapshotQueue,
            IMessageBus messageBus, ILogger<IncidentWorkflowCommandHandler> logger, IncidentGroupResolver groupResolver,
            EventCompletionPolicy completionPolicy, BaseRepository<TmsIncidentAutomationRun> automationRuns,
            VdsAutomationImagePolicy imagePolicy)
        {
            this._incidentRepo = incidentRepo;
            this._workExecuteRepo = workExecuteRepo;
            this._workExecuteHistoryRepo = workExecuteHistoryRepo;
            this._trafficPlanWorkDetailRepo = trafficPlanWorkDetailRepo;
            _cache = cache;
            _broadcaster = broadcaster;
            _automationService = automationService;
            _snapshotQueue = snapshotQueue;
            _messageBus = messageBus;
            _logger = logger;
            _groupResolver = groupResolver;
            _completionPolicy = completionPolicy;
            _automationRuns = automationRuns;
            _imagePolicy = imagePolicy;
        }

        /// <summary>Only a claimed run can provide trusted image context; never exposed as an HTTP DTO.</summary>
        public async Task<bool> HandleAsync(AutomationIncidentStateChange command, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(command.TenantId) || command.State is not (2 or 3)) return false;
            var run = await _automationRuns.Context.Queryable<TmsIncidentAutomationRun>()
                .Where(x => x.ID == command.RunId && x.TenantId == command.TenantId
                    && x.IncidentId == command.IncidentId && x.IsDelete == null).FirstAsync(ct);
            var incident = await _incidentRepo.Context.Queryable<TmsIncident>()
                .Where(x => x.ID == command.IncidentId && x.TenantId == command.TenantId && x.IsDelete == null).FirstAsync(ct);
            if (run is null || incident is null || run.EventTypeId != incident.EventTypeId) return false;
            if (command.State == (int)TmsIncidentStateEnum.Verified)
            {
                if (run.Status != IncidentAutomationConst.RunStatus.Starting
                    || run.TriggerState != IncidentAutomationConst.TriggerState.Registration
                    || incident.State != ((int)TmsIncidentStateEnum.Registration).ToString()) return false;
            }
            else if (run.Status != IncidentAutomationConst.RunStatus.Running
                || incident.State != ((int)TmsIncidentStateEnum.Verified).ToString()) return false;
            await _groupResolver.PopulateGroupsAsync(new[] { incident });
            if (!IncidentEventGroupConst.AllowsProcessing(incident.EventGroup, incident.State)) return false;
            var changed = await UpdateIncidentState(new TmsIncidentStateInput
            {
                Id = incident.ID, State = command.State.ToString(), TrafficPlanId = run.TrafficPlanId,
                Actor = IncidentAutomationConst.SystemActor
            }, incident, command.State, IncidentAutomationConst.SystemActor, automationRun: run);
            if (!changed) return false; // A rolled-back processing transition must never create picture 3.
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncident);
            VdsAutomationImageDecisionOutput? choice = null;
            try
            {
                var persisted = await _incidentRepo.Context.Queryable<TmsIncident>()
                    .Where(x => x.ID == incident.ID && x.TenantId == command.TenantId && x.IsDelete == null).FirstAsync(ct);
                if (persisted is not null) choice = await _imagePolicy.SelectAsync(persisted, run, command.State, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[VdsAutomationImage] Image policy failed after successful transition for {IncidentId}; use CCTV/RTSP.", incident.ID);
            }
            _snapshotQueue.Enqueue(incident.ID, command.State, choice);
            return true;
        }

        /// <summary>Internal expiry uses the shared transition and its audit/snapshot/broadcast.</summary>
        public async Task<bool> HandleAsync(CompleteUnclaimedEvent command)
        {
            var incident = await _incidentRepo.GetFirstAsync(x => x.ID == command.IncidentId
                && x.TenantId == command.TenantId && x.IsDelete == null);
            if (incident == null) return false;
            var config = await _completionPolicy.ResolveAsync(incident);
            if (config == null || !EventCompletionPolicy.IsDue(incident.Metadata, DateTimeOffset.UtcNow, config.ID)) return false;
            var state = (int)TmsIncidentStateEnum.Finished;
            var changed = await UpdateIncidentState(new TmsIncidentStateInput { Id = incident.ID, State = state.ToString(),
                Actor = IncidentAutomationConst.SystemActor }, incident, state, IncidentAutomationConst.SystemActor, config);
            if (changed)
            {
                _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncident);
                _snapshotQueue.Enqueue(incident.ID, state);
            }
            return changed;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật state và xử lý chi tiết (nếu có) cho một record
        /// Created date: 19/08/2024
        /// </summary>
        public async Task<bool> HandleAsync(TmsIncidentStateInput command)
        {
            // TODO: Check authorization

            // Kiểm tra giá trị state hợp lệ
            var incidentState = this.ConvertIncidentState(command.State);
            if (incidentState == -1)
                return false;

            var incident = await this._incidentRepo.GetByIdAsync(command.Id);
            if (incident == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (incident.IsDelete != null || incident.State is "0" or "4" or "5") return false;

            // KienLT - 14/10/2026: không cho lùi trạng thái (2 → 1, 3 → 2/1). Trạng thái kết thúc 0/4/5 đã bị chặn ở trên.
            var currentState = this.ConvertIncidentState(incident.State);
            if (IsBackwardTransition(currentState, incidentState))
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.StateBackwardNotAllowed);

            // KienLT - 14/10/2026: chặn lặp Đang xử lý → Đang xử lý (vd hai người cùng chọn kịch bản, bấm lại):
            // lần sau sẽ đổi kịch bản và sinh thêm một bộ công việc chồng lên bộ đang làm.
            if (currentState == (int)TmsIncidentStateEnum.InProgress && incidentState == (int)TmsIncidentStateEnum.InProgress)
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.AlreadyInProgress);

            // Tiến hành cập nhật state và xử lý chi tiết cho incident (thông báo realtime nằm trong hàm)
            await this._groupResolver.PopulateGroupsAsync(new[] { incident });
            if (!IncidentEventGroupConst.AllowsProcessing(incident.EventGroup, incident.State)) throw Oops.Oh(BaseMsg.TmsIncident.Exception.EventHasNoWorkflow);
            var result = await this.UpdateIncidentState(command, incident, incidentState, command.Actor);

            // Xóa cache để các truy vấn đọc lấy dữ liệu mới
            if (result)
            {
                _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncident);
                _snapshotQueue.Enqueue(incident.ID, incidentState);
            }

            return result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái state cho nhiều record
        /// Note: Chỉ cập nhật hàng loạt nếu state truyền vào là Verified | Skipped | Canceled
        /// Note: KHÔNG cập nhật hàng loạt nếu state truyền vào là InProgress | Finished
        /// Created date: 19/08/2024
        /// </summary>
        public async Task<bool> HandleAsync(List<TmsIncidentStateInput> command)
        {
            // TODO: Check authorization

            if (command.Count == 0 || command.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.State)))
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // State truyền vào chỉ hợp lệ nếu == Verified | Skipped | Canceled
            command.ForEach(item =>
            {
                var newState = this.ConvertIncidentState(item.State);
                if (newState != (int)TmsIncidentStateEnum.Verified && newState != (int)TmsIncidentStateEnum.Skipped && newState != (int)TmsIncidentStateEnum.Canceled)
                    throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);
            });

            // Map id -> state mới (đồng thời loại trùng id)
            var stateById = new Dictionary<string, string>();
            command.ForEach(item =>
            {
                if (!stateById.ContainsKey(item.Id))
                    stateById.Add(item.Id, item.State);
            });
            if (stateById.Count != command.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // Lấy các incident tương ứng và kiểm tra số lượng khớp
            var incidentList = await this._incidentRepo.GetListAsync(ti => stateById.Keys.Contains(ti.ID));
            await this._groupResolver.PopulateGroupsAsync(incidentList);
            if (incidentList.Any(x => !IncidentEventGroupConst.AllowsProcessing(x.EventGroup, x.State))) throw Oops.Oh(BaseMsg.TmsIncident.Exception.EventStateNotChangeable);
            if (incidentList.Count != stateById.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // Không cho update các incident hiện đang ở trạng thái kết thúc (Finished | Skipped | Canceled)
            incidentList.ForEach(incident =>
            {
                var currentState = this.ConvertIncidentState(incident.State);
                if (currentState == (int)TmsIncidentStateEnum.Finished || currentState == (int)TmsIncidentStateEnum.Skipped || currentState == (int)TmsIncidentStateEnum.Canceled)
                    throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

                // KienLT - 14/10/2026: kiểm lùi trạng thái cho CẢ lô trước khi ghi, tránh lô bị áp dở dang.
                if (IsBackwardTransition(currentState, this.ConvertIncidentState(stateById[incident.ID])))
                    throw Oops.Oh(BaseMsg.TmsIncident.Exception.StateBackwardNotAllowed);
            });

            // Reuse the same transition for verification, cancellation and side effects.
            // In particular, batch verification must schedule the verify trigger just like a single row.
            var changed = false;
            foreach (var item in command)
                changed = await HandleAsync(item) || changed;
            return changed;
        }

        #region Private Functions ====================
        /// <summary>
        /// Author: KienLT
        /// Description: Chuyển LÙI trong luồng xử lý: đích là Đăng ký/Đã xác minh/Đang xử lý nhưng nhỏ hơn trạng thái
        ///              hiện tại (2 → 1, 3 → 2, 3 → 1). Lùi trạng thái làm sự cố quay lại hàng chờ, có thể lập thêm
        ///              lần tự động và sinh thêm một bộ công việc khi chọn kịch bản lại.
        /// Created date: 14/10/2026
        /// </summary>
        private static bool IsBackwardTransition(int currentState, int targetState)
            => currentState is (int)TmsIncidentStateEnum.Registration or (int)TmsIncidentStateEnum.Verified
                    or (int)TmsIncidentStateEnum.InProgress
               && targetState is (int)TmsIncidentStateEnum.Registration or (int)TmsIncidentStateEnum.Verified
                    or (int)TmsIncidentStateEnum.InProgress
               && targetState < currentState;

        /// <summary>
        /// Author: ThangNN
        /// Description: Convert + validate giá trị state (trả -1 nếu không hợp lệ)
        /// </summary>
        private int ConvertIncidentState(string? state)
        {
            if (string.IsNullOrWhiteSpace(state) || !int.TryParse(state, out var incidentState))
                return -1;

            if (
                incidentState != (int)TmsIncidentStateEnum.Skipped
                && incidentState != (int)TmsIncidentStateEnum.Registration
                && incidentState != (int)TmsIncidentStateEnum.Verified
                && incidentState != (int)TmsIncidentStateEnum.InProgress
                && incidentState != (int)TmsIncidentStateEnum.Finished
                && incidentState != (int)TmsIncidentStateEnum.Canceled
            )
                return -1;

            return incidentState;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: NGUỒN GÁN TẬP TRUNG — quy định mỗi state sẽ gán những cột nào (audit + State).
        /// Cần điều chỉnh field theo trạng thái thì chỉ sửa tại đây (dùng chung cho cả update đơn và hàng loạt).
        /// </summary>
        private Expression<Func<TmsIncident, TmsIncident>> BuildStateColumns(int state, string? actor = null)
        {
            // Author: SonTH - 24/07/2026
            // Cho phép override actor (dùng khi luồng tự động dispatch ngoài HTTP context, không có App.User).
            var account = actor ?? App.User?.FindFirst(ClaimConst.Account)?.Value;
            var now = DateTime.Now;
            var stateStr = state.ToString();

            return state switch
            {
                (int)TmsIncidentStateEnum.Verified =>
                    u => new TmsIncident { State = stateStr, ValidatedBy = account, ValidatedDate = now },

                (int)TmsIncidentStateEnum.InProgress =>
                    u => new TmsIncident { State = stateStr, ApprovedBy = account, ApprovedDate = now },

                (int)TmsIncidentStateEnum.Finished
                or (int)TmsIncidentStateEnum.Skipped
                or (int)TmsIncidentStateEnum.Canceled =>
                    u => new TmsIncident { State = stateStr, FinishedBy = account, FinishedDate = now },

                _ => u => new TmsIncident { State = stateStr }
            };
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật state và xử lý chi tiết cho incident (sinh work-execute / archive history nếu có)
        /// </summary>
        private async Task<bool> UpdateIncidentState(TmsIncidentStateInput command, TmsIncident incident, int state, string? actor = null,
            TmsIncidentAutomationConfig? completionConfig = null, TmsIncidentAutomationRun? automationRun = null)
        {
            var unclaimedExpiry = completionConfig != null;
            // InProgress bắt buộc có kịch bản
            if (state == (int)TmsIncidentStateEnum.InProgress)
            {
                if (string.IsNullOrWhiteSpace(command.TrafficPlanId))
                    return false;

                incident.TrafficPlanId = command.TrafficPlanId; // cần cho việc sinh work-execute bên dưới
            }
            else if (state != (int)TmsIncidentStateEnum.Verified
                && state != (int)TmsIncidentStateEnum.Finished
                && state != (int)TmsIncidentStateEnum.Skipped
                && state != (int)TmsIncidentStateEnum.Canceled)
            {
                return false; // Các trạng thái khác (vd Registration) không xử lý qua UpdateState
            }

            // Cập nhật state + audit theo điều kiện (mapping tập trung tại BuildStateColumns)
            var updateable = this._incidentRepo.AsUpdateable().SetColumns(this.BuildStateColumns(state, actor));
            if (state == (int)TmsIncidentStateEnum.InProgress)
            {
                var trafficPlanId = command.TrafficPlanId;
                updateable = updateable.SetColumns(u => new TmsIncident { TrafficPlanId = trafficPlanId });
            }

            // Expiry must lose to verification/scheduling/metadata changes in the same UPDATE, not a prior SELECT.
            if (unclaimedExpiry)
                updateable = GuardUnclaimedCompletion(updateable, incident, completionConfig!);

            if (automationRun is not null)
            {
                var runId = automationRun.ID;
                var runStatus = automationRun.Status;
                updateable = updateable.Where(u => SqlFunc.Subqueryable<TmsIncidentAutomationRun>()
                    .Where(r => r.ID == runId && r.IncidentId == u.ID && r.TenantId == u.TenantId
                        && r.EventTypeId == u.EventTypeId && r.IsDelete == null && r.Status == runStatus).Any());
            }

            int updatedCount = await updateable
                .Where(u => u.ID == incident.ID && u.TenantId == incident.TenantId && u.IsDelete == null && u.State == incident.State)
                .EnableDiffLogEvent()
                .ExecuteCommandAsync();
            if (updatedCount == 0)
                return false;

            // Broadcast the state persisted by the conditional update.
            incident.State = state.ToString();

            // Finished => chuyển work-execute sang TmsWorkExecuteHistory (nếu có) và luôn thông báo
            if (state == (int)TmsIncidentStateEnum.Finished || state == (int)TmsIncidentStateEnum.Canceled)
            {
                var workExecuteHistoryList = await this._workExecuteRepo.AsQueryable()
                    .Where(we => we.IncidentId == incident.ID)
                    .Select(we => new TmsWorkExecuteHistory() { }, true)
                    .ToListAsync();

                if (workExecuteHistoryList.Count > 0
                    && await this._workExecuteHistoryRepo.InsertRangeAsync(workExecuteHistoryList))
                {
                    await this._workExecuteRepo.DeleteAsync(we => we.IncidentId == incident.ID);
                }

                // Author: SonTH - 24/07/2026 - Nếu Finish được gọi thủ công trong khi có active run
                // (ví dụ đang chờ auto-finish), hủy run đó để worker không tự kết thúc lần nữa.
                if (actor != IncidentAutomationConst.SystemActor)
                    await this.SafeCancelActiveRunsAsync(incident.ID, "ManualFinish");

                // A: đóng thủ công -> reset VMS (automation tự lo phần của nó nên chỉ chạy khi actor != system)
                if (actor != IncidentAutomationConst.SystemActor)
                    await this.SafeResetIncidentVmsAsync(incident.ID);

                // An unclaimed event never applied a scene; closing it must not reset another incident's wall.
                if (!unclaimedExpiry)
                    await this.ActivateVideoWallSceneAsync(
                        incident,
                        IncidentAutomationMessageConst.VideoWallSceneCode.Normal);

                await _broadcaster.NotifyAsync(incident, state, actor);
                return true;
            }

            // InProgress => sinh danh sách work-execute theo TrafficPlanId
            if (state == (int)TmsIncidentStateEnum.InProgress)
            {
                // KienLT - 16/10/2026: State=3 đã ghi ở trên. Trước đây chỉ rollback khi insert trả false; nếu đọc/insert
                // công việc NÉM lỗi (vd mất kết nối) thì sự cố nằm ở Đang xử lý mà không có công việc nào - không danh sách
                // nào hiện nó và cũng không chọn lại kịch bản được (chặn lặp 3→3). Nay lỗi cũng đi đúng nhánh rollback.
                var insertSuccess = false;
                try
                {
                    var workExecuteList = await this._trafficPlanWorkDetailRepo.AsQueryable()
                        .InnerJoin<TmsWorkDetail>((tblTpwd, tblWd) => tblTpwd.WorkDetailId == tblWd.ID)
                        .Where((tblTpwd, tblWd) =>
                            tblTpwd.IsDelete == null
                            && tblWd.IsDelete == null
                            && tblWd.Status == BaseEnums.StatusEnum.Enable
                        )
                        .Where((tblTpwd, tblWd) => tblTpwd.TrafficPlanId == incident.TrafficPlanId)
                        .Select((tblTpwd, tblWd) => new TmsWorkExecute()
                        {
                            IncidentId = incident.ID,
                            TrafficPlanId = incident.TrafficPlanId,
                            WorkDetailId = tblTpwd.WorkDetailId,
                            Step = tblTpwd.Step,
                            ExecuteOrg = tblTpwd.ExecuteOrgId,
                            WorkName = tblWd.Name,
                            Description = tblWd.Remark,
                            WorkMode = tblWd.WorkMode,
                            ControlType = tblWd.ControlType,
                            State = ((int)TmsWorkExecuteStateEnum.Waiting).ToString()
                        })
                        .ToListAsync();

                    if ((workExecuteList?.Count() ?? 0) > 0)
                        insertSuccess = await this._workExecuteRepo.InsertRangeAsync(workExecuteList);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "[IncidentWorkflow] Sinh công việc cho sự cố {IncidentId} lỗi; quay về Đã xác minh.", incident.ID);
                    insertSuccess = false;
                }

                // Rollback về Verified nếu sinh work-execute thất bại
                if (!insertSuccess)
                {
                    incident.ApprovedBy = null;
                    incident.ApprovedDate = null;
                    incident.TrafficPlanId = null;
                    incident.State = ((int)TmsIncidentStateEnum.Verified).ToString();

                    await this._incidentRepo.UpdateWithDiffLogAsync(incident);
                    return false;
                }

                // Author: SonTH - 24/07/2026 - InProgress được thực hiện thủ công (người dùng tự chọn/chạy
                // plan) trong khi có run automation đang WarningPending: hủy run cũ để tránh chạy trùng.
                if (actor != IncidentAutomationConst.SystemActor)
                    await this.SafeCancelActiveRunsAsync(incident.ID, "ManualInProgress");

                await _broadcaster.NotifyAsync(incident, state, actor);
                return true;
            }

            // Author: SonTH - 24/07/2026
            // Verified: thử schedule automation nếu EventType bật trigger Verified (no-op nếu chưa cấu hình).
            // Skipped | Canceled: hủy active run nếu người dùng tự thao tác (tránh automation chạy đè).
            if (state == (int)TmsIncidentStateEnum.Verified && actor != IncidentAutomationConst.SystemActor)
            {
                // Tự động hóa là chức năng phụ trợ: lỗi lập lịch không được làm hỏng việc xác minh sự cố.
                try
                {
                    await this._automationService.TryScheduleAsync(incident.ID, IncidentAutomationConst.TriggerState.Verified);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "[IncidentWorkflowCommandHandler] Lập lịch tự động cho sự cố {IncidentId} thất bại.", incident.ID);
                }
            }
            else if ((state == (int)TmsIncidentStateEnum.Skipped || state == (int)TmsIncidentStateEnum.Canceled)
                && actor != IncidentAutomationConst.SystemActor)
            {
                await this.SafeCancelActiveRunsAsync(incident.ID, "ManualCancelOrSkip");
                await this.SafeResetIncidentVmsAsync(incident.ID); // A: reset khi bỏ qua/hủy thủ công
            }

            if (state == (int)TmsIncidentStateEnum.Verified)
            {
                await this.ActivateVideoWallSceneAsync(
                    incident,
                    IncidentAutomationMessageConst.VideoWallSceneCode.Incident,
                    includeEventType: true);
            }

            // Verified | Skipped | Canceled
            await _broadcaster.NotifyAsync(incident, state, actor);
            return true;
        }

        /// <summary>
        /// Gọi pipeline Video Wall sau khi state incident đã cập nhật thành công.
        /// Lỗi Video Wall chỉ ghi log, không rollback state incident đã ghi DB.
        /// </summary>
        internal static IUpdateable<TmsIncident> GuardUnclaimedCompletion(IUpdateable<TmsIncident> updateable,
            TmsIncident incident, TmsIncidentAutomationConfig config) =>
            updateable.Where(u => u.ID == incident.ID && u.TenantId == incident.TenantId
                && u.IsDelete == null && u.State == "1" && u.Metadata == incident.Metadata
                && u.ValidatedDate == null && u.ApprovedDate == null && (u.TrafficPlanId == null || u.TrafficPlanId == "")
                && !SqlFunc.Subqueryable<TmsIncidentAutomationRun>().Where(r => r.IncidentId == u.ID && r.TenantId == u.TenantId).Any()
                && SqlFunc.Subqueryable<TmsIncidentAutomationConfig>().Where(c => c.ID == config.ID
                    && c.TenantId == u.TenantId && c.EventTypeId == u.EventTypeId && c.IsDelete == null
                    && c.ConfigVersion == config.ConfigVersion
                    && c.IsUnclaimedAutoFinishEnabled && !c.IsAutoExecuteEnabled && c.Status == StatusEnum.Enable).Any()
                && !SqlFunc.Subqueryable<TmsWorkExecute>().Where(w => w.IncidentId == u.ID && w.TenantId == u.TenantId).Any());

        private async Task ActivateVideoWallSceneAsync(
            TmsIncident incident,
            string sceneCode,
            bool includeEventType = false)
        {
            try
            {
                // Handler trả VwActiveSceneOutput: phải gọi bản generic, nếu không Wolverine coi giá trị trả về là
                // cascading message và ném "not allowed with Wolverine is bootstrapped in MediatorOnly mode".
                await this._messageBus.InvokeAsync<VwActiveSceneOutput>(new VwActiveSceneInput
                {
                    EventTypeId = includeEventType ? incident.EventTypeId : null
                    , Code =  includeEventType ? null : sceneCode
                });
            }
            catch (Exception ex)
            {
                // Không rollback sự cố, nhưng phải để lại dấu vết để biết tường không đổi hình.
                this._logger.LogError(
                    ex,
                    IncidentAutomationMessageConst.Log.VideoWallSceneActivationFailed,
                    includeEventType ? $"EventType:{incident.EventTypeId}" : sceneCode,
                    incident.ID);
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Hủy các automation run đang hoạt động của sự cố, không để lỗi phụ trợ này làm thất bại
        ///              thao tác chuyển trạng thái chính của người dùng.
        /// Created date: 24/07/2026
        /// </summary>
        private async Task SafeCancelActiveRunsAsync(string incidentId, string reason)
        {
            try
            {
                await this._automationService.CancelActiveRunsByIncidentAsync(incidentId, reason);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "[IncidentWorkflowCommandHandler] Hủy automation run của sự cố {IncidentId} thất bại.", incidentId);
            }
        }

        /// <summary>
        /// Đóng sự cố thủ công -> yêu cầu VMS reset thiết bị về ảnh mặc định + dọn IncidentControl.
        /// Best-effort: lỗi VMS chỉ ghi log, không làm hỏng thao tác đóng sự cố.
        /// Author: ChuongLP
        /// Date: 04/09/2026
        /// </summary>
        private async Task SafeResetIncidentVmsAsync(string incidentId)
        {
            try
            {
                await this._messageBus.InvokeAsync(new ResetIncidentToDefaultInput { IncidentId = incidentId });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "[IncidentWorkflowCommandHandler] Reset VMS về mặc định cho sự cố {IncidentId} thất bại.", incidentId);
            }
        }

        #endregion Private Functions ====================
    }
}
