using Module.ShareData.Core.Dto.ActivityLog;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.ShareData.Controllers.ActivityLog;

/// <summary>
/// Author: HieuNV
/// Description: API tra cứu Nhật ký hoạt động phân hệ Chia sẻ dữ liệu (ShareDataActivityLog)
/// Created date: 2026-08-05
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 630)]
public class ShareDataActivityLogController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy nhật ký hoạt động theo phân trang
    /// Created date: 2026-08-05
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get ShareDataActivityLog page list")]
    public virtual async Task<SqlSugarPagedList<ShareDataPageActivityLogOutput>> Page(ShareDataPageActivityLogInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<ShareDataPageActivityLogOutput>>(input);
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy nhật ký hoạt động theo tiêu chí lọc (không phân trang, có giới hạn TopN)
    /// Created date: 2026-08-05
    /// </summary>
    [DisplayName("Get ShareDataActivityLog list")]
    public async Task<List<ShareDataActivityLogOutput>> GetList([FromQuery] ShareDataActivityLogInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataActivityLogOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 bản ghi nhật ký theo ID (kèm BeforeJson / AfterJson)
    /// Created date: 2026-08-05
    /// </summary>
    [DisplayName("Get ShareDataActivityLog by id")]
    public async Task<ShareDataActivityLogOutput> GetById([FromQuery] ShareDataIdActivityLogInput input)
    {
        return await MessBus.InvokeAsync<ShareDataActivityLogOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thống kê nhanh phục vụ các thẻ số liệu phía trên lưới nhật ký
    /// Created date: 2026-08-05
    /// </summary>
    [ApiDescriptionSettings(Name = "Summary"), HttpPost]
    [DisplayName("Get ShareDataActivityLog summary")]
    public async Task<ShareDataSummaryActivityLogOutput> Summary(ShareDataSummaryActivityLogInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSummaryActivityLogOutput>(input);
    }

    /// <summary>
    /// Description: Lấy cây một phiên truyền nhận theo ID dòng cha: dòng cha kèm các bước xử lý lồng trong
    /// Created date: 02/10/2026
    /// </summary>
    [DisplayName("Get ShareDataActivityLog steps")]
    public async Task<List<ShareDataActivityLogOutput>> GetSteps([FromQuery] ShareDataStepActivityLogInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataActivityLogOutput>>(input);
    }
}
