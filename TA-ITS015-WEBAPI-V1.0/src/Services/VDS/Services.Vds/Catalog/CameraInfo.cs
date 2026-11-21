namespace Services.Vds.Catalog;

/// <summary>
/// Author: KienLT
/// Description: Thông tin định vị một camera VDS — nguồn gốc là <c>TmsEquipment</c> trên SQL Server.
///
/// Mọi bản tin VDS chỉ mang <c>camera_ip</c>; toàn bộ phần còn lại (thiết bị nào, vùng nào, Km bao nhiêu,
/// hướng nào) phải tra từ đây. Vì vậy camera không có trong danh mục thì bản tin bị bỏ ở cửa vào —
/// không có cách nào biết nó ở đâu để mà ghi.
///
/// <c>record</c> nên bất biến: đã vào cache thì không ai sửa được tại chỗ.
/// Created date: 24/08/2026
/// </summary>
/// <param name="CameraIp">Khoá nhận diện. Đây là thứ DUY NHẤT bản tin VDS mang theo.</param>
/// <param name="EquipmentId"><c>TmsEquipment.ID</c> — ghi vào mọi bản ghi sinh từ camera này.</param>
/// <param name="Km">Số Km trên tuyến.</param>
/// <param name="Met">Số mét bổ sung (Km + Met = vị trí chính xác).</param>
/// <param name="Direction">
/// Hướng tuyến (<c>TmsEquipment.DirectionId</c>). Dùng để đồng bộ ngưỡng tốc độ lên AID.
/// Rule Speeding API v1.8 tin phân loại camera/làn của AID kể cả khi hướng chưa có ngưỡng.
/// </param>
/// <param name="Lane">Làn của THIẾT BỊ. Làn của từng lượt xe lấy từ bản tin, không lấy từ đây.</param>
/// <param name="Location">Vị trí trên tuyến (tuyến chính / tuyến phụ / hầm…), dùng cho sự cố.</param>
/// <param name="Code">Mã thiết bị (<c>VDS-01</c>…) — để đọc log cho người.</param>
public sealed record CameraInfo(
    string CameraIp,
    string EquipmentId,
    string? Name = null,
    int? Km = null,
    int? Met = null,
    string? Direction = null,
    string? Lane = null,
    string? Location = null,
    string? Code = null)
{
    /// <summary>Nhãn ngắn cho log: <c>VDS-01(192.168.1.101)</c>.</summary>
    public string Label => string.IsNullOrWhiteSpace(Code) ? CameraIp : $"{Code}({CameraIp})";
}
