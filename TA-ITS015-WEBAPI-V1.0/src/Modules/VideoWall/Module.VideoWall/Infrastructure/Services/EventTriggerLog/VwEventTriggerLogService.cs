using Furion;
using Furion.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Infrastructure;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Module.VideoWall.Infrastructure.Services.EventTriggerLog
{
    /// <summary>
    /// Author: Đạt
    /// Description: Hiện thực <see cref="IVwEventTriggerLogService"/> — ghi VwEventTriggerLog cho cả dòng
    ///              bước ISAPI và dòng quyết định nghiệp vụ kích hoạt kịch bản.
    ///
    ///              BA ĐIỂM THIẾT KẾ ĐÁNG CHÚ Ý:
    ///
    ///              1. Nuốt lỗi có kiểm soát. Toàn bộ thân hàm ghi nằm trong try/catch; lỗi chỉ ra
    ///                 _logger.LogWarning rồi đi tiếp. Lý do: lệnh xuống thiết bị đã chạy xong và
    ///                 kết quả thật phải về được tay người vận hành, không thể vì DB đầy đĩa mà báo
    ///                 lỗi đấu nối. Bước ISAPI THẤT BẠI thì VẪN ghi một dòng, mang HttpStatus và
    ///                 Message — mất dòng đó là mất đúng thứ cần điều tra.
    ///
    ///              2. Đóng dấu tên bộ điều khiển lúc ghi bằng cách tra VwController.
    ///                 VwController không còn OrgId (bỏ 26/09/2026) nên không đóng dấu đơn vị.
    ///
    ///              3. Cắt chuỗi trước khi ghi. Message của thiết bị có thể dài hơn cột 512 ký tự;
    ///                 không cắt thì SqlSugar ném lỗi truncate và mất trắng dòng log.
    /// Created date: 26/08/2026
    /// </summary>
    public class VwEventTriggerLogService(
        BaseRepository<VwEventTriggerLog> vwEventTriggerLogRep,
        BaseRepository<VwController> vwControllerRep,
        ILogger<VwEventTriggerLogService> logger) : IVwEventTriggerLogService, IScoped
    {
        private readonly BaseRepository<VwEventTriggerLog> _vwEventTriggerLogRep = vwEventTriggerLogRep;
        private readonly BaseRepository<VwController> _vwControllerRep = vwControllerRep;
        private readonly ILogger<VwEventTriggerLogService> _logger = logger;

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi toàn bộ bước của một lượt gọi bằng MỘT lệnh insert.
        ///              Gộp lại thay vì insert từng dòng vì một lần Probe sinh hàng chục bước.
        /// Created date: 26/08/2026
        /// </summary>
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
                var operatorName = CurrentOperator();
                var operatorIp = CurrentIp();

                var rows = steps
                    .Select(step => BuildRow(action, controller, controllerId, sceneId, sceneCode, step, occurredAt, operatorName, operatorIp))
                    .ToList();

                await _vwEventTriggerLogRep.InsertRangeAsync(rows);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "VwEventTriggerLogService: không ghi được nhật ký {Action} của bộ điều khiển {ControllerId} ({StepCount} bước).",
                    action,
                    controllerId,
                    steps.Count);
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi một bước đơn lẻ.
        /// Created date: 26/08/2026
        /// </summary>
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

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi một dòng đã dựng sẵn, server đóng dấu lại phần định danh.
        /// Created date: 26/08/2026
        /// </summary>
        public async Task Write(VwEventTriggerLog log, CancellationToken ct = default)
        {
            try
            {
                var controller = await LoadController(log.ControllerId, ct);

                // Đóng dấu lại tên bộ điều khiển theo DB. VwController không còn OrgId (26/09/2026)
                // nên OrgId của log giữ nguyên giá trị bên gọi truyền vào.
                log.ControllerName = controller?.Name ?? log.ControllerName;
                log.TriggeredAt ??= DateTime.Now;
                log.OperatorName ??= CurrentOperator();
                log.OperatorIp ??= CurrentIp();
                log.Message = Clip(log.Message, EntityConst.Length512);
                log.StepName = Clip(log.StepName, EntityConst.Length128);
                log.Endpoint = Clip(log.Endpoint, EntityConst.Length512);
                log.Method = Clip(log.Method, EntityConst.Length16);
                log.CreateUId ??= log.OperatorName;

                // TriggeredAt vừa được gán ở trên nên chắc chắn có giá trị.
                log.CreateTime = log.TriggeredAt.Value;

                await _vwEventTriggerLogRep.InsertAsync(log);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "VwEventTriggerLogService: không ghi được nhật ký {Action} của bộ điều khiển {ControllerId}.",
                    log.Action,
                    log.ControllerId);
            }
        }

        /// <summary>
        /// Description: Dựng một dòng nhật ký từ một bước ISAPI.
        /// </summary>
        private static VwEventTriggerLog BuildRow(
            string action,
            VwController? controller,
            string? controllerId,
            string? sceneId,
            string? sceneCode,
            VwSetupSceneStep step,
            DateTime occurredAt,
            string operatorName,
            string? operatorIp) => new()
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

                // Bước bị bỏ qua mang Success = true trong DTO, nhưng nó KHÔNG phải một lượt gửi
                // thành công. Ghi Success rỗng để thống kê tỉ lệ thành công không bị thổi phồng —
                // muốn lọc riêng nhóm này thì dùng cột Skipped.
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
                OperatorIp = operatorIp,
                CreateUId = operatorName,
                CreateTime = occurredAt,
            };

        /// <summary>
        /// Description: Tra bộ điều khiển để lấy Name. Không có ID hoặc không tìm thấy thì
        ///              trả null — dòng log vẫn được ghi, chỉ là không gắn được tên bộ điều khiển.
        /// </summary>
        private async Task<VwController?> LoadController(string? controllerId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(controllerId))
                return null;

            return await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ID == controllerId)
                .FirstAsync(ct);
        }

        /// <summary>
        /// Description: Tài khoản đang thao tác. Luồng nền hoặc lượt gọi anonymous ghi 'system'.
        /// </summary>
        private static string CurrentOperator()
        {
            try
            {
                return App.User == null ? "system" : App.User.GetUsername();
            }
            catch
            {
                return "system";
            }
        }

        /// <summary>
        /// Description: IP của người thao tác, rỗng khi chạy nền.
        /// </summary>
        private static string? CurrentIp()
        {
            try
            {
                return App.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Description: Cắt chuỗi cho vừa độ dài cột. Thiết bị trả message dài hơn cột là chuyện
        ///              thường, không cắt thì lỗi truncate làm mất cả dòng log.
        /// </summary>
        private static string? Clip(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value[..maxLength];
        }
    }
}
