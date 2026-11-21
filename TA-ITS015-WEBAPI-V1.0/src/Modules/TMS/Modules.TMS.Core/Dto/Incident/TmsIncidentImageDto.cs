using System.ComponentModel.DataAnnotations;

namespace Modules.TMS.Core.Dto.Incident;

/// <summary>Đầu vào API đọc ảnh đã chụp của một sự cố.</summary>
public sealed class TmsIncidentImageInput
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$")]
    public string ID { get; set; } = string.Empty;

    /// <summary>1: đăng ký; 2: xác nhận; 3: xử lý; 4: kết thúc/bỏ qua/hủy.</summary>
    [Range(1, 4)]
    public int PictureType { get; set; }
}

/// <summary>Ảnh camera tương ứng với một mốc trạng thái của sự cố.</summary>
public sealed class TmsIncidentImageOutput
{
    public string IncidentId { get; set; } = string.Empty;
    public string EquipmentId { get; set; } = string.Empty;
    public int PictureType { get; set; }
    public DateTime CaptureTime { get; set; }
    public bool Exists { get; set; }
    public string Base64 { get; set; } = string.Empty;
    /// <summary>Luôn rỗng từ 14/10/2026: ảnh nằm trên share (không phải file tĩnh của site), dùng <see cref="Base64"/>. Giữ trường để không đổi hợp đồng API.</summary>
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public string ImageSource { get; set; } = "cctv";
    public string? EquipmentName { get; set; }
    /// <summary>Vị trí camera đã chụp ảnh (camera VDS với ảnh VDS).</summary>
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public DateTime? ImageCapturedAt { get; set; }
    public string Status { get; set; } = "Ready";
    public DateTime? WaitUntilUtc { get; set; }
}
