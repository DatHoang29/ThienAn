using Mapster;
using Module.VideoWall.Core.Dto.EventTriggerLog;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Wolverine;

namespace Module.VideoWall.Controllers.EventTriggerLog.Commands
{
    /// <summary>
    /// Author: Đạt
    /// Description: Handler ghi 1 dòng nhật ký thao tác thiết bị / kích hoạt vào VwEventTriggerLog.
    ///
    ///              Chỉ ủy quyền sang <see cref="IVwEventTriggerLogWriter"/> — toàn bộ logic an toàn
    ///              (nuốt lỗi, đóng dấu lại OrgId / ControllerName / TriggeredAt / OperatorName, cắt chuỗi
    ///              quá dài) nằm ở <see cref="IVwEventTriggerLogWriter"/> để đường ghi qua API và đường
    ///              ghi nội bộ từ VwISAPIDeviceService.DeviceSetup.cs / VwSceneWorkflowCommandHandler.cs
    ///              có ĐÚNG MỘT chuẩn đóng dấu.
    /// Created date: 26/08/2026
    /// </summary>
    public class VwEventTriggerLogCommandHandler(IVwEventTriggerLogService eventTriggerLogService) : IWolverineHandler
    {
        private readonly IVwEventTriggerLogService _eventTriggerLogService = eventTriggerLogService;

        /// <summary>
        /// Author: Đạt
        /// Description: Xử lý lệnh ghi 1 dòng nhật ký.
        /// Created date: 26/08/2026
        /// </summary>
        public async Task HandleAsync(VwAddEventTriggerLogInput command, CancellationToken ct)
        {
            await _eventTriggerLogService.Write(command.Adapt<VwEventTriggerLog>(), ct);
        }
    }
}
