using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;
using ITS.VMS.Core.Storage;
using ITS.VMS.Core.Telemetry;

namespace ITS.VMS.Wpf.ViewModels;

/// <summary>
/// Tab Telemetry của form: thử nghiệm luồng DỊCH VỤ TỰ thu thập thông số thiết bị VMS/LCS.
///
/// Form đóng đúng vai của ITS.VMS.Worker nhưng cho phép bấm tay từng bước, thuận cho test hiện trường:
///   - "Đồng bộ thiết bị"   : hỏi BE danh sách thiết bị (kênh ta.its.data.vms, Type = equipment-request).
///   - "Quét telemetry ngay": đọc thông số toàn bộ thiết bị rồi đẩy về BE (Type = telemetry).
///   - "Bật vòng lặp"       : chạy tự động theo chu kỳ, đúng như Worker sẽ chạy ngoài site.
///
/// Author: ChuongLP
/// Created date: 23/09/2026
/// </summary>
public sealed partial class TelemetryViewModel : ObservableObject
{
    /// <summary>Nguồn số liệu khi người dùng bấm quét tay — khớp VmsStatConst.Source bên BE.</summary>
    private const string SourceManual = "manual";

    private const string SourceAuto = "auto";

    private readonly VmsTelemetryOption _options;
    private readonly VmsEquipmentSyncService _syncService;
    private readonly VmsTelemetryCollector _collector;
    private readonly VmsEquipmentStore _store;
    private readonly VmsDataChannel _channel;
    private readonly VmsLogger _logger;

    /// <summary>Bộ đếm chu kỳ cho chế độ tự động. Dùng DispatcherTimer để cập nhật UI an toàn.</summary>
    private readonly DispatcherTimer _timer = new();

    private CancellationTokenSource? _cts;

    public TelemetryViewModel(VmsTelemetryOption options, VmsEquipmentSyncService syncService,
                              VmsTelemetryCollector collector, VmsEquipmentStore store,
                              VmsDataChannel channel, VmsLogger logger)
    {
        _options = options;
        _syncService = syncService;
        _collector = collector;
        _store = store;
        _channel = channel;
        _logger = logger;

        DataSubject = channel.Subject;
        CollectIntervalMinutes = options.CollectIntervalMinutes > 0 ? options.CollectIntervalMinutes : 15;
        DeviceTypeText = options.DeviceTypeCodes == null ? "(BE tự quyết)" : string.Join(", ", options.DeviceTypeCodes);
        StatTypeText = options.StatTypes == null || options.StatTypes.Length == 0
            ? "(tất cả)" : string.Join(", ", options.StatTypes);
        MaxParallelDevice = options.MaxParallelDevice;

        _timer.Tick += async (_, _) => await CollectAsync(SourceAuto);

        // Nạp danh sách + gói tồn của lần chạy trước để bật form là thấy ngay trạng thái.
        _syncService.LoadFromFile();
        _collector.LoadBuffer();
        RefreshEquipmentGrid();
        RefreshStatus();
    }

    #region Thông tin cấu hình (chỉ đọc, sửa trong appsettings.json)

    [ObservableProperty] private string _dataSubject = string.Empty;

    [ObservableProperty] private int _collectIntervalMinutes;

    [ObservableProperty] private string _deviceTypeText = string.Empty;

    [ObservableProperty] private string _statTypeText = string.Empty;

    [ObservableProperty] private int _maxParallelDevice;

    #endregion

    #region Trạng thái

    /// <summary>Số thiết bị đang giữ / số thiết bị thiếu cấu hình / mốc đồng bộ.</summary>
    [ObservableProperty] private string _equipmentStatus = string.Empty;

    /// <summary>Số gói telemetry chưa đẩy được về BE.</summary>
    [ObservableProperty] private string _bufferStatus = string.Empty;

    /// <summary>Kết quả lượt quét gần nhất.</summary>
    [ObservableProperty] private string _lastCollectSummary = "Chưa quét lần nào.";

    /// <summary>Vòng lặp tự động đang chạy hay không.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AutoStateText))]
    private bool _autoRunning;

    public string AutoStateText => AutoRunning
        ? $"Vòng lặp TỰ ĐỘNG đang chạy, chu kỳ {CollectIntervalMinutes} phút"
        : "Vòng lặp tự động đang tắt";

