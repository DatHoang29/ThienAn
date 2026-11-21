using Furion.FriendlyException;
using Modules.FMS.Controllers.EquipmentTelemetry.Dto;
using Modules.FMS.Core.Entities;
using Modules.FMS.Core.Exceptions;
using Modules.FMS.Infrastructure;
using Wolverine;

namespace Modules.FMS.Controllers.EquipmentTelemetry.Commands
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Xử lý lệnh telemetry thiết bị FMS: xóa bản ghi lịch sử.
    /// Created date: 2026-08-27
    /// </summary>
    public class EquipmentTelemetryCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<FmsEquipmentTelemetry> _repository;

        public EquipmentTelemetryCommandHandler(BaseRepository<FmsEquipmentTelemetry> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Xóa một bản ghi lịch sử telemetry thiết bị.
        /// </summary>
        public async Task HandleAsync(DeleteEquipmentTelemetryInput command)
        {
            var entity = await _repository.GetFirstAsync(x => x.ID == command.ID);
            if (entity == null) throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _repository.DeleteAsync(entity);
        }
    }
}
