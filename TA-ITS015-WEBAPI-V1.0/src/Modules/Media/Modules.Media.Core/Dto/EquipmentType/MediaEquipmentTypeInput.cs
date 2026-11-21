namespace Modules.Media.Core.Dto.EquipmentType;

/// <summary>
/// Description: Tham số chung lấy icon loại thiết bị.
/// Created date: 08/10/2026
/// </summary>
public class MediaEquipmentTypeInput
{
    /// <summary>TmsEquipmentType.ID</summary>
    public string ID { get; set; } = string.Empty;

    /// <summary>Default | Active | Inactive | Fault (bỏ trống = Default).</summary>
    public string? Field { get; set; }
}
