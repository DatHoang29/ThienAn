namespace Modules.Integration.TrafficLight.Base.Enums;

/// <summary>
/// Trạng thái màu đèn tín hiệu giao thông.
/// Kế thừa và chuẩn hóa từ hệ thống CLVH:
/// - OFF = 0: Tắt toàn bộ đèn
/// - RED = 1: Bật đèn Đỏ sáng liên tục
/// - GREEN = 2: Bật đèn Xanh sáng liên tục
/// - YELLOW = 3: Bật đèn Vàng sáng liên tục
/// - BLINK_RED = 4: Chớp đèn Đỏ theo chu kỳ On/Off
/// - BLINK_GREEN = 8: Chớp đèn Xanh theo chu kỳ On/Off
/// - BLINK_YELLOW = 16: Chớp đèn Vàng theo chu kỳ On/Off
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public enum TrafficLightColor
{
    Off = 0,
    Red = 1,
    Green = 2,
    Yellow = 3,
    BlinkRed = 4,
    BlinkGreen = 8,
    BlinkYellow = 16
}

/// <summary>
/// Hãng / Giao thức điều khiển Đèn tín hiệu giao thông.
/// </summary>
public enum TrafficLightVendor
{
    /// <summary>PLC Siemens S7-1200 (điều khiển qua DB3.DBB0 / DBB1,2,5,6).</summary>
    SiemensS71200 = 1,

    /// <summary>Bộ điều khiển Modbus TCP (EasyModbus / NModbus Coils: Red, Yellow, FM).</summary>
    ModbusTcp = 2
}
