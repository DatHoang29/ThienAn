using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Danh mục dùng chung của ETC (gộp loại xe, loại vé, loại giá, lý do,
/// hình thức thanh toán, trạng thái, trạng thái giao dịch) - phân biệt bằng DataType.
/// Nguồn ETC: Sys_VehicleType, Sys_TicketType, Sys_PriceType, Sys_Reason,
///            Sys_Payment, Sys_Status, Sys_TransactionStatus
/// Author: ChuongLP
/// Created date: 23/07/2026
/// </summary>
[SugarTable("TollMasterData")]
public class TollMasterData : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "Nhóm danh mục: VehicleType/TicketType/PriceType/Reason/Payment/Status/TransactionStatus")]
    public string? DataType { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã gốc theo BE ETC (ID bản ghi danh mục)")]
    public string? SourceId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Tên hiển thị")]
    public string? Name { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Nhóm phân loại theo ETC (Group/Type)")]
    public string? GroupCode { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Mô tả")]
    public string? Remark { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Kích hoạt/ vô hiệu")]
    public int? Active { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Payload gốc từ API ETC (dự phòng truy vết)")]
    public string? RawJson { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm ITS đồng bộ bản ghi")]
    public DateTime? LastSyncTime { get; set; }
}
