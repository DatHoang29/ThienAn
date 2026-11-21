namespace Modules.TMS.Core.Dto.Incident;

/// <summary>
/// Author: KienLT
/// Description: Kết quả mở luồng SSE sự cố: khoá đệm (đã gồm danh tính người gọi) và bộ lọc dạng JSON.
/// Created date: 13/10/2026
/// </summary>
public class IncidentStreamOpenOutput
{
    /// <summary>Khoá đệm của <c>IncidentSnapshotCache</c>, tính MỘT LẦN khi mở stream.</summary>
    public string Key { get; set; } = "";

    /// <summary>Bộ lọc đã serialize; chỉ có với luồng danh sách.</summary>
    public string? QueryJson { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Vỏ bọc snapshot SSE sự cố. Payload giữ NGUYÊN đối tượng ẩn danh của
/// <c>TmsIncidentStreamService</c> để frame gửi FE không đổi; vỏ chỉ để Wolverine có kiểu response cụ thể.
/// Created date: 13/10/2026
/// </summary>
public class IncidentSnapshotPayloadOutput
{
    public object Payload { get; set; } = default!;
}
