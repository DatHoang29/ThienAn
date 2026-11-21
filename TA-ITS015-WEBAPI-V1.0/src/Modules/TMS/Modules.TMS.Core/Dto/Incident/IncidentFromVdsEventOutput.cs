namespace Modules.TMS.Core.Dto.Incident;

/// <summary>
/// Author: KienLT
/// Description: Kết quả tạo sự cố từ bản tin VDS. Ba trường hợp:
///              <list type="bullet">
///              <item><c>Created</c>: vừa tạo mới.</item>
///              <item><c>AlreadyExists</c>: bản tin đã có sự cố (cột EventId duy nhất) ⇒ trả sự cố đó để FE mở.</item>
///              <item><c>ConflictIncidentId</c> có giá trị: đang có sự cố mở cùng loại, cùng vị trí ⇒ CHƯA tạo,
///              FE hỏi lại người vận hành rồi gửi lại với <c>Force = true</c>.</item>
///              </list>
/// Created date: 23/10/2026
/// </summary>
public class IncidentFromVdsEventOutput
{
    public string? IncidentId { get; set; }

    public string? Code { get; set; }

    public DateTime? StartDate { get; set; }

    public bool Created { get; set; }

    public bool AlreadyExists { get; set; }

    /// <summary>Trạng thái sự cố sau khi tạo (chuỗi số TmsIncidentStateEnum).</summary>
    public string? State { get; set; }

    /// <summary>Hình thức xử lý: đã lập lịch kịch bản tự động (đếm ngược) cho sự cố.</summary>
    public bool AutomationScheduled { get; set; }

    public string? ConflictIncidentId { get; set; }

    public string? ConflictCode { get; set; }

    public string? ConflictName { get; set; }

    public DateTime? ConflictStartDate { get; set; }
}
