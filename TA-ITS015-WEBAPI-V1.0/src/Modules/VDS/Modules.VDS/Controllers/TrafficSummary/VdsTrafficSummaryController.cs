using Modules.VDS.Controllers.TrafficSummary.Dto;

namespace Modules.VDS.Controllers.TrafficSummary;

/// <summary>
/// Author: KienLT
/// Description: API trả số liệu tổng hợp đã chốt theo giờ/ngày/tháng từ TmsTrafficSummary.
/// Created date: 21/09/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 587)]
[ApiController]
public class VdsTrafficSummaryController : BaseController
{
    /// <summary>
    /// Query VDS traffic summary
    /// </summary>
    /// <param name="input">Cửa sổ thời gian, chu kỳ chốt và phạm vi (thiết bị hoặc vùng).</param>
    /// <returns>Số liệu tổng hợp kèm khoảng phủ theo từng phạm vi.</returns>
    [HttpPost("Query")]
    [DisplayName("Traffic summary query")]
    public async Task<TrafficSummaryOutput> Query(TrafficSummaryInput input)
    {
        return await MessBus.InvokeAsync<TrafficSummaryOutput>(input);
    }
}
