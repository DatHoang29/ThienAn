using CommunityToolkit.Mvvm.ComponentModel;
using Modules.Integration.Barrier.Base.Enums;
using Modules.Integration.Barrier.Base.Models;

namespace ITS.Barrier.Wpf.ViewModels;

/// <summary>
/// Đại diện cho 1 thiết bị Barrier trên giao diện WPF.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed partial class BarrierTargetItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _ipAddress = "192.168.0.6";

    [ObservableProperty]
    private int _port = 102;

    [ObservableProperty]
    private short _rack = 0;

    [ObservableProperty]
    private short _slot = 0;

    [ObservableProperty]
    private string _openBit = "DB1.DBX0.1";

    [ObservableProperty]
    private string _closeBit = "DB1.DBX0.2";

    [ObservableProperty]
    private string _statusBit = "DB1.DBX0.4";

    [ObservableProperty]
    private string _loopBarrierBit = "DB1.DBX1.0";

    [ObservableProperty]
    private int _timeoutMs = 5000;

    [ObservableProperty]
    private bool _pingBeforeConnect = true;

    [ObservableProperty]
    private int _pulseResetMs = 0;

    [ObservableProperty]
    private string _lastResult = "Chưa kiểm tra";

    [ObservableProperty]
    private bool? _isConnected;

    [ObservableProperty]
    private BarrierStatusType _currentStatus = BarrierStatusType.Closed;

    [ObservableProperty]
    private bool _hasVehicleOnLoop = false;

    public BarrierConfig ToConfig() => new()
    {
        Code = Code,
        IpAddress = IpAddress.Trim(),
        Port = Port,
        Rack = Rack,
        Slot = Slot,
        OpenBit = OpenBit.Trim(),
        CloseBit = CloseBit.Trim(),
        StatusBit = StatusBit.Trim(),
        LoopBarrierBit = LoopBarrierBit.Trim(),
        TimeoutMs = TimeoutMs,
        PingBeforeConnect = PingBeforeConnect,
        PulseResetMs = PulseResetMs
    };
}
