using Module.ShareData.Core.Dto.Subscription;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.ShareData.Controllers.Subscription;

/// <summary>
/// Description: API quản lý Đăng ký chia sẻ dữ liệu gửi/nhận (ShareDataSubscription)
/// Created date: 2026-08-04
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 620)]
public class ShareDataSubscriptionController : BaseController
{
    /// <summary>
    /// Description: Lấy danh sách đăng ký theo phân trang
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get ShareDataSubscription page list")]
    public virtual async Task<SqlSugarPagedList<ShareDataPageSubscriptionOutput>> Page(ShareDataPageSubscriptionInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<ShareDataPageSubscriptionOutput>>(input);
    }

    /// <summary>
    /// Description: Lấy toàn bộ danh sách đăng ký (không phân trang)
    /// Created date: 2026-08-04
    /// </summary>
    [DisplayName("Get ShareDataSubscription list")]
    public async Task<List<ShareDataSubscriptionOutput>> GetList([FromQuery] ShareDataSubscriptionInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataSubscriptionOutput>>(input);
    }

    /// <summary>
    /// Description: Lấy chi tiết 1 đăng ký theo ID
    /// Created date: 2026-08-04
    /// </summary>
    [DisplayName("Get ShareDataSubscription by id")]
    public async Task<ShareDataSubscriptionOutput> GetById([FromQuery] ShareDataIdSubscriptionInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSubscriptionOutput>(input);
    }

    /// <summary>
    /// Description: Thêm mới 1 đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ShareDataSubscription")]
    public async Task<ShareDataSubscriptionOutput> AddShareDataSubscription(ShareDataAddSubscriptionInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSubscriptionOutput>(input);
    }

    /// <summary>
    /// Description: Cập nhật 1 đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ShareDataSubscription")]
    public async Task<ShareDataSubscriptionOutput> UpdateShareDataSubscription(ShareDataUpdateSubscriptionInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSubscriptionOutput>(input);
    }

    /// <summary>
    /// Description: Xóa 1 đăng ký chia sẻ (xóa mềm)
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ShareDataSubscription")]
    public async Task DeleteShareDataSubscription(ShareDataDeleteSubscriptionInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Xóa nhiều đăng ký chia sẻ (xóa mềm)
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple ShareDataSubscription")]
    public async Task BatchDeleteShareDataSubscription(List<ShareDataDeleteSubscriptionInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Tắt tạm 1 đăng ký (ACTIVE → PAUSED)
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Pause"), HttpPost]
    [DisplayName("Pause ShareDataSubscription")]
    public async Task<ShareDataSubscriptionOutput> PauseShareDataSubscription(ShareDataPauseSubscriptionInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSubscriptionOutput>(input);
    }

    /// <summary>
    /// Description: Bật lại 1 đăng ký (PAUSED → ACTIVE)
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Resume"), HttpPost]
    [DisplayName("Resume ShareDataSubscription")]
    public async Task<ShareDataSubscriptionOutput> ResumeShareDataSubscription(ShareDataResumeSubscriptionInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSubscriptionOutput>(input);
    }

    /// <summary>
    /// Description: Hủy 1 đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Cancel"), HttpPost]
    [DisplayName("Cancel ShareDataSubscription")]
    public async Task CancelShareDataSubscription(ShareDataCancelSubscriptionInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Duyệt 1 đăng ký INBOUND (PENDING → ACTIVE)
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Approve"), HttpPost]
    [DisplayName("Approve ShareDataSubscription")]
    public async Task<ShareDataSubscriptionOutput> ApproveShareDataSubscription(ShareDataApproveSubscriptionInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSubscriptionOutput>(input);
    }

    /// <summary>
    /// Description: Từ chối 1 đăng ký INBOUND
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Reject"), HttpPost]
    [DisplayName("Reject ShareDataSubscription")]
    public async Task RejectShareDataSubscription(ShareDataRejectSubscriptionInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
