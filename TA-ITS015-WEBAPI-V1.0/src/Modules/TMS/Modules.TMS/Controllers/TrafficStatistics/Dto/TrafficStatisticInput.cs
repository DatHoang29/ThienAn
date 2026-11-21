using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.TrafficStatistics.Dto;

public class PagetmsTrafficStatisticInput : BasePageInput
{
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public float? AvgSpeed { get; set; }
    public DateTime? FromTime { get; set; }
    public DateTime? ToTime { get; set; }
    public string? Lane { get; set; }
    public string? Location { get; set; }
    public string? EquipmentId { get; set; }
    public int? SVehicleNumber { get; set; }
    public int? MVehicleNumber { get; set; }
    public int? LVehicleNumber { get; set; }
    public int? OVehicleNumber { get; set; }


}

public class TrafficStatisticInput
{
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public float? AvgSpeed { get; set; }
    public DateTime? FromTime { get; set; }
    public DateTime? ToTime { get; set; }
    public string? Lane { get; set; }
    public string? Location { get; set; }
    public string? EquipmentId { get; set; }
    public int? SVehicleNumber { get; set; }
    public int? MVehicleNumber { get; set; }
    public int? LVehicleNumber { get; set; }
    public int? OVehicleNumber { get; set; }


}

public class TrafficStatisticByEquipmentInput
{
    public string? EquipmentId { get; set; }
}

public class AddTmsTrafficStatisticInput : TmsTrafficStatistic
{
}

public class UpdateTmsTrafficStatisticInput : AddTmsTrafficStatisticInput
{

}

public class DeleteTmsTrafficStatisticInput : BaseIdInput
{

}

/// <summary>
/// Author: Ngọc Thắng
/// Created Date: 24/02/2025
/// </summary>
public class TrafficStatisticByDateInput
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}