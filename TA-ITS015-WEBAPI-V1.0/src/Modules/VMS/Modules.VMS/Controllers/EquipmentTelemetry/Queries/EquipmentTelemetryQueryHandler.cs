using System.Threading.Tasks;
using Modules.VMS.Controllers.EquipmentTelemetry.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.EquipmentTelemetry;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VMS.Controllers.EquipmentTelemetry.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Truy vấn telemetry thiết bị VMS/LCS: đọc thủ công (ảnh/stat từ thiết bị) và
    ///              phân trang lịch sử đã lưu.
    /// Created date: 2026-08-20
    /// </summary>
    public class EquipmentTelemetryQueryHandler : IScoped
    {
        private readonly BaseRepository<VmsEquipmentTelemetry> _repository;
        private readonly VmsEquipmentTelemetryService _telemetryService;

        public EquipmentTelemetryQueryHandler(BaseRepository<VmsEquipmentTelemetry> repository,
            VmsEquipmentTelemetryService telemetryService)
        {
            _repository = repository;
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// Đọc ảnh hiện tại đang hiển thị trên thiết bị (thủ công).
        /// </summary>
        public async Task<ReadDeviceImageOutput> HandleAsync(ReadDeviceImageInput command)
        {
            return await _telemetryService.ReadImageAsync(command);
        }

        /// <summary>
        /// Đọc trạng thái bên trong thiết bị (thủ công), gom nhóm theo nội dung.
        /// </summary>
        public async Task<GetDeviceStatOutput> HandleAsync(GetDeviceStatInput command)
        {
            return await _telemetryService.GetStatAsync(command);
        }

        /// <summary>
        /// Phân trang lịch sử telemetry thiết bị đã lưu.
        /// </summary>
        public async Task<SqlSugarPagedList<PageVmsEquipmentTelemetryOutput>> HandleAsync(PageVmsEquipmentTelemetryInput command)
        {
            return await _repository.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentCode), x => x.EquipmentCode != null && x.EquipmentCode.Contains(command.EquipmentCode))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentTypeId), x => x.EquipmentTypeId == command.EquipmentTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Source), x => x.Source == command.Source)
                .WhereIF(command.Succeeded != null, x => x.Succeeded == command.Succeeded)
                .WhereIF(command.FromDate != null, x => x.CollectedTime >= command.FromDate)
                .WhereIF(command.ToDate != null, x => x.CollectedTime <= command.ToDate)
                .Select((x) => new PageVmsEquipmentTelemetryOutput() { }, true)
                .OrderBuilder(command, "", "CollectedTime", false)
                .ToPagedListAsync(command.Page, command.PageSize);
        }
    }
}