    /// <summary>Đang chạy một tác vụ (đồng bộ / quét) — chặn bấm chồng.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRun))]
    private bool _isBusy;

    public bool CanRun => !IsBusy;

    #endregion

    public ObservableCollection<EquipmentRowItem> Equipments { get; } = new();

    public ObservableCollection<TelemetryRowItem> Results { get; } = new();

    #region Lệnh

    /// <summary>Hỏi BE danh sách thiết bị và lưu ra file.</summary>
    [RelayCommand]
    private async Task SyncEquipmentAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var message = await _syncService.SyncOnceAsync(CancellationToken.None);
            RefreshEquipmentGrid();
            RefreshStatus();
            _logger.Info(LogCategory.Config, $"[Form] Đồng bộ thiết bị: {message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Quét thông số toàn bộ thiết bị một lần và đẩy về BE.</summary>
    [RelayCommand]
    private async Task CollectNowAsync() => await CollectAsync(SourceManual);

    /// <summary>Bật/tắt vòng lặp tự động — mô phỏng đúng cách Worker chạy ngoài site.</summary>
    [RelayCommand]
    private void ToggleAuto()
    {
        if (AutoRunning)
        {
            _timer.Stop();
            _cts?.Cancel();
            AutoRunning = false;
            _logger.Info(LogCategory.Device, "[Form] Đã TẮT vòng lặp telemetry tự động.");
            return;
        }

        var minutes = CollectIntervalMinutes > 0 ? CollectIntervalMinutes : 15;
        _cts = new CancellationTokenSource();
        _timer.Interval = TimeSpan.FromMinutes(minutes);
        _timer.Start();
        AutoRunning = true;

        _logger.Success(LogCategory.Device, $"[Form] Đã BẬT vòng lặp telemetry tự động, chu kỳ {minutes} phút.");

        // Chạy ngay lượt đầu thay vì chờ hết chu kỳ.
        _ = CollectAsync(SourceAuto);
    }

    /// <summary>Xoá bộ đệm gói chưa đẩy được (dùng khi dữ liệu tồn đã quá cũ, không cần nữa).</summary>
    [RelayCommand]
    private void ClearResults()
    {
        Results.Clear();
        LastCollectSummary = "Chưa quét lần nào.";
    }

    #endregion

    /// <summary>Một lượt quét: đọc thông số rồi đẩy về BE.</summary>
    private async Task CollectAsync(string source)
    {
        if (IsBusy) return;

        if (!_store.HasData && _store.GetNotReadyEquipments().Count == 0)
        {
            LastCollectSummary = "Chưa có danh sách thiết bị. Bấm 'Đồng bộ thiết bị' trước.";
            return;
        }

        try
        {
            IsBusy = true;
            LastCollectSummary = "Đang quét...";

            var summary = await _collector.CollectOnceAsync(source, _cts?.Token ?? CancellationToken.None);

            Results.Clear();
            foreach (var item in summary.Items) Results.Add(TelemetryRowItem.From(item));

            LastCollectSummary = $"[{summary.CollectedAt:HH:mm:ss}] {summary.Message}";
            RefreshStatus();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Vẽ lại lưới danh sách thiết bị từ store.</summary>
    private void RefreshEquipmentGrid()
    {
        RunOnUi(() =>
        {
            Equipments.Clear();
            foreach (var info in _store.GetAll()) Equipments.Add(EquipmentRowItem.From(info));
        });
    }

    private void RefreshStatus()
    {
        RunOnUi(() =>
        {
            var notReady = _store.GetNotReadyEquipments().Count;
            var syncedAt = _store.SyncedAt;

            EquipmentStatus = $"{_store.Count} thiết bị" +
                              (notReady > 0 ? $" ({notReady} thiếu Vendor/Ip)" : string.Empty) +
                              (syncedAt.HasValue ? $" · đồng bộ lúc {syncedAt:yyyy-MM-dd HH:mm:ss}" : " · chưa đồng bộ");

            BufferStatus = _collector.PendingPackets == 0
                ? "Không có gói tồn"
                : $"{_collector.PendingPackets} gói chưa đẩy được về BE";
        });
    }

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }
}
