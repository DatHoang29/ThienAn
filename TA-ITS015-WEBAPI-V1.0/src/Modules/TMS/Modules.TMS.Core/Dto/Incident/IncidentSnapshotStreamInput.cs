namespace Modules.TMS.Core.Dto.Incident;

/// <summary>
/// Author: KienLT
/// Description: Message MỞ luồng SSE danh sách sự cố: kiểm bộ lọc và tính khoá đệm theo người gọi.
///
/// <para>Controller await message này trong action, TRƯỚC khi trả <c>SseResult</c>: lỗi bộ lọc vẫn ra
/// response lỗi thường (chưa mở stream), và khoá đệm được tính từ <c>App.User</c> của request — đúng như
/// trước khi tách.</para>
/// Created date: 13/10/2026
/// </summary>
public class IncidentListStreamOpenInput
{
    /// <summary>Bộ lọc FE gửi lên (query string).</summary>
    public TmsIncidentMonitorInput Query { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Message MỞ luồng SSE icon sự cố: tính khoá đệm theo người gọi tại thời điểm mở stream.
/// Created date: 13/10/2026
/// </summary>
public class IncidentIconStreamOpenInput
{
}

/// <summary>
/// Author: KienLT
/// Description: Message lấy một snapshot danh sách sự cố cho vòng poll SSE.
/// Created date: 13/10/2026
/// </summary>
public class IncidentListSnapshotInput
{
    /// <summary>Khoá đệm từ <see cref="IncidentStreamOpenOutput.Key"/>.</summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Bộ lọc dạng JSON. Phải dựng LẠI input từ chuỗi này mỗi vòng poll vì handler danh sách SỬA input tại chỗ.
    /// </summary>
    public string QueryJson { get; set; } = "";
}

/// <summary>
/// Author: KienLT
/// Description: Message lấy một snapshot icon sự cố trên bản đồ cho vòng poll SSE.
/// Created date: 13/10/2026
/// </summary>
public class IncidentIconSnapshotInput
{
    /// <summary>Khoá đệm từ <see cref="IncidentStreamOpenOutput.Key"/>.</summary>
    public string Key { get; set; } = "";
}
