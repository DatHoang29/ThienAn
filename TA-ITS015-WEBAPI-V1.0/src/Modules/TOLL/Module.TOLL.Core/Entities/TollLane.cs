using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Thông tin làn thu phí
/// Author: ChuongLP
/// Created date: 23/07/2026
/// </summary>
[SugarTable("TollLane")]
public class TollLane : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
    public string? LaneId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
    public string? Name { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength)]
    public string? Note { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength)]
    public string? Remark { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
    public string? StationId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "Hướng vào ra của ETC")]
    public string? Direction { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "Mã làn theo BE")]
    public string? LaneBE { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? LastSyncTime { get; set; }
}
