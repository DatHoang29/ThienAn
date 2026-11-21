using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.FA.Core.Entities;

/// <summary>
/// Trạng thái điểm báo cháy (đầu báo, nút ấn, chuông, bơm, van, quạt...) cho service FA.
/// Bảng cũ: FA_OBJECT_DATA
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
[SugarTable("FaObjectData")]
public class FaObjectData : EntityTenant
{
    [SugarColumn(IsNullable = true, ColumnDescription = "Địa chỉ điểm (BACnet/Modbus). Cũ: Id")]
    public int? PointAddress { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Loại đối tượng. Code service cũ chỉ dùng 1 = đầu báo nhiệt, 2 = nút ấn (ngược comment DB cũ, cần xác nhận). Cũ: TypeObject")]
    public int? TypeObject { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Tên thiết bị. Cũ: Description")]
    public string? Description { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Lý trình Km")]
    public int? KmNumber { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Lý trình mét")]
    public int? MetNumber { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Mã vị trí tuyến/hầm (SysConfigData nhóm tms_zone_location), cùng kiểu TmsZone.Location. Cũ: Tunnel (số)")]
    public string? Location { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Số làn")]
    public int? Lane { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Đang kích hoạt báo động. Cũ: IsActived")]
    public bool? IsActive { get; set; }
}
