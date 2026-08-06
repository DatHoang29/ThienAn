using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Dto.Session;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.ShareData.Controllers.Partner;

/// <summary>
/// Description: API quản lý Đối tác chia sẻ dữ liệu (ShareDataPartner)
/// Created date: 2026-08-04
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 610)]
public class ShareDataPartnerController : BaseController
{
    /// <summary>
    /// Description: Lấy danh sách đối tác theo phân trang
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get ShareDataPartner page list")]
    public virtual async Task<SqlSugarPagedList<ShareDataPagePartnerOutput>> Page(ShareDataPagePartnerInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<ShareDataPagePartnerOutput>>(input);
    }

    /// <summary>
    /// Description: Lấy toàn bộ danh sách đối tác (không phân trang)
    /// Created date: 2026-08-04
    /// </summary>
    [DisplayName("Get ShareDataPartner list")]
    public async Task<List<ShareDataPartnerOutput>> GetList([FromQuery] ShareDataPartnerInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataPartnerOutput>>(input);
    }

    /// <summary>
    /// Description: Lấy chi tiết 1 đối tác theo ID
    /// Created date: 2026-08-04
    /// </summary>
    [DisplayName("Get ShareDataPartner by id")]
    public async Task<ShareDataPartnerOutput> GetById([FromQuery] ShareDataIdPartnerInput input)
    {
        return await MessBus.InvokeAsync<ShareDataPartnerOutput>(input);
    }

    /// <summary>
    /// Description: Thêm mới 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ShareDataPartner")]
    public async Task AddShareDataPartner(ShareDataAddPartnerInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Cập nhật 1 đối tác
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ShareDataPartner")]
    public async Task UpdateShareDataPartner(ShareDataUpdatePartnerInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Xóa 1 đối tác (xóa mềm, đặt trạng thái DISABLED)
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ShareDataPartner")]
    public async Task DeleteShareDataPartner(ShareDataDeletePartnerInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Xóa nhiều đối tác (xóa mềm)
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete multiple ShareDataPartner")]
    public async Task BatchDeleteShareDataPartner(List<ShareDataDeletePartnerInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Mở phiên kết nối tới đối tác (Initiate + Login)
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Connect"), HttpPost]
    [DisplayName("Connect ShareDataPartner")]
    public async Task<ShareDataSessionOutput> ConnectShareDataPartner(ShareDataConnectPartnerInput input)
    {
        return await MessBus.InvokeAsync<ShareDataSessionOutput>(input);
    }

    /// <summary>
    /// Description: Ngắt phiên kết nối với đối tác
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "Disconnect"), HttpPost]
    [DisplayName("Disconnect ShareDataPartner")]
    public async Task DisconnectShareDataPartner(ShareDataDisconnectPartnerInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Description: Thử kết nối tới đối tác, trả kết quả từng bước
    /// Created date: 2026-08-04
    /// </summary>
    [ApiDescriptionSettings(Name = "TestConnection"), HttpPost]
    [DisplayName("Test ShareDataPartner connection")]
    public async Task<List<ShareDataTestConnectionStepOutput>> TestShareDataPartnerConnection(ShareDataTestConnectionPartnerInput input)
    {
        return await MessBus.InvokeAsync<List<ShareDataTestConnectionStepOutput>>(input);
    }
}
