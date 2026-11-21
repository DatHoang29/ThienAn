using Modules.VDS.Controllers.Journey.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VDS.Controllers.Journey;

/// <summary>
/// Author: KienLT
/// Description: API hành trình A-B của VDS: danh sách phân trang và chi tiết kèm các sự cố liên quan.
/// Created date: 21/09/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 590)]
[ApiController]
public class VdsJourneyController : BaseController
{
    /// <summary>
    /// Get VDS journey pagination list
    /// </summary>
    /// <param name="input">Cửa sổ thời gian và bộ lọc đoạn tuyến / camera / biển số / tốc độ.</param>
    /// <returns>Trang danh sách hành trình.</returns>
    [DisplayName("Journey paged list")]
    public async Task<SqlSugarPagedList<PageJourneyOutput>> Page(JourneyInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageJourneyOutput>>(input);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Phân trang các chuyến xe, kèm tóm tắt và danh sách hành trình trong từng chuyến.
    /// Created date: 21/10/2026
    /// </summary>
    /// <param name="input">Cùng bộ lọc với <c>Page</c>; phân trang theo CHUYẾN (nhóm hành trình liên tiếp của một xe).</param>
    /// <returns>Trang các chuyến, mỗi chuyến kèm tóm tắt, chuỗi chính và mọi hành trình con.</returns>
    [DisplayName("Journey trip paged list")]
    public async Task<SqlSugarPagedList<JourneyTripOutput>> TripPage(JourneyInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<JourneyTripOutput>>(new JourneyTripInput { Filter = input });
    }

    /// <summary>
    /// Get VDS journey detail
    /// </summary>
    /// <param name="id">ID hành trình.</param>
    /// <returns>Hành trình, mốc camera hai đầu và các sự cố sinh ra từ chính hành trình đó.</returns>
    [HttpGet("Detail")]
    [DisplayName("Journey detail")]
    public async Task<JourneyDetailOutput> Detail([Required] string id)
    {
        return await MessBus.InvokeAsync<JourneyDetailOutput>(new JourneyDetailInput { ID = id });
    }
}
