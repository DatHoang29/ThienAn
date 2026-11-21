namespace Modules.TMS.Controllers.VehicleFilterHistory.Dto;

/// <summary>
/// Author: KienLT
/// Description: Tham số đọc lịch sử đối chiếu biển số với danh sách lọc phương tiện.
///
/// <para><c>Field</c>/<c>Order</c> lấy từ <c>BasePageInput</c> thay vì tự khai —
/// trước 22/09/2026 lớp này khai lại hai trường đã có ở base.</para>
/// <para>Luật quy tên cột sắp xếp nằm ở <c>Infrastructure/Services/VehicleFilterHistory/Rules/TmsVehicleFilterHistorySortRules</c>.</para>
/// Created date: 16/09/2026
/// </summary>
public sealed class VehicleFilterHistoryWindowInput : Shared.Core.Utilities.BasePageInput
{
    public VehicleFilterHistoryWindowInput() { PageSize = 50; }
    [Required] public DateTime? FromDate { get; set; }
    [Required] public DateTime? ToDate { get; set; }
    public string? EquipmentId { get; set; }
    public string? ZoneId { get; set; }
    public string? Direction { get; set; }
    public string? LicensePlate { get; set; }
    public string? Type { get; set; }
}
