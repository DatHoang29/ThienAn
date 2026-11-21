using Modules.TMS.Core.Entities;

namespace Modules.TMS.Core.Dto.IncidentControl;

public class TmsIncidentControlOutput : TmsIncidentControl
{
}

/// <summary>
/// Bản ghi giữ thiết bị kèm thông tin sự cố và loại thiết bị (kết quả GetIncidentControlOccupancyInput).
/// State (kế thừa) là trạng thái giữ biển: override / append / null (gửi thẳng lần đầu).
/// Author: ChuongLP
/// Created date: 19/10/2026
/// </summary>
public class TmsIncidentControlDataOutput : TmsIncidentControl
{
    /// <summary> Trạng thái sự cố (TmsIncident.State) </summary>
    public string? IncidentState { get; set; }

    /// <summary> Loại sự kiện của sự cố (TmsIncident.EventTypeId) </summary>
    public string? EventTypeId { get; set; }

    /// <summary> Loại thiết bị (TmsEquipment.EquipmentTypeId) </summary>
    public string? EquipmentTypeId { get; set; }
}
