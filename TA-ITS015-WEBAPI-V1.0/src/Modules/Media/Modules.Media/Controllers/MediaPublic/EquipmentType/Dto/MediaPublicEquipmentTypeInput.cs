using Modules.Media.Core.Dto.EquipmentType;

namespace Modules.Media.Controllers.MediaPublic.EquipmentType.Dto;

/// <summary>
/// Description: [WP] Tham số EquipmentType của MediaPublic.
/// Created date: 08/10/2026
/// </summary>
public class MediaPublicEquipmentTypeInput : MediaEquipmentTypeInput
{
    /// <summary>Phiên bản FE truyền (updateTime) chỉ để phá cache trình duyệt, BE không dùng.</summary>
    public string? V { get; set; }
}
