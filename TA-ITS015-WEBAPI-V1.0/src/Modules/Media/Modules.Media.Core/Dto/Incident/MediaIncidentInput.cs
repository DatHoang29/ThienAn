namespace Modules.Media.Core.Dto.Incident;

/// <summary>
/// Description: Tham số chung lấy ảnh camera của sự cố tại một mốc trạng thái.
/// Created date: 08/10/2026
/// </summary>
public class MediaIncidentInput
{
    /// <summary>TmsIncident.ID</summary>
    public string ID { get; set; } = string.Empty;

    /// <summary>1: đăng ký; 2: xác nhận; 3: xử lý; 4: kết thúc/bỏ qua/hủy.</summary>
    public int PictureType { get; set; }
}
