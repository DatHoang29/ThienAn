namespace Modules.TMS.Controllers.VehicleFilterHistory.Dto;

/// <summary>
/// Author: KienLT
/// Description: Tham số đọc lịch sử lọc xe theo CHUYẾN — cùng bộ lọc với <see cref="VehicleFilterHistoryWindowInput"/>
/// (cửa sổ UTC tối đa 60 ngày, biển, camera, vùng, hướng, loại). Lớp riêng, không kế thừa, để Wolverine không
/// khớp nhầm handler của lượt.
/// Created date: 23/10/2026
/// </summary>
public sealed class VehicleFilterHistoryTripInput : Shared.Core.Utilities.BasePageInput
{
    public VehicleFilterHistoryTripInput() { PageSize = 50; }
    [Required] public DateTime? FromDate { get; set; }
    [Required] public DateTime? ToDate { get; set; }
    public string? EquipmentId { get; set; }
    public string? ZoneId { get; set; }
    public string? Direction { get; set; }
    public string? LicensePlate { get; set; }
    public string? Type { get; set; }

    /// <summary>Cùng bộ lọc dưới dạng tham số đọc theo lượt (dùng chung luật cửa sổ và điều kiện lọc).</summary>
    public VehicleFilterHistoryWindowInput ToWindow() => new()
    {
        Page = Page,
        PageSize = PageSize,
        FromDate = FromDate,
        ToDate = ToDate,
        EquipmentId = EquipmentId,
        ZoneId = ZoneId,
        Direction = Direction,
        LicensePlate = LicensePlate,
        Type = Type,
    };
}
