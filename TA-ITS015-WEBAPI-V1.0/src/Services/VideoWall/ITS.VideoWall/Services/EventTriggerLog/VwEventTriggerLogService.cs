using Microsoft.Extensions.Logging;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using ITS.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace ITS.VideoWall.Services.EventTriggerLog
{
    /// <summary>
    /// Description: Hiện thực IVwEventTriggerLogService cho Worker VideoWall.
    ///              Chạy ngầm (background/daemon), ghi log với OperatorName = "system", nuốt lỗi an toàn.
    /// Created date: 11/09/2026
    /// </summary>
    public class VwEventTriggerLogService(
        BaseRepository<VwEventTriggerLog> vwEventTriggerLogRep,
        BaseRepository<VwController> vwControllerRep,
        ILogger<VwEventTriggerLogService> logger) : IVwEventTriggerLogService
    {
        private readonly BaseRepository<VwEventTriggerLog> _vwEventTriggerLogRep = vwEventTriggerLogRep;
        private readonly BaseRepository<VwController> _vwControllerRep = vwControllerRep;
        private readonly ILogger<VwEventTriggerLogService> _logger = logger;

        public async Task WriteSteps(
            string action,
            string? controllerId,
            string? sceneId,
            string? sceneCode,
            IReadOnlyList<VwSetupSceneStep>? steps,
            CancellationToken ct = default)
        {
            if (steps is null || steps.Count == 0)
                return;

            try
            {
                var controller = await LoadController(controllerId, ct);
                var occurredAt = DateTime.Now;
                const string operatorName = "system";

                var rows = steps
                    .Select(step => BuildRow(action, controller, controllerId, sceneId, sceneCode, step, occurredAt, operatorName))
                    .ToList();

                await _vwEventTriggerLogRep.InsertRangeAsync(rows);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "VwEventTriggerLogService (Worker): không ghi được nhật ký {Action} của bộ điều khiển {ControllerId} ({StepCount} bước).",
                    action,
                    controllerId ?? "(ad-hoc)",
                    steps.Count);
            }
        }

        public Task WriteStep(
            string action,
            string? controllerId,
            string? sceneId,
            string? sceneCode,
            VwSetupSceneStep? step,
            CancellationToken ct = default)
        {
            if (step is null)
                return Task.CompletedTask;

            return WriteSteps(action, controllerId, sceneId, sceneCode, [step], ct);
        }

        public async Task Write(VwEventTriggerLog log, CancellationToken ct = default)
        {
            try
            {
                var controller = await LoadController(log.ControllerId, ct);

                // VwController không còn OrgId (26/09/2026): OrgId của log giữ nguyên giá trị bên gọi truyền vào.
                log.ControllerName = controller?.Name ?? log.ControllerName;
                log.TriggeredAt ??= DateTime.Now;
                log.OperatorName ??= "system";
                log.OperatorIp ??= null;
                log.Message = Clip(log.Message, EntityConst.Length512);
                log.StepName = Clip(log.StepName, EntityConst.Length128);
                log.Endpoint = Clip(log.Endpoint, EntityConst.Length512);
                log.Method = Clip(log.Method, EntityConst.Length16);
                log.CreateUId ??= log.OperatorName;
                log.CreateTime = log.TriggeredAt.Value;

                await _vwEventTriggerLogRep.InsertAsync(log);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "VwEventTriggerLogService (Worker): không ghi được nhật ký {Action} của bộ điều khiển {ControllerId}.",
                    log.Action,
                    log.ControllerId);
            }
        }

        private static VwEventTriggerLog BuildRow(
            string action,
            VwController? controller,
            string? controllerId,
            string? sceneId,
            string? sceneCode,
            VwSetupSceneStep step,
            DateTime occurredAt,
            string operatorName) => new()
            {
                Action = Clip(action, EntityConst.Length32),
                ControllerId = controller?.ID ?? controllerId,
                ControllerName = controller?.Name,
                TargetSceneId = sceneId,
                SceneCode = Clip(sceneCode, EntityConst.Length64),
                StepOrder = step.Order,
                StepName = Clip(step.Name, EntityConst.Length128),
                Method = Clip(step.Method, EntityConst.Length16),
                Endpoint = Clip(step.Endpoint, EntityConst.Length512),
                Skipped = step.Skipped,
                Success = step.Skipped
                    ? null
                    : step.Success ? BaseEnums.SuccessEnums.Success : BaseEnums.SuccessEnums.Fail,
                HttpStatus = step.HttpStatus,
                Message = Clip(step.Message, EntityConst.Length512),
                RequestPayload = step.RequestXml,
                ResponsePayload = step.ResponseXml,
                DurationMs = step.DurationMs,
                TriggeredAt = occurredAt,
                OperatorName = operatorName,
                OperatorIp = null,
                CreateUId = operatorName,
                CreateTime = occurredAt,
            };

        private async Task<VwController?> LoadController(string? controllerId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(controllerId))
                return null;

            return await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ID == controllerId)
                .FirstAsync(ct);
        }

        private static string? Clip(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value[..maxLength];
        }
    }
}
