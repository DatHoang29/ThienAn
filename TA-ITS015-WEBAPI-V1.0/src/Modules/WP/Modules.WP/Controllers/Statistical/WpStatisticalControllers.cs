using Mapster;
using Microsoft.AspNetCore.Mvc;
using Modules.WP.Controllers;
using Modules.WP.Controllers.Statistical.Dto;

namespace Modules.WP.Controllers.Statistical;      

/// <summary>
/// Author: HieuNV
/// Description: WP Statistical - Thống kê hệ thống WP
/// Created date: 03/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName,Order = 460)]
public class WpStatisticalController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy details
    /// Created date: 14/08/2026
    /// </summary>
    [DisplayName("Get Wp Statistical")]
    public async Task<List<WpStatisticalChartOutput>> GetDetails([FromQuery] WpStatisticalChartInput input)
    {
        return await MessBus.InvokeAsync<List<WpStatisticalChartOutput>>(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Update Wp Config
    /// Created date: 14/08/2026
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>

    [DisplayName("Count Wp Statistical")]
    [HttpPost]
    public async Task Count([FromBody] WpStatisticalCountInput input)
    {
        await MessBus.InvokeAsync(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy summary
    /// Created date: 14/08/2026
    /// </summary>

    [DisplayName("Get Wp Statistical Summary")]
    public async Task<WpStatisticalSummaryOutput> GetSummary([FromQuery] WpStatisticalSummaryInput input)
    {
        return await MessBus.InvokeAsync<WpStatisticalSummaryOutput>(input);
    }
}
