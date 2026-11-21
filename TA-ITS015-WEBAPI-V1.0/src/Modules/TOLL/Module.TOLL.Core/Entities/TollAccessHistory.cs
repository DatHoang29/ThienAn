using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using System;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Lịch sử truy cập ứng dụng của nhân viên thu phí (đồng bộ từ TCS,
/// API appaccesslog/accesshistorytcs).
/// TCS không trả về ID định danh nên khóa nghiệp vụ để chống trùng là bộ
/// (EmployeeCode + AccessTime + Status).
/// Author: ChuongLP
/// Created date: 09/09/2026
/// </summary>
[SugarTable("TollAccessHistory")]
public class TollAccessHistory : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã nhân viên / tài khoản truy cập")]
    public string? EmployeeCode { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Tên nhân viên")]
    public string? EmployeeName { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Thời gian truy cập")]
    public DateTime? AccessTime { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Trạng thái truy cập (vd: Login/Logout)")]
    public string? Status { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm ITS đồng bộ bản ghi")]
    public DateTime? LastSyncTime { get; set; }
}
