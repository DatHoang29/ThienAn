using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services.Vds.Shared.Domain;
using Services.Vds.Diagnostics;
using Services.Vds.Shared.Testing;
using Services.Vds.Configuration;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Services.Vds.Features.ZoneStatus;
using Services.Vds.Manager;
using Services.Vds.Wpf.Logging;
using Services.Vds.Wpf.Simulation;
using Microsoft.Extensions.Options;
using Services.Shared.Events;
using Shared.Utility.Utilities;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;

namespace Services.Vds.Wpf.ViewModels;

/// <summary>
/// Author: KienLT
/// Description: Toàn bộ trạng thái và hành vi của cửa sổ test VDS — thay ~450 dòng code-behind của
/// <c>VdsTestWindow.xaml.cs</c> (P9). Hai nhóm chức năng:
/// <list type="bullet">
/// <item><b>Regular (tự động)</b> — chọn camera sinh raw, chọn camera được đếm, khai khoảng random cho
/// nhịp gửi/tốc độ/chiều dài rồi chạy liên tục; Statistic gửi theo chu kỳ từ đúng số liệu đã sinh.</item>
/// <item><b>Sự kiện (thủ công)</b> — tích một hay nhiều camera, chọn loại sự kiện và tham số để bơm bản tin.</item>
/// </list>
/// Cả hai đều bơm qua <see cref="IVdsFrameInjector"/> nên đi đúng pipeline như bản tin SSE thật.
///
/// Feed nhật ký nhận qua <c>ActivityLoggerProvider</c>: log của pipeline và activity MediatR của simulator
/// (chuyển tiếp bởi <c>ActivityNotificationForwarder</c>) cùng đổ về một chỗ, còn ViewModel tự phát activity
/// cho các thao tác của người dùng. Nhờ vậy không còn <c>UiLog</c> static.
///
/// KienLT - 29/10/2026: ViewModel KHÔNG được làm <c>INotificationHandler</c> nữa — MediatR quét assembly sẽ
/// đăng ký nó dạng transient và dựng một bản mới mỗi lần phát (rò RAM + cạn pool SQL, xem
/// <c>ActivityNotificationForwarder</c>).
/// Created date: 04/08/2026
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    /// <summary>Trần số dòng nhật ký giữ trên UI.</summary>
    private const int MaxActivities = 500;

    /// <summary>Giai đoạn của activity do chính ViewModel phát (phân biệt với log của Core).</summary>
    private const string UiStage = "Harness";

    /// <summary>
    /// Danh sách loại bản tin bơm được bằng tay ở tab sự kiện — lấy thẳng 16 loại của đặc tả v1.3
    /// (<see cref="VdsEventTypes.All"/>) để harness không bao giờ lệch tên với pipeline.
    /// </summary>
    public static readonly string[] AvailableEventTypes = VdsEventTypes.All.ToArray();

    private readonly IVdsFrameInjector _injector;
    private readonly CameraManager _cameras;
    private readonly ZoneManager _zoneCatalog;
    private readonly VdsCatalogRepository _catalog;
    private readonly ZoneStatusUpdater _zoneStatus;
    private readonly VdsConfigManager _config;
    private readonly RegularSimulator _simulator;
    private readonly SimulatorOptions _defaults;
    private readonly VdsDiagnosticsRepository _diagnostics;
    private readonly IVdsServiceManager _manager;
    private readonly SpeedRangeResolver _speeds;

    /// <summary>
    /// Dispatcher của luồng UI, chốt tại constructor. <see cref="ActivityNotification"/> đến từ luồng nền
    /// (consumer Wolverine, bộ mô phỏng) nên phải marshal về đây trước khi sửa
    /// <see cref="ObservableCollection{T}"/>. KHÔNG lấy <c>Dispatcher.CurrentDispatcher</c> trong
    /// <see cref="Handle"/> vì lúc đó nó trả về dispatcher của luồng nền.
    /// </summary>
    private readonly Dispatcher _dispatcher;

    public MainViewModel(
        IVdsFrameInjector injector,
        CameraManager cameras,
        ZoneManager zoneCatalog,
        VdsCatalogRepository catalog,
        ZoneStatusUpdater zoneStatus,
        VdsConfigManager config,
        RegularSimulator simulator,
        IOptions<SimulatorOptions> defaults,
        VdsDiagnosticsRepository diagnostics,
        IVdsServiceManager manager,
        ActivityLoggerProvider pipelineLogs,
        SpeedRangeResolver speeds,
        CatalogRefresher catalogRefresher)
    {
        _injector = injector;
        _cameras = cameras;
        _zoneCatalog = zoneCatalog;
        _catalog = catalog;
        _zoneStatus = zoneStatus;
        _config = config;
        _simulator = simulator;
        _defaults = defaults.Value;
        _diagnostics = diagnostics;
        _manager = manager;
        _speeds = speeds;
        _dispatcher = Dispatcher.CurrentDispatcher;

        MinIntervalSeconds = _defaults.MinIntervalSeconds;
        MaxIntervalSeconds = _defaults.MaxIntervalSeconds;
        MinLengthMeter = _defaults.MinLengthMeter;
        MaxLengthMeter = _defaults.MaxLengthMeter;
        StatisticPeriodMinutes = _defaults.StatisticPeriodMinutes;
        SelectedEventType = AvailableEventTypes[0];

        LoadCameras();
        _ = LoadZonesAsync();

        // Đăng ký SAU khi dựng xong state: nhận cả log đã đệm từ lúc khởi động (nạp danh mục camera,
        // Wolverine, cảnh báo IP trùng...) rồi nhận tiếp log realtime.
        pipelineLogs.Subscribe(Post);

        // Danh mục được nạp lại giữa lúc chạy (lệnh refresh NATS từ web — vd vừa đổi Min/Max của zone —, hoặc
        // chu kỳ Catalog:ReloadSeconds) ⇒ cập nhật ngưỡng/vùng trên màn hình. Sự kiện đến từ luồng nền.
        catalogRefresher.Reloaded += parts =>
        {
            if ((parts & (CatalogParts.Cameras | CatalogParts.Zones | CatalogParts.OpConfig)) == 0) return;
            if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished) return;
            _dispatcher.BeginInvoke(new Action(() => OnCatalogReloaded(parts)));
        };
    }

    // ════════════════════ Nhật ký ════════════════════

    /// <summary>Feed hoạt động, mới nhất ở trên.</summary>
    public ObservableCollection<Activity> Activities { get; } = new();

    // ════════════════════ Danh mục camera ════════════════════

    /// <summary>Camera sinh dữ liệu raw (tab mô phỏng).</summary>
    public ObservableCollection<CameraCheckItem> RawCameras { get; } = new();

    /// <summary>
    /// Mỗi hướng tuyến một dòng: chọn camera nhận BẢN NHÂN ĐÔI số đếm của hướng đó.
    /// Bỏ trống = hướng đó không nhân bản. Thay cho danh sách "camera được đếm" trước đây —
    /// phạm vi đếm giờ để mặc định RỖNG, tức đếm mọi camera đã nhận diện.
    /// </summary>
    public ObservableCollection<DirectionCountOption> DirectionCounts { get; } = new();

    /// <summary>
    /// Camera ở tab sự kiện thủ công, tích được nhiều camera để bắn một lượt.
    /// </summary>
    public ObservableCollection<CameraCheckItem> EventCameras { get; } = new();

    /// <summary>Camera đang tích ở tab sự kiện, theo thứ tự hiển thị (hướng → Km).</summary>
    private List<CameraCheckItem> CheckedEventCameras => EventCameras.Where(c => c.IsChecked).ToList();

    public string SelectedCameraInfo
    {
        get
        {
            var picked = CheckedEventCameras;
            if (picked.Count == 0) return $"Chưa chọn camera nào (0/{EventCameras.Count}).";
            if (picked.Count == 1) return $"Đã chọn 1/{EventCameras.Count}: {picked[0].DirectionName} · EquipmentId={picked[0].EquipmentId}";
            return $"Đã chọn {picked.Count}/{EventCameras.Count} camera.";
        }
    }

    /// <summary>Tích/bỏ một camera ⇒ cập nhật dòng thông tin, tốc độ hiển thị và nút Gửi.</summary>
    private void OnEventCameraChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CameraCheckItem.IsChecked)) return;
        OnPropertyChanged(nameof(SelectedCameraInfo));
        OnPropertyChanged(nameof(EffectiveSpeedInfo));
        SendCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CheckAllEvent() => SetAll(EventCameras, true);

    [RelayCommand]
    private void UncheckAllEvent() => SetAll(EventCameras, false);

    /// <summary>Bấm ở tiêu đề nhóm: cả hướng đang tích hết thì bỏ hết, còn lại thì tích hết.</summary>
    [RelayCommand]
    private void ToggleEventDirection(object? directionName)
    {
        var name = directionName?.ToString();
        var items = EventCameras.Where(c => string.Equals(c.DirectionName, name, StringComparison.Ordinal)).ToList();
        if (items.Count == 0) return;
        SetAll(items, !items.All(c => c.IsChecked));
    }

    // ════════════════════ Tham số mô phỏng ════════════════════

    [ObservableProperty] private int _minIntervalSeconds;
    [ObservableProperty] private int _maxIntervalSeconds;
    [ObservableProperty] private double _minLengthMeter;
    [ObservableProperty] private double _maxLengthMeter;
    [ObservableProperty] private int _statisticPeriodMinutes;

    /// <summary>
    /// Ngưỡng tốc độ ĐANG hiệu lực, chỉ để xem — ngưỡng vùng (<c>TmsZone.MinSpeed/MaxSpeed</c>) và mặc định
    /// theo hướng (<c>VdsConfig</c> <c>vds_route_{hướng}_min_speed</c> / <c>_max_speed</c>), sửa trên web.
    /// </summary>
    [ObservableProperty] private string _speedRangeInfo = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSimulationCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopSimulationCommand))]
    private bool _isSimulating;

    [ObservableProperty] private string _simulationStatus = "Chưa chạy";

    // ════════════════════ Tham số job / DB ════════════════════

    [ObservableProperty] private string _databaseInfo = "";

    // ════════════════════ Tab Zone ════════════════════

    private const int DefaultZoneSlowSpeedKmh = 20;
    private const int DefaultZoneFreeSpeedKmh = 80;

    /// <summary>Vùng lấy từ mapping của chính các camera VDS đang cấu hình (<c>TmsZoneEquipment</c>).</summary>
    public ObservableCollection<ZoneOption> Zones { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedZoneInfo))]
    [NotifyCanExecuteChangedFor(nameof(ApplyZoneConditionCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendZoneFreeFlowCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendZoneSlowCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendZoneSlowEventCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelZoneSlowCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendZoneCongestionCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelZoneCongestionCommand))]
    private ZoneOption? _selectedZone;

    public string SelectedZoneInfo => SelectedZone is null
        ? ""
        : $"→ ZoneId={SelectedZone.ZoneId} · ngưỡng {ZoneSpeedText(SelectedZone)} · "
          + $"{SelectedZone.Cameras.Count} camera: "
          + string.Join(", ", SelectedZone.Cameras.Select(c => c.Name ?? c.CameraIp));

    /// <summary>Ngưỡng vùng; thiếu hướng vùng thì dùng hướng camera đầu tiên, cùng camera gửi mẫu.</summary>
    private ResolvedSpeedRange? ZoneSpeedOf(ZoneOption zone)
        => _speeds.ForZone(zone.ZoneId, zone.Cameras.FirstOrDefault()?.Direction);

    /// <summary>Min hiệu lực để đánh giá chậm, không phụ thuộc max của vùng.</summary>
    private int? ZoneMinSpeedOf(ZoneOption zone)
        => _speeds.MinSpeedOfZone(zone.ZoneId, zone.Cameras.FirstOrDefault()?.Direction);

    private string ZoneSpeedText(ZoneOption zone)
    {
        if (ZoneSpeedOf(zone) is ResolvedSpeedRange speed) return speed.ToString();
        return ZoneMinSpeedOf(zone) is int min
            ? $"min hiệu lực {min} km/h (chưa có cặp min/max hợp lệ)"
            : "chưa có (vùng và hướng đều chưa khai min hợp lệ)";
    }

    /// <summary>
    /// Đổi vùng ⇒ đặt lại mẫu: xe "chậm" theo min hiệu lực; xe "thoáng" dùng cặp đủ nếu có, thiếu max thì
    /// dùng min + ClearMarginKmh. Thiếu min ⇒ dùng mặc định, không giữ tốc độ của vùng trước. Vẫn sửa tay được.
    /// </summary>
    partial void OnSelectedZoneChanged(ZoneOption? value)
    {
        ZoneSlowSpeedKmh = DefaultZoneSlowSpeedKmh;
        ZoneFreeSpeedKmh = DefaultZoneFreeSpeedKmh;
        if (value is null || ZoneMinSpeedOf(value) is not int min) return;

        ZoneSlowSpeedKmh = Math.Max(min / 2, 1);
        // Giữa ngưỡng, nhưng không dưới V_min + ClearMarginKmh — dưới mức đó xe "thoáng" không hạ được bậc chậm.
        var clearAt = min + (int)Math.Ceiling(_config.Current.ZoneStatus.SpeedSlow.ClearMarginKmh);
        ZoneFreeSpeedKmh = ZoneSpeedOf(value) is ResolvedSpeedRange speed
            ? Math.Max((speed.Range.Min + speed.Range.Max) / 2, clearAt)
            : clearAt;
    }

    /// <summary>
    /// Ba trạng thái khai trong <c>Service:ZoneStatus</c> — đúng bộ mã FE đang dùng
    /// (<c>NormalCondition</c> / <c>SlowCondition</c> / <c>CongestedCondition</c>).
    /// </summary>
    public IReadOnlyList<string> ZoneConditions => ZoneConditionChoices.Keys.ToList();

    /// <summary>Nhãn trên UI ↔ bậc của máy trạng thái. Một chỗ khai duy nhất để không lệch chuỗi.</summary>
    private static readonly Dictionary<string, ZoneCondition> ZoneConditionChoices =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Thông thoáng"] = ZoneCondition.Normal,
            ["Chậm"] = ZoneCondition.Slow,
            ["Nghẽn"] = ZoneCondition.Congested
        };

    [ObservableProperty] private string _selectedZoneCondition = "Thông thoáng";

    /// <summary>
    /// Số xe mỗi lượt bơm mẫu. Mặc định 5 = <c>ZoneStatus:SpeedSlow:MinVehicles</c> mặc định: ít hơn thì đánh
    /// giá chậm theo tốc độ chưa đủ xe để kết luận.
    /// </summary>
    [ObservableProperty] private int _zoneSampleCount = 5;

    /// <summary>
    /// Tốc độ xe "thông thoáng" — phải ≥ <c>V_min + ClearMarginKmh</c> của vùng mới hạ được bậc chậm theo
    /// tốc độ. Tự đặt giữa ngưỡng vùng khi đổi vùng.
    /// </summary>
    [ObservableProperty] private int _zoneFreeSpeedKmh = DefaultZoneFreeSpeedKmh;

    /// <summary>
    /// Tốc độ xe "chậm" — phải DƯỚI <c>V_min</c> của vùng mới tính là xe chậm. Chỉ có tác dụng khi bật
    /// <c>ZoneStatus:SpeedSlow:Enabled</c>; tắt thì xe chậm không làm đổi trạng thái vùng.
    /// </summary>
    [ObservableProperty] private int _zoneSlowSpeedKmh = DefaultZoneSlowSpeedKmh;

    [ObservableProperty] private string _zoneStatusInfo = "";

    // ════════════════════ Tham số gửi thủ công ════════════════════

    public IReadOnlyList<string> EventTypes => AvailableEventTypes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSpeedEditable))]
    [NotifyPropertyChangedFor(nameof(EffectiveSpeedInfo))]
    private string _selectedEventType = VdsEventTypes.Normal;

    [ObservableProperty] private string _lane = "1";
    [ObservableProperty] private string _plate = "60A90685";
    [ObservableProperty] private int _speedKmh = 100;

    /// <summary>
    /// Chỉ loại <c>Speeding</c> cho nhập tốc độ tay — nó là loại DUY NHẤT mà tốc độ quyết định kết quả
    /// (so với ngưỡng của hướng). Các loại khác lấy tốc độ TRONG ngưỡng để không vô tình thành vi phạm.
    /// </summary>
    public bool IsSpeedEditable =>
        string.Equals(SelectedEventType, VdsEventTypes.Speeding, StringComparison.OrdinalIgnoreCase);

    /// <summary>Mô tả tốc độ sẽ gửi thực tế — để người dùng thấy trước khi bấm Gửi.</summary>
    public string EffectiveSpeedInfo
    {
        get
        {
            if (IsSpeedEditable) return "nhập tay (loại Speeding)";
            var picked = CheckedEventCameras;
            if (picked.Count == 0) return "";
            // Nhiều camera: mỗi camera lấy giữa ngưỡng của chính nó, tốc độ cụ thể ghi trong nhật ký khi gửi.
            if (picked.Count > 1) return "giữa ngưỡng của từng camera";
            var speed = SpeedRangeOf(picked[0].Ip);
            return speed is null
                ? "chưa có ngưỡng cho camera này (vùng và hướng đều chưa khai)"
                : $"{MidSpeed(speed.Range)} km/h (giữa ngưỡng {speed})";
        }
    }

    /// <summary>
    /// Ngưỡng tốc độ của một camera ở tab gửi thủ công — cùng nguồn với ngưỡng đẩy xuống AID:
    /// vùng chính của camera, giá trị vùng không khai thì mặc định theo hướng.
    /// </summary>
    private ResolvedSpeedRange? SpeedRangeOf(string ip)
        => _cameras.TryGet(ip, out var info) && info is not null ? _speeds.ForCamera(info) : null;

    /// <summary>Giữa ngưỡng — tất định để lượt gửi tay lặp lại cho cùng kết quả.</summary>
    private static int MidSpeed(SpeedRange range) => (range.Min + range.Max) / 2;

    /// <summary>
    /// Tốc độ thực sự gửi đi cho một camera: loại <c>Speeding</c> dùng giá trị nhập tay, còn lại dùng giữa
    /// ngưỡng của camera đó. Không có ngưỡng ⇒ giữ giá trị đang nhập (bản tin vẫn gửi được, chỉ là không
    /// đảm bảo nằm trong ngưỡng nào).
    /// </summary>
    private int EffectiveSpeedKmhOf(string ip)
    {
        if (IsSpeedEditable) return SpeedKmh;
        var range = SpeedRangeOf(ip)?.Range;
        return range is null ? SpeedKmh : MidSpeed(range);
    }

    /// <summary>Chiều dài xe; để trống ⇒ bản ghi vào nhóm Other khi tổng hợp.</summary>
    [ObservableProperty] private string _lengthMeterText = "4.5";

    [ObservableProperty] private int _sendCount = 1;

    /// <summary>Trạng thái vòng đời của service — chỉ để quan sát, lifecycle do App điều khiển.</summary>
    public VdsServiceManagerState ServiceState => _manager.State;

    // ════════════════════ Lệnh: danh mục ════════════════════

    /// <summary>
    /// Nạp lại toàn bộ danh mục lên UI (camera + vùng). Gọi từ <c>App</c> ngay sau
    /// <c>IServiceManager.InitAsync()</c>: constructor của ViewModel chạy TRƯỚC đó (cửa sổ hiện ngay để
    /// người dùng thấy nhật ký khởi động) nên lúc ấy <c>CameraManager</c> còn rỗng.
    ///
    /// An toàn khi gọi nhiều lần: <see cref="LoadCameras"/> dựng lại danh sách và
    /// <see cref="BuildDirectionCounts"/> giữ nguyên lựa chọn camera đếm theo hướng.
    /// </summary>
    public void RefreshCatalog()
    {
        LoadCameras();
        RefreshSpeedRangeInfo();
        _ = LoadZonesAsync();
    }

    /// <summary>
    /// Hiện ngưỡng tốc độ đang hiệu lực (chỉ để xem). Nguồn là <c>VdsConfig</c> do
    /// <c>OpConfigManager</c> nạp lúc khởi động, nên phải gọi SAU <c>InitAsync</c>.
    /// </summary>
    /// <summary>
    /// Danh mục vừa nạp lại: làm mới dòng ngưỡng, danh sách vùng (giữ vùng đang chọn) và thông tin tốc độ.
    /// KHÔNG dựng lại danh sách camera (sẽ mất các ô đã tick) — camera mới/xoá thì bấm "Tải lại camera".
    /// Bộ mô phỏng đang chạy tự lấy ngưỡng mới ở lượt xe kế tiếp (RegularSimulator tra lại mỗi lượt).
    /// </summary>
    private void OnCatalogReloaded(CatalogParts parts)
    {
        if (_cameras.Count == 0) return; // chưa Init xong: RefreshCatalog() sau Init sẽ lo
        RefreshSpeedRangeInfo();
        _ = LoadZonesAsync(keepSelection: true);
        OnPropertyChanged(nameof(SelectedZoneInfo));
        Info($"Danh mục vừa nạp lại ({parts}) ⇒ đã cập nhật ngưỡng tốc độ/vùng trên màn hình"
             + (IsSimulating ? "; mô phỏng đang chạy áp ngưỡng mới từ lượt xe kế tiếp." : "."));
    }

    private void RefreshSpeedRangeInfo()
    {
        var ranges = _config.Current.SpeedRangeByDirection;
        var defaults = ranges.Count == 0
            ? "mặc định theo hướng: CHƯA KHAI (VdsConfig nhóm 'vds_config')"
            : "mặc định theo hướng (VdsConfig): " + string.Join(" · ", ranges
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => $"{_defaults.DirectionLabel(kv.Key)} = {kv.Value.Min}–{kv.Value.Max} km/h"));

        SpeedRangeInfo = $"Ngưỡng tốc độ: {_zoneCatalog.ZoneSpeedCount} vùng khai riêng (TmsZone.MinSpeed/MaxSpeed), "
                       + $"còn lại dùng {defaults}";

        OnPropertyChanged(nameof(EffectiveSpeedInfo));
    }

    [RelayCommand]
    private void ReloadCameras() => LoadCameras();

    [RelayCommand]
    private void CheckAllRaw() => SetAll(RawCameras, true);

    [RelayCommand]
    private void UncheckAllRaw() => SetAll(RawCameras, false);

    private static void SetAll(IEnumerable<CameraCheckItem> items, bool value)
    {
        foreach (var item in items) item.IsChecked = value;
    }

    /// <summary>Bỏ chọn camera đếm của một hướng ⇒ hướng đó không nhân bản nữa.</summary>
    [RelayCommand]
    private void ClearDirectionCount(DirectionCountOption? option)
    {
        if (option is not null) option.Selected = null;
    }

    // ════════════════════ Lệnh: mô phỏng ════════════════════

    [RelayCommand(CanExecute = nameof(CanStartSimulation))]
    private void StartSimulation()
    {
        if (!TryBuildRun(out var run, out var error))
        {
            Warn(error);
            return;
        }

        _simulator.Start(run!);
        IsSimulating = true;
        SimulationStatus = $"Đang chạy — 1 xe/lượt trong {run!.Cameras.Count} camera";
    }

    private bool CanStartSimulation() => !IsSimulating;

    [RelayCommand(CanExecute = nameof(CanStopSimulation))]
    private async Task StopSimulationAsync()
    {
        await _simulator.StopAsync();
        IsSimulating = false;
        SimulationStatus = "Đã dừng";
    }

    private bool CanStopSimulation() => IsSimulating;

    /// <summary>Kiểm tra các khoảng giá trị; trả false kèm lý do để hiện lên nhật ký.</summary>
    private bool TryBuildRun(out SimulatorRun? run, out string error)
    {
        run = null;
        error = "";

        // Ngưỡng mỗi camera phân giải đúng như ngưỡng đẩy xuống AID: vùng chính → mặc định theo hướng.
        // Camera không phân giải được thì không sinh được tốc độ hợp lệ ⇒ loại khỏi lượt chạy.
        var picked = RawCameras.Where(i => i.IsChecked).ToList();
        var cameras = new List<SimulatorCamera>();
        var dropped = new List<string>();
        foreach (var item in picked)
        {
            if (_cameras.TryGet(item.Ip, out var info) && info is not null
                && _speeds.ForCamera(info) is ResolvedSpeedRange speed)
                cameras.Add(new SimulatorCamera(item.Ip, item.DirectionId, speed));
            else
                dropped.Add(item.Ip);
        }

        if (dropped.Count > 0)
            Warn($"Bỏ {dropped.Count} camera khỏi lượt chạy vì chưa có ngưỡng tốc độ (vùng chính và hướng đều "
               + $"chưa khai, hoặc ghép ra min > max): {string.Join(", ", dropped)}");

        if (cameras.Count == 0)
        {
            error = picked.Count == 0
                ? "Chưa chọn camera nào để sinh dữ liệu raw."
                : "Không camera nào được chọn có ngưỡng tốc độ: khai TmsZone.MinSpeed/MaxSpeed cho vùng, hoặc "
                  + "vds_route_{hướng}_min_speed / _max_speed trong VdsConfig nhóm 'vds_config'.";
            return false;
        }

        if (MinIntervalSeconds <= 0 || MaxIntervalSeconds < MinIntervalSeconds)
        { error = "Nhịp gửi không hợp lệ (phải > 0 và min <= max)."; return false; }
        if (MinLengthMeter <= 0 || MaxLengthMeter < MinLengthMeter)
        { error = "Khoảng chiều dài không hợp lệ (phải > 0 và min <= max)."; return false; }
        if (StatisticPeriodMinutes <= 0)
        { error = "Chu kỳ Statistic phải là số nguyên dương."; return false; }

        var countByDirection = DirectionCounts
            .Where(d => d.Selected is not null)
            .ToDictionary(d => d.DirectionId, d => d.Selected!.Ip, StringComparer.OrdinalIgnoreCase);

        // Camera đếm không nằm trong danh sách sinh raw vẫn hợp lệ (nó chỉ nhận bản nhân đôi), nhưng nói rõ
        // để người dùng không tưởng là đã chọn sai.
        var outside = countByDirection.Values
            .Where(ip => !cameras.Any(c => string.Equals(c.Ip, ip, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (outside.Count > 0)
            Warn($"{outside.Count} camera đếm không nằm trong danh sách sinh raw ⇒ chỉ nhận bản nhân đôi, "
               + "không tự sinh xe: " + string.Join(", ", outside));

        run = new SimulatorRun(cameras, countByDirection, MinIntervalSeconds, MaxIntervalSeconds,
            MinLengthMeter, MaxLengthMeter, StatisticPeriodMinutes);
        return true;
    }

    /// <summary>Một lượt giả lập theo hợp đồng chung <c>IServiceManager</c> (một bản tin cho mỗi camera).</summary>
    [RelayCommand]
    private async Task SimulateBatchAsync()
    {
        try { await _manager.SimulateBatchAsync(); }
        catch (Exception ex) { Error("Giả lập batch lỗi: " + ex.Message); }
    }

    // ════════════════════ Lệnh: job theo yêu cầu ════════════════════

    /// <summary>
    /// Chỉ đọc số bản ghi để quan sát; không chạy job, không dựng lại số đếm và không xoá dữ liệu.
    /// </summary>
    [RelayCommand]
    private Task CountRowsAsync() => RefreshRowCountsAsync();

    /// <summary>Đếm số bản ghi các bảng chính để quan sát kết quả sau mỗi lượt test.</summary>
    private async Task RefreshRowCountsAsync()
    {
        try
        {
            var c = await _diagnostics.CountRowsAsync();
            string N(int v) => v < 0 ? "n/a" : v.ToString();
            DatabaseInfo = $"DB: {_diagnostics.DescribeConnection()} — QuestDB 24h: Raw {N(c.TrafficDataRecent)} · "
                         + $"Statistic {N(c.StatisticRecent)} — SQL: Incident {c.Incident} · "
                         + $"ZoneStatus {c.ZoneStatus} · FilterHistory {c.FilterHistory}";
        }
        catch (Exception ex)
        {
            DatabaseInfo = "Không đọc được số bản ghi: " + ex.Message;
        }
    }

    private static DateTime Floor5(DateTime t)
        => new(t.Year, t.Month, t.Day, t.Hour, t.Minute / 5 * 5, 0, t.Kind);

    // ════════════════════ Lệnh: tab Zone ════════════════════

    [RelayCommand]
    private Task ReloadZones() => LoadZonesAsync();

    /// <summary>Ép trạng thái vùng đang chọn: ghi <c>TmsZoneStatus</c> + phát NATS ngay.</summary>
    [RelayCommand(CanExecute = nameof(HasZone))]
    private async Task ApplyZoneConditionAsync()
    {
        var zone = SelectedZone!;

        try
        {
            var ok = await _zoneStatus.ForceConditionAsync(
                zone.ZoneId,
                SelectedZoneConditionValue,
                zone.Cameras.FirstOrDefault()?.EquipmentId,
                default);

            if (ok)
                Info($"Đã đặt zone {zone.Display} = {SelectedZoneCondition} (ghi TmsZoneStatus + phát NATS).");
            else
                Warn("Không đặt được trạng thái vùng — kiểm tra Service:ZoneStatus (Enabled + mã Condition + ngưỡng).");

            await RefreshZoneStatusAsync();
        }
        catch (Exception ex) { Error("Đặt trạng thái vùng lỗi: " + ex.Message); }
    }

    /// <summary>
    /// Mẫu: chạy N xe THÔNG THOÁNG qua vùng ⇒ hạ bậc chậm THEO TỐC ĐỘ (chỉ khi bật
    /// <c>ZoneStatus:SpeedSlow</c>). Không hạ chốt sự kiện — chốt đó chỉ hết theo TTL hoặc nút huỷ.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasZone))]
    private Task SendZoneFreeFlowAsync() => SendZoneVehiclesAsync(ZoneFreeSpeedKmh, "thông thoáng");

    /// <summary>
    /// Mẫu: chạy N xe CHẬM (dưới V_min của vùng) ⇒ đủ <c>SpeedSlow:MinVehicles</c> xe thì vùng lên bậc chậm
    /// THEO TỐC ĐỘ. Tắt <c>SpeedSlow</c> thì trạng thái vùng không đổi — đúng thiết kế, không phải lỗi.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasZone))]
    private Task SendZoneSlowAsync() => SendZoneVehiclesAsync(ZoneSlowSpeedKmh, "chậm");

    private async Task SendZoneVehiclesAsync(int speedKmh, string label)
    {
        var zone = SelectedZone!;
        var cam = zone.Cameras.FirstOrDefault();
        if (cam is null) { Warn("Vùng này không có camera nào để bơm bản tin."); return; }

        if (!_config.Current.ZoneStatus.SpeedSlow.Enabled)
            Warn("ZoneStatus:SpeedSlow:Enabled = false ⇒ tốc độ xe KHÔNG làm đổi trạng thái vùng (chỉ cập nhật "
               + "tốc độ trung bình). Dùng nút Gọi Slow Moving / Congestion để đổi trạng thái theo sự kiện.");

        var n = Math.Max(ZoneSampleCount, 1);
        var ok = 0;

        try
        {
            for (var i = 0; i < n; i++)
            {
                // Mỗi xe một biển số khác nhau: tầng đếm chống trùng theo (biển số, camera) trong cửa sổ,
                // gửi cùng biển sẽ bị loại và chuỗi tốc độ không đủ mẫu.
                var plate = $"60A{DateTime.Now:HHmmss}{i}";
                if (await _injector.InjectAsync(VdsSampleEvents.Normal(
                        cam.CameraIp, lane: EffectiveLane, plate: plate, speedKmh: speedKmh))) ok++;
                await Task.Delay(120);
            }

            Info($"Đã bơm {ok}/{n} xe {label} ({speedKmh} km/h) qua {cam.Name ?? cam.CameraIp} "
                 + $"→ zone {zone.Display}.");
            await RefreshZoneStatusAsync();
        }
        catch (Exception ex) { Error($"Bơm xe {label} lỗi: " + ex.Message); }
    }

    /// <summary>Mẫu: gọi sự kiện <c>Congestion</c> cho vùng ⇒ nghẽn (đường DUY NHẤT ra nghẽn).</summary>
    [RelayCommand(CanExecute = nameof(HasZone))]
    private Task SendZoneCongestionAsync()
        => SendZoneEventAsync(VdsEventTypes.Congestion, _config.Current.ZoneStatus.CongestionEventTtlSeconds);

    /// <summary>Mẫu: gọi sự kiện <c>Slow Moving</c> cho vùng ⇒ chậm.</summary>
    [RelayCommand(CanExecute = nameof(HasZone))]
    private Task SendZoneSlowEventAsync()
        => SendZoneEventAsync(VdsEventTypes.SlowMoving, _config.Current.ZoneStatus.SlowEventTtlSeconds);

    private async Task SendZoneEventAsync(string eventType, int ttlSeconds)
    {
        var zone = SelectedZone!;
        var cam = zone.Cameras.FirstOrDefault();
        if (cam is null) { Warn("Vùng này không có camera nào để bơm bản tin."); return; }

        try
        {
            // Mỗi lượt một biển số mới: cửa chống trùng lượt xe (camera + loại + biển) sẽ chặn bản tin thứ hai
            // cùng biển trong cửa sổ skip, và chốt sự kiện không được gia hạn.
            var plate = $"60A{DateTime.Now:HHmmss}";
            var sent = await _injector.InjectAsync(VdsSampleEvents.Event(
                cam.CameraIp, eventType, lane: EffectiveLane, plate: plate, speedKmh: ZoneSlowSpeedKmh));

            Info(sent
                ? $"Đã gọi {eventType} trên {cam.Name ?? cam.CameraIp} → zone {zone.Display} "
                  + $"(chốt tự tắt sau {ttlSeconds}s kể từ bản tin gần nhất)."
                : $"Không bơm được bản tin {eventType}.");
            await RefreshZoneStatusAsync();
        }
        catch (Exception ex) { Error($"Gọi {eventType} lỗi: " + ex.Message); }
    }

    /// <summary>Huỷ chốt sự kiện nghẽn ngay, không chờ TTL.</summary>
    [RelayCommand(CanExecute = nameof(HasZone))]
    private Task CancelZoneCongestionAsync()
        => CancelZoneEventAsync(VdsEventTypes.Congestion, _zoneStatus.CancelCongestionEventAsync);

    /// <summary>Huỷ chốt sự kiện chậm ngay, không chờ TTL. Bậc chậm THEO TỐC ĐỘ (nếu bật) giữ nguyên.</summary>
    [RelayCommand(CanExecute = nameof(HasZone))]
    private Task CancelZoneSlowAsync()
        => CancelZoneEventAsync(VdsEventTypes.SlowMoving, _zoneStatus.CancelSlowEventAsync);

    private async Task CancelZoneEventAsync(string eventType, Func<string, CancellationToken, Task<bool>> cancel)
    {
        var zone = SelectedZone!;
        try
        {
            var ok = await cancel(zone.ZoneId, default);
            Info(ok
                ? $"Đã huỷ sự kiện {eventType} của zone {zone.Display} (đã ghi lại trạng thái)."
                : $"Zone {zone.Display}: không có gì đổi (chưa từng có sự kiện, hoặc nguồn khác vẫn giữ trạng thái).");
            await RefreshZoneStatusAsync();
        }
        catch (Exception ex) { Error($"Huỷ {eventType} lỗi: " + ex.Message); }
    }

    private bool HasZone() => SelectedZone is not null;

    /// <summary>Bậc tương ứng nhãn đang chọn; nhãn lạ (không nên xảy ra) ⇒ thông thoáng.</summary>
    private ZoneCondition SelectedZoneConditionValue =>
        ZoneConditionChoices.TryGetValue(SelectedZoneCondition ?? "", out var value)
            ? value
            : ZoneCondition.Normal;

    // ════════════════════ Lệnh: gửi thủ công ════════════════════

    /// <summary>
    /// Bắn cho tất cả camera đang tích, tuần tự từng camera, mỗi camera <see cref="SendCount"/>
    /// bản tin. Lỗi ở một camera chỉ ghi nhật ký rồi đi tiếp camera sau. Cùng biển số ở camera khác nhau không bị
    /// chống trùng (khoá chống trùng gồm cả camera).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var picked = CheckedEventCameras;
        if (picked.Count == 0) { Warn("Chưa chọn camera nào để gửi."); return; }

        var type = SelectedEventType;
        var n = Math.Max(SendCount, 1);
        var totalOk = 0;

        foreach (var cam in picked)
        {
            var speed = EffectiveSpeedKmhOf(cam.Ip);
            var ok = 0;
            try
            {
                for (var i = 0; i < n; i++)
                    if (await _injector.InjectAsync(BuildJson(type, cam.Ip, speed))) ok++;
            }
            catch (Exception ex)
            {
                Error($"Gửi [{type}] cam={cam.Display} lỗi: {ex.Message}");
            }

            totalOk += ok;
            var detail = type == VdsEventTypes.Statistic
                ? "thống kê 2 làn (mẫu cố định)"
                : $"biển={EffectivePlate} làn={EffectiveLane} tốc độ={speed}km/h "
                  + $"dài={LengthMeter?.ToString(CultureInfo.InvariantCulture) ?? "(không có)"}";
            Info($"Gửi {ok}/{n} [{type}] cam={cam.Display} {detail}");
        }

        if (picked.Count > 1)
            Info($"Xong lượt gửi [{type}]: {totalOk}/{n * picked.Count} bản tin trên {picked.Count} camera.");
    }

    private bool CanSend() => EventCameras.Any(c => c.IsChecked);

    private string EffectiveLane => string.IsNullOrWhiteSpace(Lane) ? "1" : Lane.Trim();

    private string EffectivePlate => string.IsNullOrWhiteSpace(Plate) ? "60A90685" : Plate.Trim();

    private double? LengthMeter
    {
        get
        {
            var text = LengthMeterText?.Trim();
            if (string.IsNullOrEmpty(text)) return null;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0
                ? v : null;
        }
    }

    private string BuildJson(string type, string ip, int speedKmh) => type switch
    {
        VdsEventTypes.Normal => VdsSampleEvents.Normal(ip, lane: EffectiveLane, plate: EffectivePlate,
            speedKmh: speedKmh, lengthMeter: LengthMeter),
        VdsEventTypes.Statistic => VdsSampleEvents.Statistic(ip),
        // Event: truyền biển số + tốc độ + chiều dài để kiểm soát ConditionHash, guard Speeding và cỡ xe.
        _ => VdsSampleEvents.Event(ip, type, lane: EffectiveLane, plate: EffectivePlate,
            speedKmh: speedKmh, lengthMeter: LengthMeter)
    };

    // ════════════════════ Nhật ký ════════════════════

    [RelayCommand(CanExecute = nameof(CanClearActivities))]
    private void ClearActivities()
    {
        Activities.Clear();
        ClearActivitiesCommand.NotifyCanExecuteChanged();
    }

    private bool CanClearActivities() => Activities.Count > 0;

    /// <summary>
    /// Đưa một activity lên UI, marshal về luồng UI nếu đang ở luồng nền.
    ///
    /// Bỏ qua êm khi Dispatcher đã bắt đầu shutdown: lúc đóng cửa sổ, pipeline vẫn còn log
    /// ("[VDS] Stopped.", Wolverine drain...) và <c>BeginInvoke</c> trên Dispatcher đã tắt sẽ NÉM lỗi —
    /// lỗi đó đi ngược vào lời gọi <c>ILogger</c> của pipeline và làm hỏng đường dừng.
    /// </summary>
    private void Post(Activity activity)
    {
        if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished) return;

        try
        {
            if (_dispatcher.CheckAccess()) Append(activity);
            else _dispatcher.BeginInvoke(new Action(() => Append(activity)));
        }
        catch (Exception)
        {
            // Dispatcher tắt giữa hai lệnh kiểm tra ⇒ mất một dòng nhật ký, không được lan ra ngoài.
        }
    }

    private void Append(Activity activity)
    {
        Activities.Insert(0, activity);
        while (Activities.Count > MaxActivities) Activities.RemoveAt(Activities.Count - 1);
        ClearActivitiesCommand.NotifyCanExecuteChanged();
    }

    // Thông báo của chính ViewModel ghi TRỰC TIẾP vào feed, không qua MediatR: publish rồi tự nhận lại
    // qua handler của mình sẽ khiến MediatR phải resolve chính ViewModel này ngay trong constructor
    // (LoadCameras gọi Info) — vòng phụ thuộc.
    private void Info(string detail) => Publish(detail, ActivityLevel.Success);
    private void Warn(string detail) => Publish(detail, ActivityLevel.Warning);
    private void Error(string detail) => Publish(detail, ActivityLevel.Error);

    private void Publish(string detail, ActivityLevel level)
        => Post(new Activity(BaseUtil.TimeServer.GetTimeServer(), UiStage, detail, level));

    // ════════════════════ Nạp danh mục ════════════════════

    private void LoadCameras()
    {
        // Sắp theo hướng rồi tới Km để mỗi nhóm hiển thị đúng thứ tự dọc tuyến.
        var cams = _cameras.All
            .OrderBy(c => c.Direction ?? "~")
            .ThenBy(c => c.Km ?? int.MaxValue)
            .ThenBy(c => c.Name)
            .ToList();

        // Giữ các camera đã tích ở tab sự kiện khi bấm "Tải lại danh sách camera".
        var previouslyChecked = new HashSet<string>(
            EventCameras.Where(c => c.IsChecked).Select(c => c.Ip), StringComparer.OrdinalIgnoreCase);
        foreach (var old in EventCameras) old.PropertyChanged -= OnEventCameraChanged;

        EventCameras.Clear();
        RawCameras.Clear();

        foreach (var c in cams)
        {
            var direction = _defaults.DirectionLabel(c.Direction);
            RawCameras.Add(NewCheckItem(c, direction));

            var eventItem = NewCheckItem(c, direction);
            eventItem.IsChecked = previouslyChecked.Contains(c.CameraIp);
            eventItem.PropertyChanged += OnEventCameraChanged;
            EventCameras.Add(eventItem);
        }

        BuildDirectionCounts(cams);

        OnPropertyChanged(nameof(SelectedCameraInfo));
        OnPropertyChanged(nameof(EffectiveSpeedInfo));
        SendCommand.NotifyCanExecuteChanged();

        // Constructor chạy trước InitAsync nên lần gọi đầu danh mục còn rỗng — nói rõ là "chưa nạp" thay
        // vì báo "0 thiết bị" khiến người dùng tưởng danh mục trống thật.
        if (cams.Count == 0)
        {
            Info("Danh mục camera chưa nạp xong; sẽ tự hiện khi khởi động hoàn tất.");
            return;
        }

        var byDirection = cams
            .GroupBy(c => _defaults.DirectionLabel(c.Direction))
            .Select(g => $"{g.Key}: {g.Count()}");
        Info($"Danh mục camera: {cams.Count} thiết bị ({string.Join(", ", byDirection)}).");
    }

    /// <summary>
    /// Một dòng cho mỗi hướng tuyến CÓ KHAI <c>DirectionId</c>. Camera chưa khai hướng không thể nhân bản
    /// theo hướng (không biết thuộc hướng nào) nên không sinh dòng — chúng vẫn sinh raw như bình thường.
    /// Giữ lại lựa chọn cũ khi nạp lại danh mục để người dùng không phải chọn lại.
    /// </summary>
    private void BuildDirectionCounts(IReadOnlyList<CameraInfo> cams)
    {
        var previous = DirectionCounts.ToDictionary(d => d.DirectionId, d => d.Selected?.Ip, StringComparer.OrdinalIgnoreCase);

        var groups = cams
            .Where(c => !string.IsNullOrWhiteSpace(c.Direction))
            .GroupBy(c => c.Direction!.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        DirectionCounts.Clear();
        foreach (var g in groups)
        {
            var options = g
                .OrderBy(c => c.Km ?? int.MaxValue)
                .Select(c => new CameraOption(c.CameraIp, Label(c), _defaults.DirectionLabel(c.Direction), c))
                .ToList();

            var item = new DirectionCountOption
            {
                DirectionId = g.Key,
                DirectionName = _defaults.DirectionLabel(g.Key),
                Cameras = options
            };

            if (previous.TryGetValue(g.Key, out var keepIp) && keepIp is not null)
                item.Selected = options.FirstOrDefault(o => string.Equals(o.Ip, keepIp, StringComparison.OrdinalIgnoreCase));

            DirectionCounts.Add(item);
        }
    }

    private static string Label(CameraInfo c) => $"{c.Name ?? "?"}  [{c.CameraIp}]  Km{c.Km}+{c.Met}";

    private static CameraCheckItem NewCheckItem(CameraInfo c, string directionName) => new()
    {
        Ip = c.CameraIp,
        EquipmentId = c.EquipmentId,
        Display = $"{c.Name ?? "?"}  [{c.CameraIp}]  Km{c.Km}+{c.Met}",
        DirectionId = c.Direction,
        DirectionName = directionName
    };

    // ════════════════════ Nạp danh mục vùng ════════════════════

    /// <summary>
    /// Danh sách vùng của tab Zone = các vùng mà CHÍNH các camera VDS đang cấu hình map tới
    /// (<c>Catalog:CameraCodes</c> → <c>ZoneManager</c> → <c>TmsZoneEquipment</c>). Không liệt kê toàn bộ
    /// <c>TmsZone</c>: vùng không có camera VDS thì harness không bơm được bản tin nào vào.
    /// </summary>
    /// <param name="keepSelection">Giữ vùng đang chọn nếu vẫn còn trong danh sách mới (dùng khi danh mục nạp lại).</param>
    private async Task LoadZonesAsync(bool keepSelection = false)
    {
        // Chưa có danh mục camera (constructor chạy trước InitAsync) ⇒ chưa suy được vùng nào; bỏ qua êm,
        // App sẽ gọi lại RefreshCatalog() sau khi Init xong.
        if (_cameras.Count == 0) return;

        var previousZoneId = SelectedZone?.ZoneId;

        try
        {
            var byZone = new Dictionary<string, List<CameraInfo>>(StringComparer.OrdinalIgnoreCase);
            foreach (var cam in _cameras.All)
                foreach (var zoneId in _zoneCatalog.ZonesOf(cam.EquipmentId).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!byZone.TryGetValue(zoneId, out var list)) byZone[zoneId] = list = new List<CameraInfo>();
                    // Danh mục dev có dòng TmsZoneEquipment trùng ⇒ lọc để camera không hiện hai lần.
                    if (list.All(c => !string.Equals(c.EquipmentId, cam.EquipmentId, StringComparison.OrdinalIgnoreCase)))
                        list.Add(cam);
                }

            // Tên vùng lấy từ TmsZone để nhìn được trên UI; thiếu tên thì hiển thị ID.
            var names = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (byZone.Count > 0)
                foreach (var row in await _catalog.QueryZonesAsync(byZone.Keys.ToList()))
                    names[row.Id] = string.IsNullOrWhiteSpace(row.Name) ? row.Code : row.Name;

            var items = byZone
                .Select(kv => new ZoneOption(
                    kv.Key,
                    names.TryGetValue(kv.Key, out var name) && !string.IsNullOrWhiteSpace(name)
                        ? $"{name}  ({kv.Value.Count} cam)"
                        : $"{kv.Key}  ({kv.Value.Count} cam)",
                    kv.Value.OrderBy(c => c.Km ?? int.MaxValue).ToList()))
                .OrderBy(z => z.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _dispatcher.Invoke(() =>
            {
                Zones.Clear();
                foreach (var item in items) Zones.Add(item);
                var keepId = keepSelection ? previousZoneId : null;
                SelectedZone = (keepId is null ? null : Zones.FirstOrDefault(z => string.Equals(z.ZoneId, keepId, StringComparison.OrdinalIgnoreCase)))
                               ?? Zones.FirstOrDefault();
            });

            Info(items.Count == 0
                ? $"Không có vùng nào: {_cameras.Count} camera VDS trong cấu hình chưa map TmsZoneEquipment."
                : $"Danh mục vùng: {items.Count} vùng từ {_cameras.Count} camera VDS.");

            await RefreshZoneStatusAsync();
        }
        catch (Exception ex) { Error("Nạp danh mục vùng lỗi: " + ex.Message); }
    }

    /// <summary>Đọc trạng thái hiện tại của vùng đang chọn để hiện lên UI (không ghi gì).</summary>
    private async Task RefreshZoneStatusAsync()
    {
        var zone = SelectedZone;
        if (zone is null) { ZoneStatusInfo = ""; return; }

        try
        {
            var row = await _diagnostics.FindZoneStatusAsync(zone.ZoneId);
            ZoneStatusInfo = row is null
                ? $"TmsZoneStatus: chưa có dòng cho zone {zone.ZoneId}"
                : $"TmsZoneStatus: condition={row.Condition ?? "-"} · avgSpeed={row.AverageSpeed ?? "-"} "
                  + $"· updateTime={row.UpdateTime:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            ZoneStatusInfo = "Không đọc được TmsZoneStatus: " + ex.Message;
        }
    }
}

/// <summary>Một vùng trong tab Zone: ID, nhãn hiển thị và các camera VDS thuộc vùng đó.</summary>
public sealed record ZoneOption(string ZoneId, string Display, IReadOnlyList<CameraInfo> Cameras);
