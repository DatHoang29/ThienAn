
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.Equipment.Dto;

/// <summary>
/// DTO List Equipment
/// </summary>
public class EquipmentOutput : TmsEquipment
{

}

/// <summary>
/// DTO Page Equipment
/// </summary>
public class PageEquipmentOutput : TmsEquipment
{
}

/// <summary>
/// Author: ThangNN
/// Description: DTO chứa thông tin mở rộng của equipment
/// Created Date: 07/10/2024
/// </summary>
public class TmsEquipmentDataOutput : TmsEquipment
{
    public string? EquimentTypeName { get; set; }
    public string? LaneName { get; set; }
    public string? DirectionName { get; set; }
    public string? ActiveDirectionName { get; set; }
    public string? LocationName { get; set; }
    public string? EquipmentStateName { get; set; }
    public bool? IsRecommended { get; set; }
    public string? ZoneId { get; set; }
    public string? ZoneName { get; set; }

    /// <summary>
    /// Khoảng cách (mét) từ thiết bị tới vị trí sự cố, luôn là số dương.
    /// Chỉ được gán ở luồng gợi ý thiết bị (ListEquipmentSuggestion).
    /// Dùng để: sắp xếp gần -> xa, và cắt danh sách theo MaxTargetCount ở luồng tự động.
    /// </summary>
    public int? DistanceMeters { get; set; }

    /// <summary>
    /// Loại xử lý (Before/After) mà thiết bị này THỎA khi đối chiếu TmsEquipmentEventRule.
    /// null = thiết bị không thỏa cấu hình trước/sau nào -> không được tích sẵn.
    /// Trả về để FE hiển thị nhãn "Trước/Sau" và để dễ chẩn đoán khi gợi ý ra kết quả bất ngờ.
    /// </summary>
    public string? ProcessTypeMatched { get; set; }
}