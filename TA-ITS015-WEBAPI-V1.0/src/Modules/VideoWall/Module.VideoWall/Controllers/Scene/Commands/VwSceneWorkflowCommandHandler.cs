using Mapster;
using Microsoft.Extensions.Logging;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Dto.Scene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Core.Helper;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Module.VideoWall.Infrastructure.Services.Layout;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Services;
using Shared.DTO.Enums;
using Wolverine;

namespace Module.VideoWall.Controllers.Scene.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý nghiệp vụ kích hoạt kịch bản hiển thị (VwScene).
    /// Created date: 2026-08-03
    /// </summary>
    public class VwSceneWorkflowCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwScene> _vwSceneRep;
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly BaseRepository<VwEventRule> _vwEventRuleRep;
        private readonly IVwPublisher _publisher;
        private readonly IVwEventTriggerLogService _eventTriggerLogService;
        private readonly IVwNatsPublisher _natsPublisher;
        private readonly BaseCacheService _cache;
        private readonly IOptions<VwDeviceOptions> _options;
        private readonly ILogger<VwSceneWorkflowCommandHandler> _logger;
        private readonly VwWallLayoutService _layout;

        public VwSceneWorkflowCommandHandler(
            BaseRepository<VwScene> vwSceneRep,
            BaseRepository<VwWindowScene> vwWindowSceneRep,
            BaseRepository<VwController> vwControllerRep,
            BaseRepository<VwEventRule> vwEventRuleRep,
            VwPermissionService permission,
            IVwPublisher publisher,
            IVwEventTriggerLogService eventTriggerLogService,
            IVwNatsPublisher natsPublisher,
            BaseCacheService cache,
            IOptions<VwDeviceOptions> options,
            ILogger<VwSceneWorkflowCommandHandler> logger,
            VwWallLayoutService layout)
        {
            _logger = logger;
            _layout = layout;
            _natsPublisher = natsPublisher;
            _cache = cache;
            _options = options;
            _vwSceneRep = vwSceneRep;
            _vwControllerRep = vwControllerRep;
            _vwEventRuleRep = vwEventRuleRep;
            _publisher = publisher;
            _eventTriggerLogService = eventTriggerLogService;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kích hoạt 1 kịch bản hiển thị (Fire-and-forget, FE theo dõi realtime qua NATS Data).
        /// Created date: 2026-08-03
        /// </summary>
        public async Task<VwActiveSceneOutput> HandleAsync(VwActiveSceneInput command, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var actor = App.User == null ? "system" : App.User.GetUsername();

            VwScene scene;
            VwEventRule? rule = null;

            if (!string.IsNullOrWhiteSpace(command.Code))
            {
                scene = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.Code == command.Code)
                    ?? throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.SceneId]);
            }
            else if (!string.IsNullOrWhiteSpace(command.EventTypeId))
            {
                var candidateRules = await _vwEventRuleRep.AsQueryable()
                    .Where(u => u.IsDelete == null
                             && u.Status == BaseEnums.StatusEnum.Enable
                             && u.EventTypeId == command.EventTypeId)
                    .ToListAsync();

                rule = candidateRules
                    .OrderBy(u => GetPriorityRank(u.Priority))
                    .ThenBy(u => u.CreateTime)
                    .FirstOrDefault()
                    ?? throw Oops.Oh(BaseMsg.Vw.Exception.EventRuleNotFound, command.EventTypeId!);

                if (string.IsNullOrWhiteSpace(rule.TargetSceneId))
                    throw Oops.Oh(BaseMsg.Vw.Exception.RuleMissingTargetScene, rule.Code ?? rule.ID);

                scene = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == rule.TargetSceneId)
                    ?? throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.TargetSceneId]);
            }
            else
            {
                throw Oops.Oh(BaseMsg.Vw.Validation.SceneIdOrEventTypeIdRequired);
            }
            if (scene.Status != BaseEnums.StatusEnum.Enable)
            {
                await WriteTriggerLogAsync(rule, command.EventTypeId, scene.ID, now, BaseEnums.SuccessEnums.Fail);
                throw Oops.Oh(BaseMsg.Vw.Exception.SceneDisabled, scene.Name ?? scene.Code ?? scene.ID);
            }

            // Kịch bản dựng trên phiên bản bố cục khác ⇒ toạ độ cửa sổ không khớp tường thật, không đẩy xuống thiết bị.
            // Áp dụng cho cả bấm tay, lịch và quy tắc sự kiện (lịch / quy tắc ghi log Fail qua đây).
            try
            {
                await _layout.EnsureSceneOnCurrentLayoutAsync(scene);
            }
            catch
            {
                await WriteTriggerLogAsync(rule, command.EventTypeId, scene.ID, now, BaseEnums.SuccessEnums.Fail);
                throw;
            }

            // ── 3. Lấy danh sách bộ điều khiển — GIỮ: cần cho targetControllerIds (lệnh Worker + ghi ActiveSceneId) ───
            var controllers = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .Select(u => new VwController
                {
                    ID = u.ID,
                    ActiveSceneId = u.ActiveSceneId,
                    Name = u.Name,
                    IP = u.IP,
                    Port = u.Port,
                    Account = u.Account,
                    PassWord = u.PassWord,
                    ParentControllerId = u.ParentControllerId
                })
                .ToListAsync();

            // TẠM COMMENT (06/10/2026 — chưa có thiết bị): kiểm tra sớm "đã cấu hình bộ điều khiển trung tâm chưa".
            // Chỉ chạy khi kịch bản có OutputId (đã dựng xuống thiết bị) nên hiện không bao giờ chạy tới.
            // Bỏ đi cũng không mất kiểm tra: Worker (VwISAPIDeviceService.ActivateScene) vẫn tự báo lỗi
            // "Chưa cấu hình bộ điều khiển trung tâm." — chỉ là BE không chặn sớm và không ghi log Fail.
            // Khi có thiết bị: mở lại đoạn dưới.
            //if (!string.IsNullOrWhiteSpace(scene.OutputId))
            //{
            //    // Bộ trung tâm = bộ được bộ khác trỏ tới làm cha (xem VwControllerTopology).
            //    // Kịch bản áp cho cả tường (không gắn bộ điều khiển riêng) ⇒ luôn đi qua bộ trung tâm;
            //    // chưa cấu hình phân cấp (chế độ độc lập) thì lấy bộ đầu tiên.
            //    var centerIds = VwControllerTopology.GetCenterIds(controllers);
            //    var center = controllers.FirstOrDefault(u => centerIds.Contains(u.ID))
            //              ?? controllers.FirstOrDefault();
            //
            //    if (center == null)
            //    {
            //        await WriteTriggerLogAsync(rule, command.EventTypeId, scene.ID, now, BaseEnums.SuccessEnums.Fail);
            //        throw Oops.Oh(BaseMsg.Vw.Exception.CentralControllerNotConfigured);
            //    }
            //}

            // Kịch bản của cả tường ⇒ gửi toàn bộ danh sách, Worker tự chọn bộ trung tâm (cascade).
            var targetControllerIds = controllers.Select(u => u.ID).ToList();

            // ── 4. Fire-and-forget: publish lệnh xuống Worker qua NATS (không chờ ACK) ──
            //       Luôn gửi (10/11/2026): Worker tự dựng kịch bản từ DB khi chưa có / lệch bản lưu trên thiết bị,
            //       tự cấp ô kịch bản (OutputId) nếu chưa có — không còn bắt buộc khai OutputId trước.
            var payload = new VwActivateScenePayload
            {
                SceneId = scene.ID,
                OutputId = scene.OutputId,
                TargetControllerIds = targetControllerIds
            };

            await _publisher.PublishCommandAsync(
                VwCommandActions.ActivateScene,
                payload,
                controllerId: null,
                sceneId: scene.ID,
                ct: ct);

            // ── 4b. Luồng BE tự áp kịch bản (như trước 29/09/2026) — phục vụ test khi CHƯA có thiết bị ──
            //        Ghi kịch bản đang phát vào DB + bắn NATS cho FE ngay, không chờ Worker.
            //        Tắt bằng VideoWall:ApplySceneOnServer = false khi đã có thiết bị (Worker sẽ ghi sau khi
            //        thiết bị chuyển thành công — xem ITS.VideoWall VwCommandConsumer.HandleActivateSceneAsync).
            if (_options.Value.ApplySceneOnServer)
            {
                try
                {
                    await ApplyActiveSceneOnServerAsync(scene, targetControllerIds, now);
                }
                catch
                {
                    await WriteTriggerLogAsync(rule, command.EventTypeId, scene.ID, now, BaseEnums.SuccessEnums.Fail);
                    throw;
                }

                await BroadcastSceneActivatedAsync(scene, targetControllerIds, ct);
            }

            // ── 5. Ghi log yêu cầu kích hoạt vào VwEventTriggerLog ─────────────
            var triggerLogId = await WriteTriggerLogAsync(
                rule, command.EventTypeId, scene.ID, now, BaseEnums.SuccessEnums.Success);

            // ── 6. Trả về kết quả ngay (FE tự theo dõi cập nhật realtime qua NATS Data) ──
            var output = scene.Adapt<VwActiveSceneOutput>();
            output.RuleId = rule?.ID;
            output.EventTypeId = rule?.EventTypeId ?? command.EventTypeId;
            output.TriggerLogId = triggerLogId;
            output.TriggeredAt = now;

            return output;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi kịch bản đang phát vào DB ngay tại BE (khôi phục luồng trước 29/09/2026):
        ///              - VwController.ActiveSceneId / ActiveSceneAt của các bộ điều khiển đích ⇒ GET /vwscene/active trả đúng.
        ///              - Cờ VwScene.ActiveScene: bật cho kịch bản này, hạ cờ kịch bản không còn vùng nào trỏ tới.
        ///              Cùng logic với Worker (VwCommandConsumer.HandleActivateSceneAsync).
        /// Created date: 06/10/2026
        /// </summary>
        private async Task ApplyActiveSceneOnServerAsync(VwScene scene, List<string> targetControllerIds, DateTime now)
        {
            // Kịch bản còn được ít nhất một vùng trỏ tới SAU khi ghi ⇒ giữ cờ Activate
            var allControllers = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .Select(u => new VwController { ID = u.ID, ActiveSceneId = u.ActiveSceneId })
                .ToListAsync();

            var stillActiveSceneIds = allControllers
                .Select(u => targetControllerIds.Contains(u.ID) ? scene.ID : u.ActiveSceneId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Append(scene.ID)
                .Distinct()
                .ToList();

            try
            {
                _vwSceneRep.Context.Ado.BeginTran();

                if (targetControllerIds.Count > 0)
                {
                    await _vwControllerRep.AsUpdateable()
                        .SetColumns(u => new VwController { ActiveSceneId = scene.ID, ActiveSceneAt = now })
                        .Where(u => u.IsDelete == null && targetControllerIds.Contains(u.ID))
                        .ExecuteCommandAsync();
                }

                await _vwSceneRep.AsUpdateable()
                    .SetColumns(u => new VwScene { ActiveScene = BaseEnums.ActiveScene.DeActivate })
                    .Where(u => u.IsDelete == null
                             && u.ActiveScene == BaseEnums.ActiveScene.Activate
                             && !stillActiveSceneIds.Contains(u.ID))
                    .ExecuteCommandAsync();

                await _vwSceneRep.AsUpdateable()
                    .SetColumns(u => new VwScene { ActiveScene = BaseEnums.ActiveScene.Activate })
                    .Where(u => u.ID == scene.ID)
                    .ExecuteCommandAsync();

                _vwSceneRep.Context.Ado.CommitTran();
            }
            catch
            {
                // Không rollback thì transaction treo lại trên connection và làm hỏng các lệnh sau
                try { _vwSceneRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            scene.ActiveScene = BaseEnums.ActiveScene.Activate;
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScene);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Bắn NATS "ta.its.data.videowall.scene" cho FE (màn giám sát nghe qua useTransporterVideoWallScene).
        ///              Giữ đúng hình payload cũ { ActiveValue: { SceneId, SceneCode }, ControllerIds } mà FE đang bóc.
        ///              Lỗi publish chỉ ghi log — DB đã đúng, FE vẫn đồng bộ lại khi tải trang / quay lại tab.
        /// Created date: 06/10/2026
        /// </summary>
        private async Task BroadcastSceneActivatedAsync(VwScene scene, List<string> controllerIds, CancellationToken ct)
        {
            try
            {
                await _natsPublisher.PublishAsync(VwSubjects.DataScene, new
                {
                    ActiveValue = new { SceneId = scene.ID, SceneCode = scene.Code, SceneName = scene.Name },
                    ControllerIds = controllerIds
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không bắn được NATS đổi kịch bản {Code} cho FE", scene.Code);
            }
        }

        private async Task<string> WriteTriggerLogAsync(
            VwEventRule? rule, string? eventTypeId, string sceneId,
            DateTime triggeredAt, BaseEnums.SuccessEnums success)
        {
            var log = new VwEventTriggerLog
            {
                RuleId = rule?.ID,
                EventTypeId = rule?.EventTypeId ?? eventTypeId,
                TargetSceneId = sceneId,
                Action = VwEventTriggerAction.ActiveScene,
                TriggeredAt = triggeredAt,
                Success = success,
                CreateUId = App.User == null ? "system" : App.User.GetUsername(),
                CreateTime = triggeredAt,
            };

            await _eventTriggerLogService.Write(log);
            return log.ID;
        }

        public static int GetPriorityRank(string? priority)
        {
            if (string.IsNullOrWhiteSpace(priority))
                return 999;

            return priority.Trim().ToUpperInvariant() switch
            {
                "CRITICAL" => 0,
                "HIGH" => 1,
                "NORMAL" => 2,
                "LOW" => 3,
                _ => 999,
            };
        }
    }
}
