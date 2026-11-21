using System;
using System.Collections.Generic;
using Modules.Integration.TrafficLight.Base.Enums;

namespace ITS.TrafficLight.Core.Messaging;

/// <summary>
/// Các lệnh điều khiển hỗ trợ trên kênh ta.its.control.trafficlight.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public static class TrafficLightControlAction
{
    public const string SetColor = "set-color";
    public const string GetStatus = "get-status";
    public const string CheckConnect = "check-connect";
}

public static class TrafficLightControlType
{
    public const string Request = "request";
    public const string Result = "result";
}

/// <summary>
/// Gói tin yêu cầu điều khiển Đèn tín hiệu giao thông từ BE gửi xuống.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class TrafficLightControlRequest
{
    public string Type { get; set; } = TrafficLightControlType.Request;
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public string Action { get; set; } = TrafficLightControlAction.SetColor;
    public TrafficLightColor Color { get; set; } = TrafficLightColor.Red;
    public bool Simulate { get; set; }
    public List<TrafficLightControlTarget> Targets { get; set; } = new();
    public DateTime RequestTime { get; set; } = DateTime.Now;
}

/// <summary>
/// Cấu hình kết nối của 1 đèn tín hiệu trong gói tin.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class TrafficLightControlTarget
{
    public string EquipmentId { get; set; } = string.Empty;
    public string EquipmentCode { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; } = 102;
    public short Rack { get; set; } = 0;
    public short Slot { get; set; } = 0;
    public TrafficLightVendor Vendor { get; set; } = TrafficLightVendor.SiemensS71200;
    public int DbNumber { get; set; } = 3;
    public int TimeOn { get; set; } = 3;
    public int TimeOff { get; set; } = 1;

    // Modbus outputs
    public int RedOutput { get; set; } = 1;
    public int YellowOutput { get; set; } = 2;
    public int GreenOutput { get; set; } = 3;
    public int FmOutput { get; set; } = 4;

    public int TimeoutMs { get; set; } = 5000;
    public bool PingBeforeConnect { get; set; } = true;
}

/// <summary>
/// Gói tin phản hồi kết quả điều khiển Đèn tín hiệu gửi lên BE.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class TrafficLightControlResult
{
    public string Type { get; set; } = TrafficLightControlType.Result;
    public string RequestId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TrafficLightTargetResult> Results { get; set; } = new();
    public DateTime ResponseTime { get; set; } = DateTime.Now;
    public DateTime FinishedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Kết quả chi tiết của từng đèn tín hiệu.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class TrafficLightTargetResult
{
    public string EquipmentId { get; set; } = string.Empty;
    public string EquipmentCode { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool? PowerStatus { get; set; }
    public string RawDetail { get; set; } = string.Empty;
}
