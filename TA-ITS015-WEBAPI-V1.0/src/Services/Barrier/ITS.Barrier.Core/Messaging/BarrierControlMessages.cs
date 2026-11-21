using System;
using System.Collections.Generic;

namespace ITS.Barrier.Core.Messaging;

/// <summary>
/// Các lệnh điều khiển hỗ trợ trên kênh ta.its.control.barrier.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public static class BarrierControlAction
{
    /// <summary>Kích xung mở barie.</summary>
    public const string Open = "open";

    /// <summary>Kích xung đóng barie.</summary>
    public const string Close = "close";

    /// <summary>Đọc trạng thái hành trình và vòng từ.</summary>
    public const string GetStatus = "get-status";

    /// <summary>Kiểm tra ping và cổng S7.</summary>
    public const string CheckConnect = "check-connect";
}

/// <summary>
/// Loại gói tin trên kênh NATS 2 chiều.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public static class BarrierControlType
{
    public const string Request = "request";
    public const string Result = "result";
}

/// <summary>
/// Gói tin yêu cầu điều khiển Barrier từ BE hoặc công cụ gửi xuống.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class BarrierControlRequest
{
    public string Type { get; set; } = BarrierControlType.Request;
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public string Action { get; set; } = BarrierControlAction.Open;
    public bool Simulate { get; set; }
    public List<BarrierControlTarget> Targets { get; set; } = new();
    public DateTime RequestTime { get; set; } = DateTime.Now;
}

/// <summary>
/// Thông tin kết nối của 1 barrier được BE đóng gói gửi xuống.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class BarrierControlTarget
{
    public string EquipmentId { get; set; } = string.Empty;
    public string EquipmentCode { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; } = 102;
    public short Rack { get; set; } = 0;
    public short Slot { get; set; } = 0;
    public string OpenBit { get; set; } = "DB1.DBX0.1";
    public string CloseBit { get; set; } = "DB1.DBX0.2";
    public string StatusBit { get; set; } = "DB1.DBX0.4";
    public string LoopBarrierBit { get; set; } = "DB1.DBX1.0";
    public int TimeoutMs { get; set; } = 5000;
    public bool PingBeforeConnect { get; set; } = true;
    public int PulseResetMs { get; set; } = 0;
}

/// <summary>
/// Gói tin phản hồi kết quả điều khiển Barrier từ Dịch vụ gửi ngược lên BE.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class BarrierControlResult
{
    public string Type { get; set; } = BarrierControlType.Result;
    public string RequestId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BarrierTargetResult> Results { get; set; } = new();
    public DateTime ResponseTime { get; set; } = DateTime.Now;
    public DateTime FinishedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Kết quả chi tiết của từng thiết bị barrier.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class BarrierTargetResult
{
    public string EquipmentId { get; set; } = string.Empty;
    public string EquipmentCode { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool? LoopDetected { get; set; }
    public string RawDetail { get; set; } = string.Empty;
}
