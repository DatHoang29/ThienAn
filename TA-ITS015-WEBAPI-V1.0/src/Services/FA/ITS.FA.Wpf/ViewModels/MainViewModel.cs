using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITS.FA.Core.Configuration;
using ITS.FA.Core.Messaging;
using ITS.FA.Core.Models;
using ITS.FA.Core.Processing;
using ITS.FA.Wpf.Logging;
using ITS.FA.Wpf.Persistence;
using ITS.FA.Wpf.Simulation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Modules.FA.Core.Entities;
using Modules.TMS.Core.Entities;
using Services.Shared.Configuration;
using Services.Shared.Messaging;

namespace ITS.FA.Wpf.ViewModels;

/// <summary>
/// Form test hệ báo cháy + sự cố. Chạy thẳng <see cref="FaAlarmMonitor"/> của dịch vụ: người test bật tín hiệu trên
/// tủ giả, form đọc lại DB và NATS để đối chiếu kết quả. Vòng ngoài giống FaWorker (lỗi phiên thì chờ rồi dựng lại).
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IAsyncDisposable
{
    private const int MaxLines = 500;

    private readonly FaAlarmMonitor _monitor;
    private readonly FaTestRepository _repository;
    private readonly TransportManager _transport;
    private readonly FaOptions _options;
    private readonly DispatcherTimer _refreshTimer;
    private readonly Dispatcher _dispatcher = Application.Current.Dispatcher;

    private FaPanelSimulator? _simulator;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private bool _isRefreshing;

    public ObservableCollection<FaDeviceItem> Devices { get; } = [];
    public ObservableCollection<TmsIncident> Incidents { get; } = [];
    public ObservableCollection<string> NatsMessages { get; } = [];
    public ObservableCollection<string> Logs { get; } = [];

    [ObservableProperty] private bool _useSimulator = true;
    [ObservableProperty] private string _panelIp;
    [ObservableProperty] private int _panelPort;
    [ObservableProperty] private bool _unknownFire;
    [ObservableProperty] private string _databaseText;
    [ObservableProperty] private string _natsText = "NATS: đang kiểm tra";
    [ObservableProperty] private string _monitorText = "Đang dừng";
    [ObservableProperty] private TmsIncident? _selectedIncident;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(SeedSampleCommand))]
    private bool _isRunning;

    public MainViewModel(
        FaAlarmMonitor monitor,
        FaTestRepository repository,
        TransportManager transport,
        UiLogSink logSink,
        IOptions<FaOptions> options,
        IOptions<DbConnectionOptions> database)
    {
        _monitor = monitor;
        _repository = repository;
        _transport = transport;
        _options = options.Value;
        _panelIp = _options.Modbus.Ip;
        _panelPort = _options.Modbus.Port;
        _databaseText = DescribeDatabase(database.Value);

        logSink.Logged += (level, text) => Post(() => AddLine(Logs, $"{level.ToString()[..4].ToUpperInvariant()} {text}"));

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();
    }

    /// <summary>Kết nối NATS, nạp danh sách. Lỗi DB chỉ ghi log để form vẫn mở được và xem được lý do.</summary>
    public async Task InitializeAsync()
    {
        await SubscribeNatsAsync();
        await ReloadDevicesAsync();
        await RefreshAsync();
        _refreshTimer.Start();
    }

    private async Task SubscribeNatsAsync()
    {
        try
        {
            await _transport.ConnectAsync();
            if (!_transport.IsConnected)
            {
                NatsText = "NATS: chưa kết nối (Nats:Url trống hoặc lỗi), bản tin được bỏ qua";
                return;
            }

            foreach (var subject in new[] { FaSubjects.Incident, FaSubjects.Data, FaSubjects.Heartbeat })
            {
                var captured = subject;
                await _transport.SubscribeAsync(captured, json =>
                {
                    Post(() => AddLine(NatsMessages, $"{DateTime.Now:HH:mm:ss} {captured} {json}"));
                    return Task.CompletedTask;
                });
            }

            NatsText = "NATS: đã kết nối, đang nghe subject của FA";
        }
        catch (Exception ex)
        {
            NatsText = $"NATS lỗi: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        try
        {
            await ReloadDevicesAsync();

            if (UseSimulator)
            {
                var modbus = _options.Modbus;
                if (modbus.FireCountAddress == 0 || modbus.UnknownFireAlarmAddress == 0)
                    throw new InvalidOperationException("Khai Service:Modbus:FireCountAddress và UnknownFireAlarmAddress trong appsettings.json.");

                _simulator = new FaPanelSimulator(modbus.UnitId, modbus.FireCountAddress, modbus.UnknownFireAlarmAddress);
                _simulator.Start(PanelPort);
                PanelIp = "127.0.0.1";
                PushAllToSimulator();
            }

            _options.Modbus.Ip = PanelIp;
            _options.Modbus.Port = PanelPort;

            _runCts = new CancellationTokenSource();
            var token = _runCts.Token;
            _runTask = Task.Run(() => RunLoopAsync(token));
            IsRunning = true;
            MonitorText = UseSimulator ? $"Đang quét tủ giả 127.0.0.1:{PanelPort}" : $"Đang quét tủ {PanelIp}:{PanelPort}";
        }
        catch (Exception ex)
        {
            AddLine(Logs, $"Không bắt đầu được: {ex.Message}");
            await StopSimulatorAsync();
        }
    }

    private bool CanStart() => !IsRunning;

    /// <summary>Giống FaWorker.ExecuteAsync.</summary>
    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _monitor.RunSessionAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Post(() => AddLine(Logs, $"Phiên quét lỗi: {ex.Message}. Dựng lại sau {_options.Modbus.RestartDelaySeconds} giây."));
                _monitor.Reset();
                try { await Task.Delay(TimeSpan.FromSeconds(_options.Modbus.RestartDelaySeconds), ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        _monitor.Reset();
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private async Task StopAsync()
    {
        if (_runCts is not null) await _runCts.CancelAsync();
        if (_runTask is not null) await _runTask;
        _runCts?.Dispose();
        _runCts = null;
        _runTask = null;

        await StopSimulatorAsync();
        IsRunning = false;
        MonitorText = "Đang dừng";
    }

    private async Task StopSimulatorAsync()
    {
        if (_simulator is null) return;
        await _simulator.DisposeAsync();
        _simulator = null;
    }

    /// <summary>Tắt mọi tín hiệu trên tủ: dịch vụ phải thấy hệ thống reset và đưa IsActive trong DB về false.</summary>
    [RelayCommand]
    private void ResetPanel()
    {
        foreach (var device in Devices.Where(d => d.Kind != DoorKind))
            device.IsTriggered = false;
        UnknownFire = false;
        PushAllToSimulator();
    }

    partial void OnUnknownFireChanged(bool value) => _simulator?.SetUnknownFire(value);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task SeedSampleAsync()
    {
        var answer = MessageBox.Show(
            $"Thêm 1 đầu báo, 1 nút ấn, 1 cửa mẫu (mã {FaTestRepository.SampleCodePrefix}*) vào:\n{DatabaseText}\n\nChỉ làm trên DB test. Tiếp tục?",
            "Tạo dữ liệu mẫu", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            var added = await _repository.SeedSampleAsync();
            AddLine(Logs, $"Đã thêm {added} dòng mẫu.");
            await ReloadDevicesAsync();
        }
        catch (Exception ex)
        {
            AddLine(Logs, $"Tạo dữ liệu mẫu lỗi: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task FinishIncidentAsync()
    {
        if (SelectedIncident is not { } incident) return;
        try
        {
            await _repository.FinishIncidentAsync(incident.ID);
            AddLine(Logs, $"Đã kết thúc sự cố {incident.Code}.");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            AddLine(Logs, $"Kết thúc sự cố lỗi: {ex.Message}");
        }
    }

    [RelayCommand]
    private Task ReloadAsync() => ReloadDevicesAsync();

    private async Task ReloadDevicesAsync()
    {
        try
        {
            var points = await _repository.GetPointsAsync();
            var doors = await _repository.GetDoorsAsync();

            // Giữ tín hiệu người test đã tick: Bắt đầu và Nạp lại danh sách đều tạo lại các dòng,
            // trước đây làm mất tick nên tủ giả nhận toàn tín hiệu tắt.
            var triggered = Devices.Where(d => d.IsTriggered).Select(d => d.Id).ToHashSet(StringComparer.Ordinal);

            Devices.Clear();
            foreach (var point in points.Where(p => p.PointAddress != null))
            {
                var kind = point.TypeObject switch
                {
                    (int)FaDeviceType.Detector => "Đầu báo",
                    (int)FaDeviceType.Button => "Nút ấn",
                    _ => $"Loại {point.TypeObject} (không quét)",
                };
                Devices.Add(new FaDeviceItem(point.ID, point.PointAddress!.Value, kind, point.Description,
                    $"Km{point.KmNumber}+{point.MetNumber}", OnDeviceTriggered, triggered.Contains(point.ID)));
            }

            foreach (var door in doors.Where(d => d.ModbusAddress != null))
            {
                Devices.Add(new FaDeviceItem(door.ID, door.ModbusAddress!.Value, DoorKind, door.Description,
                    $"Km{door.KmNumber}+{door.MetNumber}", OnDeviceTriggered, triggered.Contains(door.ID)));
            }

            ApplyDbState(points, doors);
            PushAllToSimulator();
            AddLine(Logs, $"Đã nạp {points.Count} điểm, {doors.Count} cửa. Dữ liệu thêm khi đang chạy cần Dừng rồi Bắt đầu lại.");
        }
        catch (Exception ex)
        {
            AddLine(Logs, $"Đọc DB lỗi: {ex.Message}");
        }
    }

    private void OnDeviceTriggered(FaDeviceItem device)
    {
        if (_simulator is null)
        {
            // Tick khi chưa Bắt đầu: tủ giả chưa chạy nên dịch vụ không đọc được. Giữ trạng thái tick,
            // bấm Bắt đầu thì PushAllToSimulator đẩy lên tủ và dịch vụ phát hiện ngay chu kỳ đầu.
            AddLine(Logs, $"Chưa bấm Bắt đầu: tín hiệu {device.Address} mới được ghi nhận trên form, bấm Bắt đầu để tủ giả và dịch vụ chạy.");
            return;
        }
        _simulator.SetPoint(device.Address, device.IsTriggered);
        UpdateFireCount();
    }

    /// <summary>Số báo cháy trên tủ = số đầu báo và nút ấn đang bật. Cửa không tính.</summary>
    private void UpdateFireCount() =>
        _simulator?.SetFireCount(Devices.Count(d => d.IsTriggered && d.Kind != DoorKind));

    private void PushAllToSimulator()
    {
        if (_simulator is null) return;
        foreach (var device in Devices) _simulator.SetPoint(device.Address, device.IsTriggered);
        _simulator.SetUnknownFire(UnknownFire);
        UpdateFireCount();
    }

    private async Task RefreshAsync()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        try
        {
            var points = await _repository.GetPointsAsync();
            var doors = await _repository.GetDoorsAsync();
            ApplyDbState(points, doors);

            var selectedId = SelectedIncident?.ID;
            var incidents = await _repository.GetRecentIncidentsAsync();
            Incidents.Clear();
            foreach (var incident in incidents) Incidents.Add(incident);
            SelectedIncident = Incidents.FirstOrDefault(i => i.ID == selectedId);

            if (NatsText.StartsWith("NATS: đã", StringComparison.Ordinal) && !_transport.IsConnected)
                NatsText = "NATS: mất kết nối, đang tự kết nối lại";
        }
        catch (Exception ex)
        {
            // Chu kỳ 2 giây: lỗi lặp lại thì không ghi chồng thêm dòng.
            var line = $"Đọc DB lỗi: {ex.Message}";
            if (Logs.Count == 0 || Logs[0] != line) AddLine(Logs, line);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void ApplyDbState(IEnumerable<FaObjectData> points, IEnumerable<FaFireAlarmConfig> doors)
    {
        var states = points.ToDictionary(p => p.ID, p => p.IsActive == true ? "ĐANG BÁO" : "Bình thường");
        foreach (var door in doors) states[door.ID] = door.IsAlarm == true ? "ĐANG BÁO" : "Bình thường";

        foreach (var device in Devices)
            if (states.TryGetValue(device.Id, out var state)) device.DbState = state;
    }

    private static string DescribeDatabase(DbConnectionOptions options)
    {
        var connection = options.ConnectionConfigs.FirstOrDefault()?.ConnectionString;
        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("SET_BY_ENV", StringComparison.Ordinal))
            return "DB: chưa cấu hình (đặt biến DbConnection__ConnectionConfigs__0__ConnectionString)";

        try
        {
            var builder = new SqlConnectionStringBuilder(connection);
            return $"DB: {builder.DataSource} / {builder.InitialCatalog}";
        }
        catch (ArgumentException)
        {
            return "DB: chuỗi kết nối không đọc được";
        }
    }

    private static void AddLine(ObservableCollection<string> lines, string text)
    {
        lines.Insert(0, text);
        while (lines.Count > MaxLines) lines.RemoveAt(lines.Count - 1);
    }

    private void Post(Action action)
    {
        if (!_dispatcher.HasShutdownStarted) _dispatcher.BeginInvoke(action);
    }

    public async ValueTask DisposeAsync()
    {
        _refreshTimer.Stop();
        if (IsRunning) await StopAsync();
    }

    private const string DoorKind = "Cửa";
}
