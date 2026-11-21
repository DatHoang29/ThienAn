using Shared.Infrastructure.Persistence.SqlSugar;
using Modules.Mail.Core.Dto.TemplateMail;

namespace Modules.Mail.Controllers.EmailTemplate;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý mẫu email (MailTemplate) soạn bằng wangEditor. Nội dung HTML lưu file
///              wwwroot/Upload/Mail/Template/{ID}.html, DB lưu đường dẫn. Mẫu là nội dung khởi đầu khi soạn lịch gửi.
/// Created date: 12/10/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 605)]
public class RptEmailTemplateController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách mẫu email theo phân trang
    /// Created date: 12/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get MailTemplate page list")]
    public async Task<SqlSugarPagedList<RptPageEmailTemplateOutput>> Page(MailPageTemplateInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<RptPageEmailTemplateOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ mẫu email (không phân trang, không kèm nội dung) — ô chọn mẫu ở form lịch gửi
    /// Created date: 12/10/2026
    /// </summary>
    [DisplayName("Get MailTemplate list")]
    public async Task<List<RptEmailTemplateOutput>> GetList([FromQuery] MailTemplateInput input)
    {
        return await MessBus.InvokeAsync<List<RptEmailTemplateOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 mẫu email theo ID, kèm nội dung HTML (Content)
    /// Created date: 12/10/2026
    /// </summary>
    [DisplayName("Get MailTemplate by id")]
    public async Task<RptEmailTemplateOutput> GetById([FromQuery] MailIdTemplateInput input)
    {
        return await MessBus.InvokeAsync<RptEmailTemplateOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 mẫu email theo mã, kèm nội dung HTML — form lịch nạp nội dung mẫu vừa chọn vào editor
    /// Created date: 12/10/2026
    /// </summary>
    [DisplayName("Get MailTemplate by code")]
    public async Task<RptEmailTemplateOutput> GetByCode([FromQuery] MailCodeTemplateInput input)
    {
        return await MessBus.InvokeAsync<RptEmailTemplateOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add MailTemplate")]
    public async Task AddRptEmailTemplate(MailAddTemplateInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update MailTemplate")]
    public async Task UpdateRptEmailTemplate(MailUpdateTemplateInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Bật / tắt 1 mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "ToggleState"), HttpPost]
    [DisplayName("Toggle MailTemplate state")]
    public async Task ToggleRptEmailTemplateState(MailToggleTemplateInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete MailTemplate")]
    public async Task DeleteRptEmailTemplate(MailDeleteTemplateInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete batch MailTemplate")]
    public async Task BatchDeleteRptEmailTemplate(List<MailDeleteTemplateInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
}
