using Modules.TMS.Core.Entities;
using Modules.VDS.Controllers.Event.Dto;
using Modules.VDS.Controllers.TrafficData.Dto;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Dto;
using Modules.VDS.Infrastructure;
using Modules.VDS.Infrastructure.Services.TrafficData;
using System.Globalization;

namespace Modules.VDS.Controllers.Event.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler cho chi tiết một bản tin sự kiện VDS.
///
/// <para>Bản tin nằm ở QuestDB (đọc qua <see cref="VdsRecordReaderService"/>), sự cố liên quan nằm ở SQL Server
/// và được nối bằng <c>TmsIncident.EventId</c> = <c>vendorEventId</c> dạng chuỗi.</para>
/// <para>Phân trang sự kiện KHÔNG nằm ở đây: nó dùng chung truy vấn với TrafficData, chỉ khác cờ
/// <c>EventsOnly</c>, nên controller dispatch thẳng sang handler của TrafficData.</para>
/// Created date: 21/09/2026
/// </summary>
public class EventQueryHandler : IWolverineHandler
{
    private readonly VdsRecordReaderService _records;
    private readonly BaseRepository<TmsIncident> _incidents;

    public EventQueryHandler(VdsRecordReaderService records, BaseRepository<TmsIncident> incidents)
    {
        _records = records;
        _incidents = incidents;
    }

    /// <summary>Chi tiết một bản tin theo cặp eventId + detectTime, kèm sự cố và khóa tra media.</summary>
    public async Task<EventDetailOutput> HandleAsync(EventDetailInput command)
    {
        var row = await _records.ReadAsync(command.EventId, command.DetectTime);
        var key = command.EventId.ToString(CultureInfo.InvariantCulture);
        var linked = await _incidents.AsQueryable().Where(x => x.EventId == key && x.IsDelete == null).ToListAsync();

        return new EventDetailOutput
        {
            Data = row.Adapt<TrafficDataOutput>(),
            Incidents = linked.Select(x => x.Adapt<VdsLinkedIncidentOutput>()).ToList(),
            Media = new EventMediaOutput
            {
                EventId = command.EventId,
                DetectTime = row.DetectTime,
                // Danh sách kind chỉ là các nhánh media CÓ THỂ tra, không cam kết file đã tồn tại.
                // Lấy từ VdsMediaKindConst để không lệch với bảng hậu tố mà VdsMediaLocator dùng khi đi tìm file.
                Kinds = [.. VdsMediaKindConst.All]
            }
        };
    }
}
