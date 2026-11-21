namespace Modules.VDS.Controllers.Event.Dto;

/// <summary>
/// Author: KienLT
/// Description: Khóa kép tra chi tiết một bản tin sự kiện VDS.
///
/// <para>QuestDB không có ID tự sinh: một bản tin được định danh bằng CẶP <c>vendorEventId</c> +
/// <c>detectTime</c>, nên chi tiết phải nhận đủ hai tham số.</para>
/// <para>DTO này KHÔNG lộ ra swagger: action <c>Detail</c> giữ hai tham số <c>eventId</c>/<c>detectTime</c>
/// trên query string để contract HTTP không đổi, controller tự dựng DTO rồi dispatch qua message bus.</para>
/// <para><b>Không còn <c>[Required]</c> trên hai trường này (bỏ 23/09/2026).</b> Chúng là value type không
/// nullable nên LUÔN có giá trị: thiếu <c>eventId</c> chỉ ra <c>0</c>, thiếu <c>detectTime</c> ra
/// <c>0001-01-01</c>, và attribute không có gì để chặn. Giữ nó lại chỉ làm người đọc tưởng đã có lớp bảo vệ.
/// Miền hợp lệ thật do <c>VdsEventKeyRules</c> định nghĩa và <c>VdsRecordReaderService</c> kiểm — phủ cả đường HTTP
/// và đường message bus.</para>
/// Created date: 21/09/2026
/// </summary>
public class EventDetailInput
{
    public long EventId { get; set; }
    public DateTime DetectTime { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Bộ lọc snapshot sự kiện VDS đang hiển thị (SSE <c>api/vds/vdsEvent/Stream</c>).
/// Created date: 19/10/2026
/// </summary>
public class EventSnapshotInput
{
    public string? ZoneId { get; set; }
    public string? EquipmentId { get; set; }
    public string? Direction { get; set; }
}
