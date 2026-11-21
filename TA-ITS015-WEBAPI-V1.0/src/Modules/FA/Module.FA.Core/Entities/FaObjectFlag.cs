using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.FA.Core.Entities;

/// <summary>
/// Cờ trạng thái hệ thống báo cháy (ví dụ IsAcPowerFailed).
/// Bảng cũ: FA_OBJECT_FLAG
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
[SugarTable("FaObjectFlag")]
public class FaObjectFlag : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Tên cờ trạng thái. Cũ: ObjectKey")]
    public string? FlagKey { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Giá trị. Cũ: ObjectValue")]
    public string? FlagValue { get; set; }
}
