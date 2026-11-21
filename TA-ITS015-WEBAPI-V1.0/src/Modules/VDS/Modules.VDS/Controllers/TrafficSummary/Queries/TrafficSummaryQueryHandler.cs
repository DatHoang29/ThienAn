using Modules.VDS.Controllers.TrafficSummary.Dto;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Infrastructure.Services.TrafficSummary;
using Shared.Utility.Counting;

namespace Modules.VDS.Controllers.TrafficSummary.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler tra số liệu tổng hợp đã chốt theo giờ/ngày/tháng từ <c>TmsTrafficSummary</c>.
///
/// <para>Bảng này do worker chốt, API chỉ đọc. Không có watermark phủ dữ liệu nên <c>Completeness</c> luôn
/// là <c>unverified</c>: bên gọi KHÔNG được suy ra "đã đủ lịch sử" từ việc có dòng trả về.</para>
/// Created date: 21/09/2026
/// </summary>
public class TrafficSummaryQueryHandler : IWolverineHandler
{
    private readonly VdsTrafficSummaryService _service;

    public TrafficSummaryQueryHandler(VdsTrafficSummaryService service)
    {
        _service = service;
    }

    /// <summary>Số liệu tổng hợp trong cửa sổ, kèm khoảng phủ theo từng phạm vi.</summary>
    public async Task<TrafficSummaryOutput> HandleAsync(TrafficSummaryInput command)
    {
        var (from, to) = command.Window();
        if (!string.IsNullOrWhiteSpace(command.LicensePlate)) throw Oops.Oh(BaseMsg.Vds.Exception.SummaryPlateNotSupported);
        // Chặn lọc chéo phạm vi: hỏi theo thiết bị mà truyền zoneId (hoặc ngược lại) là câu hỏi mâu thuẫn,
        // trả rỗng một cách im lặng sẽ bị hiểu là "không có dữ liệu".
        if (command.ScopeType == TrafficCountScopes.Equipment && command.ZoneId != null || command.ScopeType == TrafficCountScopes.Zone && command.EquipmentId != null)
            throw Oops.Oh(BaseMsg.Vds.Exception.SummaryScopeFilterMismatch);

        var id = command.ScopeId ?? (command.ScopeType == TrafficCountScopes.Equipment ? command.EquipmentId : command.ZoneId);
        var rows = await _service.ReadAsync(from, to, command.PeriodType, command.ScopeType, id, command.Direction);

        return new TrafficSummaryOutput
        {
            Source = "TmsTrafficSummary",
            RequestedWindow = new TrafficSummaryWindowOutput { From = from, To = to },
            ComputedAt = rows.Max(x => x.ComputedAt),
            Coverage = rows.GroupBy(x => x.ScopeId).Select(g => new TrafficSummaryCoverageOutput
            {
                ScopeId = g.Key,
                Intervals = g.Select(x => new TrafficSummaryIntervalOutput { From = x.FromTime, To = x.ToTime }).ToList()
            }).ToList(),
            // Summary không có watermark phủ dữ liệu; bên gọi không được suy ra đã đủ lịch sử từ số dòng trả về.
            Completeness = "unverified",
            Rows = rows.Select(x => x.Adapt<TrafficSummaryRowOutput>()).ToList()
        };
    }
}
