using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Thông tin trạm thu phí
/// Author: ChuongLP
/// Created date: 23/07/2026
/// </summary>
[SugarTable("TollStation")]
public class TollStation : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
    public string? StationId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
    public string? Name { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength)]
    public string? Note { get; set; }
    [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength)]
    public string? Remark { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
    public string? Company { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Loại trạm thu phí")]
    public int? StationType { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Mã trạm theo BE ETC>")]
    public string? StationBE { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "Hệ xử lý của ETC")]
    public string? Type { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Kích hoạt/ vô hiệu")]
    public int? Active { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// ID đơn vị TMS được gán cho trạm (TmsWorkUnit.ID, chỉ đơn vị có Type = "toll").
    /// Do người dùng map trên ITS; đồng bộ TCS không ghi đè cột này.
    /// Author: HieuNV
    /// Created date: 07/10/2026
    /// </summary>
    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "ID đơn vị TMS (TmsWorkUnit.ID, Type = toll)")]
    public string? RefId { get; set; }
}
