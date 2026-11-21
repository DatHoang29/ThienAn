using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using System;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Thông tin ca làm việc thu phí (đồng bộ từ ETC).
/// Author: ChuongLP
/// Created date: 08/08/2026
/// </summary>
[SugarTable("TollShift")]
public class TollShift : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã ca làm việc")]
    public string? ShiftId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Tên ca làm việc")]
    public string? Name { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Thời gian vào ca")]
    public DateTime? StartTime { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Thời gian hết ca")]
    public DateTime? EndTime { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
    public string? Note { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã ca theo BE ETC")]
    public string? ShiftBE { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm ITS đồng bộ bản ghi")]
    public DateTime? LastSyncTime { get; set; }
}
