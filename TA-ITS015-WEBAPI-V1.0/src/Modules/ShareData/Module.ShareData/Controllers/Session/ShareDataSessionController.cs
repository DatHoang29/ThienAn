using Module.ShareData.Core.Dto.Session;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.ShareData.Controllers.Session;

/// <summary>
/// Author: HieuNV
/// Description: API tra cứu Phiên kết nối C2C (ShareDataSession).
/// Created date: 2026-08-05
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 615)]
public class ShareDataSessionController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách phiên kết nối theo phân trang
    /// Created date: 2026-08-05
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get ShareDataSession page list")]
    public virtual async Task<SqlSugarPagedList<ShareDataPageSessionOutput>> Page(ShareDataPageSessionInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<ShareDataPageSessionOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách phiên kết nối
    /// Created date: 2026-08-05
    /// </summary>
    [DisplayName("Get ShareDataSession list")]
    public async Task<List<ShareDataSessionOutput>> GetList([FromQuery] ShareDataSessionInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataSessionOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 phiên kết nối theo ID
    /// Created date: 2026-08-05
    /// </summary>
    [DisplayName("Get ShareDataSession by id")]
    public async Task<ShareDataSessionOutput> GetById([FromQuery] ShareDataIdSessionInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSessionOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Số liệu tổng hợp cho các thẻ thống kê đầu màn vận hành
    /// Created date: 2026-08-05
    /// </summary>
    [ApiDescriptionSettings(Name = "Summary"), HttpPost]
    [DisplayName("Get ShareDataSession summary")]
    public async Task<ShareDataSummarySessionOutput> Summary(ShareDataSummarySessionInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSummarySessionOutput>(input);
    }
}
