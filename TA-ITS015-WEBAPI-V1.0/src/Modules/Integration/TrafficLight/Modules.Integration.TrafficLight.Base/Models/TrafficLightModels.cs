using Modules.Integration.TrafficLight.Base.Enums;

namespace Modules.Integration.TrafficLight.Base.Models;

/// <summary>
/// Tham số cấu hình thiết bị Đèn tín hiệu giao thông.
/// Tương thích với cả Siemens S7-1200 và Modbus TCP.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class TrafficLightConfig
{
    public string Code { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 102; // S7-1200: 102, Modbus: 502
    public short Rack { get; set; } = 0;
    public short Slot { get; set; } = 0;

    /// <summary>DB number dùng cho Siemens S7-1200 (CLVH dùng DB3).</summary>
    public int DbNumber { get; set; } = 3;

    /// <summary>Thời gian đèn sáng trong chu kỳ chớp (giây).</summary>
    public int TimeOn { get; set; } = 3;

    /// <summary>Thời gian đèn tắt trong chu kỳ chớp (giây).</summary>
    public int TimeOff { get; set; } = 1;

    // Cấu hình ngõ ra cho Modbus TCP (1-based index như CLVH)
    public int RedOutput { get; set; } = 1;
    public int YellowOutput { get; set; } = 2;
    public int GreenOutput { get; set; } = 3;
    public int FmOutput { get; set; } = 4;

    public int TimeoutMs { get; set; } = 5000;
    public bool PingBeforeConnect { get; set; } = true;
}

/// <summary>
/// Kết quả điều khiển Đèn tín hiệu.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class TrafficLightResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public TrafficLightColor Color { get; set; } = TrafficLightColor.Off;
}

/// <summary>
/// Trạng thái đọc được từ Đèn tín hiệu.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class TrafficLightStatusResult
{
    public bool IsConnected { get; set; }
    public TrafficLightColor CurrentColor { get; set; } = TrafficLightColor.Off;
    public bool? PowerStatus { get; set; } // Trạng thái nguồn / CB điện nếu có
    public string RawDetail { get; set; } = string.Empty;
}
