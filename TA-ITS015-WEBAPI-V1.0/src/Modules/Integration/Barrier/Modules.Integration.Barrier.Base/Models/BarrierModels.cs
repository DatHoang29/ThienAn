using Modules.Integration.Barrier.Base.Constants;
using Modules.Integration.Barrier.Base.Enums;

namespace Modules.Integration.Barrier.Base.Models;

/// <summary>
/// Cấu hình kết nối 1 barrier. BE/dịch vụ đóng gói đủ thông tin này trong mỗi lệnh
/// (dịch vụ không đọc CSDL — cùng nguyên tắc với VMS).
/// </summary>
public class BarrierConfig
{
    /// <summary>Mã thiết bị (EquipmentCode) — chỉ để log/đối chiếu.</summary>
    public string? Code { get; set; }

    public BarrierVendor Vendor { get; set; } = BarrierVendor.SiemensS71200;

    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Cổng S7, mặc định 102.
    /// LƯU Ý: cột PORT trong DB cũ CLVH (thường = 2000) là cổng SOCKET nhận sự kiện text
    /// (BR_MO, LA_ON...) của clsPLC_S71200, KHÔNG phải cổng S7. Đừng map nhầm vào đây.
    /// </summary>
    public int Port { get; set; } = IntgBarrierConstant.DefaultS7Port;

    public short Rack { get; set; } = IntgBarrierConstant.DefaultRack;
    public short Slot { get; set; } = IntgBarrierConstant.DefaultSlot;
    public int TimeoutMs { get; set; } = IntgBarrierConstant.DefaultTimeoutMs;

    /// <summary>Ping ICMP trước khi kết nối — giữ hành vi cũ NetworkHelper.fn_PingHost.</summary>
    public bool PingBeforeConnect { get; set; } = true;

    /// <summary>
    /// Sau bao nhiêu ms thì ghi lại bit = false.
    /// 0 (mặc định) = GIỮ NGUYÊN HÀNH VI CŨ: CLVH chỉ ghi bit = true, PLC tự reset.
    /// Chỉ đặt &gt; 0 khi xác nhận chương trình PLC KHÔNG tự reset bit.
    /// </summary>
    public int PulseResetMs { get; set; } = 0;

    // Địa chỉ bit PLC từ dự án cũ CLVH
    public string OpenBit { get; set; } = IntgBarrierConstant.OpenBit;
    public string CloseBit { get; set; } = IntgBarrierConstant.CloseBit;
    public string StatusBit { get; set; } = IntgBarrierConstant.StatusBit;
    public string LoopBarrierBit { get; set; } = IntgBarrierConstant.LoopBarrierBit;
}

public class BarrierCommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}

public class BarrierStatusResult
{
    public bool IsConnected { get; set; }
    public BarrierStatusType Status { get; set; } = BarrierStatusType.Unknown;
    public bool? LoopDetected { get; set; }
    public string RawDetail { get; set; } = string.Empty;
}
