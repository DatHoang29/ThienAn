using Shared.Infrastructure.Persistence.SqlSugar;
using Modules.Mail.Core.Dto.LogMail;

namespace Modules.Mail.Controllers.EmailLog;

/// <summary>
/// Author: HieuNV
/// Description: API nhật ký gửi mail (RptEmailLog) — chỉ đọc: phân trang và chi tiết 1 lần gửi.
///              Log được ghi bởi job gửi theo lịch, nút Gửi ngay và API gửi thử; dọn log cũ bằng job "mail-log-cleanup".
/// Created date: 02/10/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 590)]
public class RptEmailLogController : BaseController
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Nhật ký gửi theo phân trang
    /// Created date: 02/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get RptEmailLog page list")]
    public async Task<SqlSugarPagedList<MailLogOutput>> Page(MailPageLogInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<MailLogOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Chi tiết 1 lần gửi
    /// Created date: 02/10/2026
    /// </summary>
    [DisplayName("Get RptEmailLog by id")]
    public async Task<MailLogOutput> GetById([FromQuery] MailIdLogInput input)
    {
        return await MessBus.InvokeAsync<MailLogOutput>(input);
    }
}
