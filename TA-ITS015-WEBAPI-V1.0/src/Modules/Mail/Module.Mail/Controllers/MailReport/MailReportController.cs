using Shared.Infrastructure.Persistence.SqlSugar;
using Modules.Mail.Core.Dto.EmailReport;

namespace Modules.Mail.Controllers.EmailReport;

/// <summary>
/// Author: HieuNV
/// Description: API danh mục báo cáo được phép đặt lịch gửi email (RptEmailReport), gom nhóm theo hệ
///              (SysPlugin.Code). Danh sách nhóm lấy qua API plugin sẵn có của hệ thống.
/// Created date: 01/10/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 600)]
public class RptEmailReportController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh mục báo cáo theo phân trang
    /// Created date: 01/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get RptEmailReport page list")]
    public async Task<SqlSugarPagedList<MailPageReportOutput>> Page(MailRptPageReportInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<MailPageReportOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh mục báo cáo (không phân trang) — ô chọn báo cáo ở form lịch
    /// Created date: 01/10/2026
    /// </summary>
    [DisplayName("Get RptEmailReport list")]
    public async Task<List<MailReportOutput>> GetList([FromQuery] MailRptReportInput input)
    {
        return await MessBus.InvokeAsync<List<MailReportOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 báo cáo theo ID
    /// Created date: 01/10/2026
    /// </summary>
    [DisplayName("Get RptEmailReport by id")]
    public async Task<MailReportOutput> GetById([FromQuery] MailRptIdReportInput input)
    {
        return await MessBus.InvokeAsync<MailReportOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 báo cáo
    /// Created date: 01/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add RptEmailReport")]
    public async Task AddRptEmailReport(MailRptAddReportInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 báo cáo
    /// Created date: 01/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update RptEmailReport")]
    public async Task UpdateRptEmailReport(MailRptUpdateReportInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Bật / tắt 1 báo cáo
    /// Created date: 01/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "ToggleState"), HttpPost]
    [DisplayName("Toggle RptEmailReport state")]
    public async Task ToggleRptEmailReportState(MailRptToggleReportInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 báo cáo
    /// Created date: 01/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete RptEmailReport")]
    public async Task DeleteRptEmailReport(MailRptDeleteReportInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều báo cáo
    /// Created date: 01/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete batch RptEmailReport")]
    public async Task BatchDeleteRptEmailReport(List<MailRptDeleteReportInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
}
