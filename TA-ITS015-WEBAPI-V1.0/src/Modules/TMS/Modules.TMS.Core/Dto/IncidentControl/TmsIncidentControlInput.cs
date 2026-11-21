using Modules.TMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.TMS.Core.Dto.IncidentControl;

public class TmsIncidentControlInput
{
    public string ID { get; set; }
    public string IncidentId { get; set; }
    public string State { get; set; }
}

public class AddTmsIncidentControlInput : TmsIncidentControl
{
}

public class UpdateTmsIncidentControlInput : AddTmsIncidentControlInput
{
}

public class DeleteTmsIncidentControlInput 
{
    public string IncidentId { get; set; }
    public string EquipmentId { get; set; }
}

public class GetIncidentControlInput
{
    public string IncidentId { get; set; }
}

public class GetIncidentControlByEquipmentInput
{
    public List<string> EquipmentIds { get; set; }
    public string ExcludeIncidentId { get; set; }   // bỏ qua chính sự cố đang thao tác

    /// <summary>
    /// true: chỉ lấy bản ghi gắn sự cố còn đang xử lý (bỏ dòng IncidentId null và sự cố đã Kết thúc/Hủy/Bỏ qua).
    /// Dùng để xác định thiết bị có thực sự đang phục vụ sự cố hay không (FE hiện nút Ghi đè/Nối tiếp, lập lịch thường).
    /// </summary>
    public bool OnlyActiveIncident { get; set; }
}

/// <summary>
/// Lấy thông tin sự cố đang giữ từng thiết bị (chỉ sự cố còn hoạt động) để quyết định Ghi đè / Nối tiếp / Chặn.
/// Khác GetIncidentControlByEquipmentInput: luôn bỏ dòng không gắn sự cố, sự cố đã xóa và sự cố đã kết thúc/hủy/bỏ qua,
/// và trả kèm thông tin sự cố (trạng thái, loại sự kiện) + loại thiết bị.
/// Author: ChuongLP
/// Created date: 19/10/2026
/// </summary>
public class GetIncidentControlOccupancyInput
{
    public List<string> EquipmentIds { get; set; }
    public string? ExcludeIncidentId { get; set; }   // bỏ qua chính sự cố đang thao tác
    public string? EquipmentTypeId { get; set; }     // lọc theo loại thiết bị (TmsEquipment.EquipmentTypeId), null = mọi loại
}

/// <summary>
/// Command nội bộ để automation gọi đúng luồng reset manual hiện hữu.
/// Không chứa business logic và không thay đổi contract của VmsProcessService.ResetIncidentToDefault.
/// </summary>
public class ResetIncidentToDefaultInput
{
    public string IncidentId { get; set; }
}

/// <summary>
/// Xoá toàn bộ bản ghi điều khiển sự cố theo IncidentId (bulk, idempotent).
/// Dùng khi reset/đóng sự cố để dọn dứt điểm, không phụ thuộc từng thiết bị.
/// </summary>
public class DeleteIncidentControlByIncidentInput
{
    public string IncidentId { get; set; }
}
