using System.Threading.Tasks;
using Modules.VMS.Controllers.EquipmentTelemetry.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;

namespace Modules.VMS.Controllers.EquipmentTelemetry.Commands
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Xử lý lệnh telemetry thiết bị VMS/LCS.
    /// Created date: 2026-08-20
    ///
    /// CẬP NHẬT 23/09/2026: bỏ 2 lệnh bật/tắt job Hangfire. Việc lập lịch thu thập telemetry đã chuyển
    /// sang dịch vụ ITS.VMS.Worker (cấu hình Telemetry:CollectIntervalMinutes), BE không còn giữ nhịp.
    /// </summary>
    public class EquipmentTelemetryCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VmsEquipmentTelemetry> _repository;

        public EquipmentTelemetryCommandHandler(BaseRepository<VmsEquipmentTelemetry> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Xóa một bản ghi lịch sử telemetry thiết bị.
        /// </summary>
        public async Task HandleAsync(DeleteVmsEquipmentTelemetryInput command)
        {
            var entity = await _repository.GetFirstAsync(x => x.ID == command.ID);
            if (entity == null) throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _repository.DeleteAsync(entity);
        }
    }
}
