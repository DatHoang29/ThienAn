using CommunityToolkit.Mvvm.ComponentModel;
using Modules.Integration.TrafficLight.Base.Enums;
using Modules.Integration.TrafficLight.Base.Models;

namespace ITS.TrafficLight.Wpf.ViewModels;

/// <summary>
/// Đại diện cho 1 thiết bị Đèn tín hiệu trên giao diện WPF.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed partial class TrafficLightTargetItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _ipAddress = "192.168.0.10";

    [ObservableProperty]
    private int _port = 102;

    [ObservableProperty]
    private short _rack = 0;

    [ObservableProperty]
    private short _slot = 0;

    [ObservableProperty]
    private TrafficLightVendor _vendor = TrafficLightVendor.SiemensS71200;

    [ObservableProperty]
    private int _dbNumber = 3;

    [ObservableProperty]
    private int _timeOn = 3;

    [ObservableProperty]
    private int _timeOff = 1;

    [ObservableProperty]
    private int _redOutput = 1;

    [ObservableProperty]
    private int _yellowOutput = 2;

    [ObservableProperty]
    private int _greenOutput = 3;

    [ObservableProperty]
    private int _fmOutput = 4;

    [ObservableProperty]
    private int _timeoutMs = 5000;

    [ObservableProperty]
    private bool _pingBeforeConnect = true;

    [ObservableProperty]
    private TrafficLightColor _currentColor = TrafficLightColor.Off;

    [ObservableProperty]
    private string _lastResult = "Chưa kiểm tra";

    [ObservableProperty]
    private bool? _isConnected;

    public TrafficLightConfig ToConfig() => new()
    {
        Code = Code,
        IpAddress = IpAddress.Trim(),
        Port = Port,
        Rack = Rack,
        Slot = Slot,
        DbNumber = DbNumber,
        TimeOn = TimeOn,
        TimeOff = TimeOff,
        RedOutput = RedOutput,
        YellowOutput = YellowOutput,
        GreenOutput = GreenOutput,
        FmOutput = FmOutput,
        TimeoutMs = TimeoutMs,
        PingBeforeConnect = PingBeforeConnect
    };
}
