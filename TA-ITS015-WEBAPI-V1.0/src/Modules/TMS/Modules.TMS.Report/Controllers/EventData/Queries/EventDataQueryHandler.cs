using Modules.TMS.Core.Entities;
using Modules.TMS.Report.Controllers.EventData.Dto;
using Modules.TMS.Report.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.TMS.Report.Controllers.EventData.Queries
{
    /// <summary>
    /// Truy vấn danh sách dữ liệu sự kiện VDS.
    /// </summary>
    public class EventDataQueryHandler
    {
        private readonly BaseRepository<TmsEventData> _tmsEventDataRep;

        public EventDataQueryHandler(BaseRepository<TmsEventData> tmsEventDataRep)
        {
            _tmsEventDataRep = tmsEventDataRep;
        }

        public async Task<SqlSugarPagedList<PageEventDataOutput>> HandleAsync(PageEventDataInput command)
        {
            if (command.StartTime == null || command.EndTime == null)
            {
                command.StartTime = global::System.DateTime.Now.AddDays(-3);
                command.EndTime = global::System.DateTime.Now;
            }

            return await _tmsEventDataRep.AsQueryable()
                .Where(t => t.DetectTime >= command.StartTime && t.DetectTime <= command.EndTime)
                .WhereIF(!string.IsNullOrEmpty(command.Direction) && command.Direction != "0", t => t.Direction == command.Direction)
                .WhereIF(!string.IsNullOrEmpty(command.VehicleType), t => t.VehicleType == command.VehicleType)
                .WhereIF(!string.IsNullOrEmpty(command.EquipmentId), t => t.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrEmpty(command.EventTypeId), t => t.EventTypeId == command.EventTypeId)
                .WhereIF(!string.IsNullOrEmpty(command.State), t => t.State == command.State)
                .WhereIF(!string.IsNullOrEmpty(command.LicensePlate), t => !string.IsNullOrEmpty(t.LicensePlate) && t.LicensePlate.ToLower() == command.LicensePlate.ToLower())
                .Select(t => new PageEventDataOutput { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        public async Task<List<EventDataOutput>> HandleAsync(EventDataInput command)
        {
            return await _tmsEventDataRep.AsQueryable()
                .WhereIF(command.StartTime.HasValue, t => t.DetectTime >= command.StartTime)
                .WhereIF(command.EndTime.HasValue, t => t.DetectTime <= command.EndTime)
                .WhereIF(!string.IsNullOrEmpty(command.Direction) && command.Direction != "0", t => t.Direction == command.Direction)
                .WhereIF(!string.IsNullOrEmpty(command.VehicleType), t => t.VehicleType == command.VehicleType)
                .WhereIF(!string.IsNullOrEmpty(command.EquipmentId), t => t.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrEmpty(command.EventTypeId), t => t.EventTypeId == command.EventTypeId)
                .WhereIF(!string.IsNullOrEmpty(command.State), t => t.State == command.State)
                .WhereIF(!string.IsNullOrEmpty(command.LicensePlate), t => !string.IsNullOrEmpty(t.LicensePlate) && t.LicensePlate.ToLower() == command.LicensePlate.ToLower())
                .Select(t => new EventDataOutput { }, true)
                .OrderByDescending(t => t.DetectTime)
                .ToListAsync();
        }
    }
}
