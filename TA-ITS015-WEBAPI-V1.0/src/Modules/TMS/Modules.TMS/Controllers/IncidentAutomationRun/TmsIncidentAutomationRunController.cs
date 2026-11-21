using Modules.TMS.Controllers.IncidentAutomationRun.Dto;
using Modules.TMS.Core.Dto.IncidentAutomationRun;

namespace Modules.TMS.Controllers.IncidentAutomationRun;

/// <summary>
/// Author: SonTH
/// Description: TMS Incident Automation Run Controller
///              Theo dõi/điều khiển lần chạy tự động xử lý sự kiện/sự cố (countdown, chạy ngay, hủy).
///              Từ 13/10/2026 (KienLT) đi qua CQRS/Wolverine như các controller khác: ghi ở
///              <c>Commands/IncidentAutomationRunCommandHandler</c>, đọc ở <c>Queries/IncidentAutomationRunQueryHandler</c>.
/// Created date: 24/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 457)]
public class TmsIncidentAutomationRunController : BaseController
{
    /// <summary>
    /// Author: KienLT
    /// Description: Lập lịch tự động cho một sự cố ĐÃ TỒN TẠI — đường vào cho hệ thống ngoài tự ghi
    ///              <c>TmsIncident</c> (hiện là service VDS) rồi nhờ TMS xử lý tiếp.
    ///
    ///              KHÔNG ném lỗi khi không lập lịch được: trả về <c>Scheduled = false</c> kèm
    ///              <c>ReasonCode</c> để bên gọi biết vì sao và KHÔNG thử lại vô ích. Chỉ những lý do
    ///              thực sự tạm thời (<c>UnexpectedError</c>, <c>InsertFailed</c>) mới đáng gọi lại.
    ///
    ///              Phát bản tin realtime khi đã lập lịch để banner cảnh báo hiện ngay — sự cố do hệ
    ///              thống ngoài ghi trực tiếp vào DB không đi qua <c>IncidentBroadcaster</c> nên nếu
    ///              không phát ở đây thì người trực phải chờ nạp lại danh sách mới thấy.
    /// Created date: 12/08/2026
    /// </summary>
    [HttpPost("ScheduleForIncident")]
    [DisplayName("Schedule automation for an existing incident")]
    public async Task<ScheduleIncidentAutomationOutput> ScheduleForIncident(ScheduleIncidentAutomationInput input)
    {
        // Body rỗng (input null) vẫn phải ra output Scheduled=false như trước, nên không dispatch null.
        return await MessBus.InvokeAsync<ScheduleIncidentAutomationOutput>(input ?? new ScheduleIncidentAutomationInput());
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Lấy run gần nhất theo Incident (để FE hiển thị countdown/trạng thái).
    /// </summary>
    [HttpPost("GetLatestByIncident")]
    [DisplayName("Get latest automation run by incident")]
    public async Task<IncidentAutomationRunOutput?> GetLatestByIncident(GetIncidentAutomationRunByIncidentInput input)
    {
        return await MessBus.InvokeAsync<IncidentAutomationRunOutput?>(input);
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Bỏ qua countdown còn lại, chạy ngay lần tự động này.
    /// </summary>
    [HttpPost("ExecuteNow")]
    [DisplayName("Execute automation run now")]
    public async Task<bool> ExecuteNow(ExecuteIncidentAutomationRunNowInput input)
    {
        return await MessBus.InvokeAsync<bool>(input);
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Hủy lần tự động (đang cảnh báo hoặc đang chờ auto-finish).
    /// </summary>
    [HttpPost("Cancel")]
    [DisplayName("Cancel automation run")]
    public async Task<bool> Cancel(CancelIncidentAutomationRunInput input)
    {
        return await MessBus.InvokeAsync<bool>(input);
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Danh sách run đang chờ thực thi / chờ tự kết thúc (kèm thông tin sự cố),
    ///              dùng cho banner cảnh báo hiển thị trên mọi màn hình.
    /// </summary>
    [HttpPost("GetActiveList")]
    [DisplayName("Get active automation run list")]
    public async Task<List<IncidentAutomationRunViewOutput>> GetActiveList()
    {
        return await MessBus.InvokeAsync<List<IncidentAutomationRunViewOutput>>(new GetActiveIncidentAutomationRunListInput());
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Chuyển sự cố sang xử lý thủ công: hủy lần tự động nhưng giữ nguyên trạng thái
    ///              hiện tại của sự cố để người vận hành tiếp tục theo luồng thủ công.
    /// </summary>
    [HttpPost("ConvertToManual")]
    [DisplayName("Convert automation run to manual handling")]
    public async Task<bool> ConvertToManual(CancelIncidentAutomationRunInput input)
    {
        // Request giữ shape CancelIncidentAutomationRunInput; dựng message riêng vì Cancel đã giữ handler của kiểu đó.
        return await MessBus.InvokeAsync<bool>(new ConvertIncidentAutomationRunToManualInput
        {
            RunId = input?.RunId,
            Reason = input?.Reason
        });
    }
}
