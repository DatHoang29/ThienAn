using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.FA.Core.Entities;

/// <summary>
/// Cấu hình điểm theo bản đồ thanh ghi Modbus. Service FA đọc TOÀN BỘ dòng bảng này
/// (stored FaObjectData_GetDoor không lọc) và coi mỗi dòng là một cửa thoát hiểm:
/// quét ModbusAddress, ghi IsAlarm + LastAlarmTime (UTC) khi trạng thái đổi.
/// Bảng cũ: FIRE_ALARM_CONFIG
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
[SugarTable("FaFireAlarmConfig")]
public class FaFireAlarmConfig : EntityTenant
{
    [SugarColumn(IsNullable = true, ColumnDescription = "ID tủ báo cháy. Cũ: PANEL_ID")]
    public int? PanelId { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "ID vùng báo cháy. Cũ: ZONE_ALARM_ID")]
    public int? ZoneAlarmId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Mã vị trí tuyến/hầm (SysConfigData nhóm tms_zone_location), cùng kiểu TmsZone.Location. Cũ: TUNNEL (số)")]
    public string? Location { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "Số loop. Cũ: LOOP")]
    public string? LoopNo { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Địa chỉ thiết bị trên tủ báo cháy. Cũ: FIRE_ALARM_ADDRESS")]
    public int? FireAlarmAddress { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Địa chỉ thanh ghi Modbus. Cũ: MODBUS_ADDRESS")]
    public int? ModbusAddress { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Tên thiết bị. Cũ: NAME")]
    public string? Name { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Mô tả. Cũ: DESCRIPTION")]
    public string? Description { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Loại thiết bị. Cũ: DEVICE_TYPE")]
    public string? DeviceType { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Lý trình Km. Cũ: NUMBER_KM")]
    public int? KmNumber { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Lý trình mét. Cũ: NUMBER_MET")]
    public int? MetNumber { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Vị trí khác. Cũ: LOCATION_OTHER")]
    public string? LocationOther { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Trạng thái (0/null: lỗi). Cũ: STATUS")]
    public int? Status { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Bit báo cháy. Cũ: ALARM_BIT")]
    public int? AlarmBit { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Bit lỗi. Cũ: FAULT_BIT")]
    public int? FaultBit { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Bit cách ly. Cũ: ISOLATED_BIT")]
    public int? IsolatedBit { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Bit tiền báo động. Cũ: PREALARM_BIT")]
    public int? PrealarmBit { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Đang báo cháy. Cũ: IS_ALARM")]
    public bool? IsAlarm { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm báo cháy gần nhất. Cũ: LAST_ALARM_TIMESTAMP")]
    public DateTime? LastAlarmTime { get; set; }
}
