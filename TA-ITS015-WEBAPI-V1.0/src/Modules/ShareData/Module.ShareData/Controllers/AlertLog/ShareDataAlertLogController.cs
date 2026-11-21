using Module.ShareData.Core.Dto.AlertLog;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.ShareData.Controllers.AlertLog;

/// <summary>
/// Author: HieuNV
/// Description: API tra cứu Cảnh báo và lỗi phân hệ Chia sẻ dữ liệu (ShareDataAlertLog).
///              Service ghi vào bảng này — không có add / update / delete thủ công,
///              người dùng chỉ xác nhận đã xử lý.
/// Created date: 2026-08-21
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 660)]
public class ShareDataAlertLogController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách cảnh báo theo phân trang
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get ShareDataAlertLog page list")]
    public virtual async Task<SqlSugarPagedList<ShareDataPageAlertLogOutput>> Page(ShareDataPageAlertLogInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<ShareDataPageAlertLogOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách cảnh báo theo tiêu chí lọc (không phân trang, có giới hạn TopN)
    /// Created date: 2026-08-21
    /// </summary>
    [DisplayName("Get ShareDataAlertLog list")]
    public async Task<List<ShareDataAlertLogOutput>> GetList([FromQuery] ShareDataAlertLogInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataAlertLogOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 cảnh báo theo ID (kèm DetailJson)
    /// Created date: 2026-08-21
    /// </summary>
    [DisplayName("Get ShareDataAlertLog by id")]
    public async Task<ShareDataAlertLogOutput> GetById([FromQuery] ShareDataIdAlertLogInput input)
    {
        return await MessBus.InvokeAsync<ShareDataAlertLogOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Đếm số cảnh báo chưa xác nhận — dùng cho chấm đỏ trên menu
    /// Created date: 2026-08-21
    /// </summary>
    [DisplayName("Count unacknowledged ShareDataAlertLog")]
    public async Task<int> CountUnacked([FromQuery] ShareDataCountUnackedAlertLogInput input)
    {
        return await MessBus.InvokeAsync<int>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thống kê nhanh phục vụ các thẻ số liệu phía trên lưới cảnh báo
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Summary"), HttpPost]
    [DisplayName("Get ShareDataAlertLog summary")]
    public async Task<ShareDataSummaryAlertLogOutput> Summary(ShareDataSummaryAlertLogInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSummaryAlertLogOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xác nhận đã xử lý một hoặc nhiều cảnh báo
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "Acknowledge"), HttpPost]
    [DisplayName("Acknowledge ShareDataAlertLog")]
    public async Task<int> Acknowledge(ShareDataAcknowledgeAlertInput input)
    {
        return await MessBus.InvokeAsync<int>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xác nhận toàn bộ cảnh báo chưa xử lý theo bộ lọc hiện tại
    /// Created date: 2026-08-21
    /// </summary>
    [ApiDescriptionSettings(Name = "AcknowledgeAll"), HttpPost]
    [DisplayName("Acknowledge all ShareDataAlertLog")]
    public async Task<int> AcknowledgeAll(ShareDataAcknowledgeAllAlertInput input)
    {
        return await MessBus.InvokeAsync<int>(input);
    }
}
