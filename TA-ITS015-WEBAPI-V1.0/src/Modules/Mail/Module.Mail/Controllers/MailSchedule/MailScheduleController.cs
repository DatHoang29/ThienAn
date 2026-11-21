using Shared.Infrastructure.Persistence.SqlSugar;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.EmailSchedule;
using Modules.Mail.Core.Dto.Template;
using Modules.Mail.Infrastructure.Services.Mail.Delivery;

namespace Modules.Mail.Controllers.EmailSchedule;

/// <summary>
/// Author: HieuNV
/// Description: API quản lý lịch gửi báo cáo qua email (MailSchedule).
///              Mỗi bản ghi là một lịch chạy tự động: chọn báo cáo, tham số, tần suất,
///              người nhận. Việc kết xuất và gửi do job nền thực hiện.
/// Created date: 14/08/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 610)]
public class RptEmailScheduleController : BaseController
{
    private readonly RptEmailSendService _sendService;
    private readonly RptEmailPreviewService _previewService;

    public RptEmailScheduleController(
        RptEmailSendService sendService,
        RptEmailPreviewService previewService)
    {
        _sendService = sendService;
        _previewService = previewService;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy danh sách lịch gửi báo cáo theo phân trang
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    [DisplayName("Get MailSchedule page list")]
    public async Task<SqlSugarPagedList<RptPageEmailScheduleOutput>> Page(RptPageEmailScheduleInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<RptPageEmailScheduleOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy toàn bộ danh sách lịch gửi báo cáo (không phân trang)
    /// Created date: 14/08/2026
    /// </summary>
    [DisplayName("Get MailSchedule list")]
    public async Task<List<RptEmailScheduleOutput>> GetList([FromQuery] RptEmailScheduleInput input)
    {
        return await MessBus.InvokeAsync<List<RptEmailScheduleOutput>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Lấy chi tiết 1 lịch gửi báo cáo theo ID
    /// Created date: 14/08/2026
    /// </summary>
    [DisplayName("Get MailSchedule by id")]
    public async Task<RptEmailScheduleOutput> GetById([FromQuery] MailIdScheduleInput input)
    {
        return await MessBus.InvokeAsync<RptEmailScheduleOutput>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thêm mới 1 lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add MailSchedule")]
    public async Task AddRptEmailSchedule(MailAddScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Cập nhật 1 lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update MailSchedule")]
    public async Task UpdateRptEmailSchedule(MailUpdateScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Bật / tắt 1 lịch gửi báo cáo.
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "ToggleState"), HttpPost]
    [DisplayName("Toggle MailSchedule state")]
    public async Task ToggleRptEmailScheduleState(MailToggleScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa 1 lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete MailSchedule")]
    public async Task DeleteRptEmailSchedule(MailDeleteScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xóa nhiều lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Delete batch MailSchedule")]
    public async Task BatchDeleteRptEmailSchedule(List<MailDeleteScheduleInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Gửi ngay một lịch, không chờ tới hạn.
    /// Created date: 17/08/2026
    /// </summary>
    /// <returns>true nếu máy chủ SMTP đã nhận thư.</returns>
    [ApiDescriptionSettings(Name = "SendNow"), HttpPost]
    [DisplayName("Send MailSchedule now")]
    public async Task<bool> SendRptEmailScheduleNow(MailIdScheduleInput input)
    {
        return await _sendService.SendNowAsync(input.ID);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Danh mục tag hệ thống — kéo thả vào tiêu đề và nội dung thư (wangEditor)
    /// Created date: 29/09/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Tags"), HttpGet]
    [DisplayName("Get MailSchedule tags")]
    public List<MailTagInfo> GetTags()
    {
        return MailTag.All.ToList();
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Gợi ý địa chỉ email từ sổ địa chỉ (MailAddress) cho ô To / CC / BCC và ô lọc người nhận.
    /// Created date: 05/10/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Addresses"), HttpGet]
    [DisplayName("Get MailSchedule address suggestions")]
    public async Task<List<string>> GetAddresses([FromQuery] MailAddressInput input)
    {
        return await MessBus.InvokeAsync<List<string>>(input);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thư mẫu dựng bằng dữ liệu giả — ảnh thu nhỏ ở bước chọn mẫu.
    /// Created date: 29/09/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Sample"), HttpGet]
    [DisplayName("Get MailSchedule template sample")]
    public async Task<MailPreviewOutput> GetSample([FromQuery] MailSampleTemplateInput input)
    {
        return await _previewService.SampleAsync(input.Code);
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Xem trước thư theo form lịch đang soạn. Không kết xuất báo cáo, không gửi.
    /// Created date: 29/09/2026
    /// </summary>
    [ApiDescriptionSettings(Name = "Preview"), HttpPost]
    [DisplayName("Preview MailSchedule mail")]
    public async Task<MailPreviewOutput> Preview(MailPreviewScheduleInput input)
    {
        return await _previewService.PreviewAsync(input);
    }

    /// <summary>
    /// Upload ảnh/logo để chèn vào nội dung lịch hoặc mẫu email.
    /// Không cần ID lịch; FE chèn Url trả về vào HTML trước khi lưu.
    /// </summary>
    [ApiDescriptionSettings(Name = "UploadImage"), HttpPost]
    [Consumes("multipart/form-data")]
    [DisplayName("Upload MailSchedule image")]
    public async Task<MailUploadImageOutput> UploadImage([FromForm] MailUploadImageInput input)
    {
        return await MessBus.InvokeAsync<MailUploadImageOutput>(input);
    }
}
