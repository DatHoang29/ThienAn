using System.Collections.Generic;

namespace Modules.TMS.Core.Dto.Equipment;

/// <summary>
/// Author: ChuongLP
/// Description: Message dùng qua MessBus (Wolverine) để lấy danh sách thiết bị theo danh sách ID.
///              Dùng cho kiểm tra thiết bị có tồn tại hay không giữa các module (thay cho BaseApiClient).
/// Created date: 05/08/2026
/// </summary>
public class GetEquipmentByIdsInput
{
    public List<string> EquipmentIds { get; set; } = new();
}

/// <summary>
/// Author: ChuongLP
/// Description: Message dùng qua MessBus (Wolverine) để lấy danh sách thiết bị theo mã loại thiết bị
///              (ví dụ vms, lcs). Dùng cho luồng lấy trạng thái thiết bị của phân hệ VMS.
///              Chỉ trả về thiết bị đang Enable và chưa bị xóa.
/// Created date: 2026-08-20
/// </summary>
public class GetEquipmentByTypeCodesInput
{
    public List<string> TypeCodes { get; set; } = new();
}

/// <summary>
/// Author: ChuongLP
/// Description: Message dùng qua MessBus (Wolverine) để lấy TOÀN BỘ thiết bị (Enable, chưa xóa).
///              Dùng cho luồng enrich vị trí (Km/Met) của cây topology - thay cho BaseApiClient.
/// Created date: 2026-09-11
/// </summary>
public class GetAllEquipmentInput
{
}
