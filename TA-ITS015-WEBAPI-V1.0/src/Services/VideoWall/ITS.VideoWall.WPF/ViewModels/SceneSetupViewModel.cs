using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using VwDirectDeviceConstants = Module.VideoWall.WPF.Api.Direct.Isapi.VwDirectDeviceConstants;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Controls;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.Storage;
using Services.Shared.Events;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Description: ViewModel thiết lập kịch bản hiển thị, phân bổ màn hình và bố cục cửa sổ VideoWall
/// Created date: 13/09/2026
/// </summary>
public sealed partial class SceneSetupViewModel : ObservableObject
{
    private readonly ActivityPublisher _activityPublisher;
    private readonly ConnectionViewModel _connection;
    private readonly IUserConfirmation _userConfirmation;
    private readonly IPublisher? _publisher;

    public SceneSetupViewModel(
        ActivityPublisher activityPublisher,
        ConnectionViewModel connection,
        IUserConfirmation userConfirmation,
        IPublisher? publisher = null)
    {
        _activityPublisher = activityPublisher;
        _connection = connection;
        _userConfirmation = userConfirmation;
        _publisher = publisher;

        _wallNo = _connection.WallNo;

        _connection.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConnectionViewModel.WallNo))
            {
                if (WallNo != _connection.WallNo)
                    WallNo = _connection.WallNo;
                ReloadScenesForCurrentWall();
                PushToDeviceCommand.NotifyCanExecuteChanged();
                PushWindowsOnlyToDeviceCommand.NotifyCanExecuteChanged();
                ActivateSceneCommand.NotifyCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(ConnectionViewModel.AdHocIp))
            {
                PopulateSources(GetDeviceSourceKey(), _connection.ProbeResult?.InputChannels);
            }
            else if (e.PropertyName is nameof(ConnectionViewModel.ProbeResult) or nameof(ConnectionViewModel.IsConnected))
            {
                SyncFromProbeResult();
                PushToDeviceCommand.NotifyCanExecuteChanged();
                PushWindowsOnlyToDeviceCommand.NotifyCanExecuteChanged();
                ActivateSceneCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(SceneCountSummary));
                OnPropertyChanged(nameof(DeviceCapabilitySummary));
            }
        };

        Scenes.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasScenes));
            OnPropertyChanged(nameof(SceneCount));
            OnPropertyChanged(nameof(SceneCountSummary));
            OnPropertyChanged(nameof(DeviceCapabilitySummary));
        };
        ScreenAssignments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasScreenAssignments));
        WindowRows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasWindowRows));
        SceneWindows.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSceneWindows));
            AddSceneWindowCommand.NotifyCanExecuteChanged();
            SaveAllSceneWindowsCommand.NotifyCanExecuteChanged();
            PushToDeviceCommand.NotifyCanExecuteChanged();
            PushWindowsOnlyToDeviceCommand.NotifyCanExecuteChanged();
            DeleteSelectedSceneWindowsCommand.NotifyCanExecuteChanged();
        };
        SceneWindowRow.StartScreenProvider = () => AvailableStartScreens.ToList();
        InitializeDefaultData();
    }

    /// <summary>Danh sách các nguồn tín hiệu (VwSourceDto) có sẵn.</summary>
    public ObservableCollection<VwSourceDto> Sources { get; } = [];

    /// <summary>Danh sách các màn hình vật lý làm điểm bắt đầu (StartScreenPreset).</summary>
    public ObservableCollection<StartScreenPreset> AvailableStartScreens { get; } = [];

    /// <summary>Danh sách ánh xạ gán nguồn cho từng màn hình đơn lẻ.</summary>
    public ObservableCollection<ScreenAssignmentRow> ScreenAssignments { get; } = [];

    /// <summary>Danh sách hàng cửa sổ trong kịch bản (WindowSceneRow).</summary>
    public ObservableCollection<WindowSceneRow> WindowRows { get; } = [];

    /// <summary>Danh sách các cửa sổ con trực quan trong kịch bản (SceneWindowRow).</summary>
    public ObservableCollection<SceneWindowRow> SceneWindows { get; } = [];

    /// <summary>Danh sách kịch bản hiển thị (VwSceneDto).</summary>
    public ObservableCollection<VwSceneDto> Scenes { get; } = [];

    /// <summary>Cờ kiểm tra có kịch bản nào trong danh sách hay không.</summary>
    public bool HasScenes => Scenes.Count > 0;

    /// <summary>Số lượng kịch bản hiển thị hiện có.</summary>
    public int SceneCount => Scenes.Count;

    /// <summary>Chuỗi tổng hợp số lượng kịch bản hiện có so với giới hạn của thiết bị.</summary>
    public string SceneCountSummary =>
        _connection.ProbeMaxSceneNums > 0
            ? $"{Scenes.Count} / {_connection.ProbeMaxSceneNums} Kịch bản"
            : $"{Scenes.Count} Kịch bản";

    /// <summary>Chuỗi tổng hợp năng lực hiển thị của thiết bị (số cửa sổ tối đa, số kịch bản).</summary>
    public string DeviceCapabilitySummary =>
        _connection.HasProbeResult
            ? $"Tối đa {_connection.ProbeMaxWindowNums} cửa sổ · {Scenes.Count} kịch bản hiện có"
            : "(Chưa khảo sát)";

    /// <summary>Cờ kiểm tra có cấu hình gán màn hình nào hay không.</summary>
    public bool HasScreenAssignments => ScreenAssignments.Count > 0;

    /// <summary>Cờ kiểm tra có cấu hình hàng cửa sổ nào hay không.</summary>
    public bool HasWindowRows => WindowRows.Count > 0;

    /// <summary>Cờ kiểm tra có cửa sổ hiển thị trực quan nào hay không.</summary>
    public bool HasSceneWindows => SceneWindows.Count > 0;

    /// <summary>Cửa sổ kịch bản đang được chọn trên giao diện canvas hoặc danh sách.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedSceneWindowsCommand))]
    private SceneWindowRow? _selectedSceneWindow;

    partial void OnSelectedSceneWindowChanged(SceneWindowRow? value)
    {
        foreach (var row in SceneWindows)
        {
            row.IsActiveSelected = (row == value);
        }
        DeleteSelectedSceneWindowsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Cờ chọn hoặc bỏ chọn toàn bộ các cửa sổ kịch bản.</summary>
    [ObservableProperty]
    private bool _isAllSceneWindowsSelected;

    partial void OnIsAllSceneWindowsSelectedChanged(bool value)
    {
        foreach (var row in SceneWindows)
            row.IsSelected = value;
        DeleteSelectedSceneWindowsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Cờ bật hoặc tắt tính năng bắt dính lưới toạ độ khi kéo thả cửa sổ.</summary>
    [ObservableProperty]
    private bool _isSnapToGridEnabled = true;

    /// <summary>
    /// Description: Kích thước ô cơ sở (tile) hiệu dụng đọc từ BaseOutputSize của thiết bị, fallback 1920 khi &lt;= 0.
    /// Created date: 08/09/2026
    /// </summary>
    public int Tile => VwDirectDeviceConstants.EffectiveTile(_connection.ProbeResult?.BaseOutputSize);

    /// <summary>Tổng chiều rộng của toàn bộ tường màn hình ghép (pixel).</summary>
    public int TotalWallWidth
    {
        get
        {
            if (AvailableStartScreens.Count > 0)
            {
                var maxX = AvailableStartScreens.Max(s => s.X + Tile);
                return Math.Max(Tile, maxX);
            }
            if (_connection.ProbeResult?.Outputs is { Count: > 0 } outputs)
            {
                var maxX = outputs.Max(o => (o.Rect?.Coordinate?.X ?? 0) + (o.Rect?.Width ?? Tile));
                return Math.Max(Tile, maxX);
            }
            return (GridCols > 0 ? GridCols.Value : 8) * Tile;
        }
    }

    /// <summary>Tổng chiều cao của toàn bộ tường màn hình ghép (pixel).</summary>
    public int TotalWallHeight
    {
        get
        {
            if (AvailableStartScreens.Count > 0)
            {
                var maxY = AvailableStartScreens.Max(s => s.Y + Tile);
                return Math.Max(Tile, maxY);
            }
            if (_connection.ProbeResult?.Outputs is { Count: > 0 } outputs)
            {
                var maxY = outputs.Max(o => (o.Rect?.Coordinate?.Y ?? 0) + (o.Rect?.Height ?? Tile));
                return Math.Max(Tile, maxY);
            }
            return (GridRows > 0 ? GridRows.Value : 4) * Tile;
        }
    }

    public void SelectWindowFromCanvas(SceneWindowRow? window)
    {
        SelectedSceneWindow = window;
    }

    /// <summary>Tên kịch bản hiển thị trong form thiết lập.</summary>
    [ObservableProperty]
    private string _sceneName = string.Empty;

    /// <summary>Mã ID kênh đầu ra hoặc số thứ tự kịch bản trên thiết bị.</summary>
    [ObservableProperty]
    private string _sceneOutputId = "1";

    /// <summary>Số cột ma trận lưới của tường ghép.</summary>
    [ObservableProperty]
    private int? _gridCols = 8;

    /// <summary>Số hàng ma trận lưới của tường ghép.</summary>
    [ObservableProperty]
    private int? _gridRows = 4;

    /// <summary>Số thứ tự WallNo của tường màn hình.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PushToDeviceCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushWindowsOnlyToDeviceCommand))]
    private int? _wallNo;

    /// <summary>Cờ trạng thái đang tạo mới kịch bản (khác với chỉnh sửa kịch bản có sẵn).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSceneSummary))]
    [NotifyPropertyChangedFor(nameof(IsNotCreatingNewScene))]
    private bool _isCreatingNewScene;

    /// <summary>Cờ trạng thái không phải đang tạo kịch bản mới.</summary>
    public bool IsNotCreatingNewScene => !IsCreatingNewScene;

    private VwSceneDto? _previousSelectedScene;

    /// <summary>Cờ chế độ hiển thị từng màn hình riêng lẻ (Individual Mode).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverlappingMode))]
    private bool _isIndividualMode = true;

    /// <summary>Kịch bản hiển thị đang được chọn để làm việc.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddSceneWindowCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyIndividualWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyOverlappingWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadSceneWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSceneWindowCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedSceneWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateSceneWindowCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveAllSceneWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushToDeviceCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushWindowsOnlyToDeviceCommand))]
    private VwSceneDto? _currentScene;

    /// <summary>Cờ kiểm tra kịch bản giả lập (DryRun) không gửi lệnh điều khiển thật xuống thiết bị.</summary>
    [ObservableProperty]
    private bool _dryRun;

    /// <summary>Cờ tự động xóa trắng các cửa sổ cũ trước khi đẩy cấu hình mới xuống thiết bị.</summary>
    [ObservableProperty]
    private bool _resetWindows = true;

    /// <summary>Cờ đồng bộ danh sách nguồn tín hiệu xuống thiết bị.</summary>
    [ObservableProperty]
    private bool _syncSources;

    /// <summary>Cờ tự động kích hoạt kịch bản ngay sau khi đẩy cấu hình xuống thiết bị thành công.</summary>
    [ObservableProperty]
    private bool _activateAfterPush = true;

    /// <summary>Thông báo trạng thái thực thi trên giao diện thiết lập kịch bản.</summary>
    [ObservableProperty]
    private string _statusMessage = "Nhập IP thiết bị ở thanh trên cùng rồi bấm 'Kết nối & Khảo sát'.";

    /// <summary>Kịch bản hiển thị đang được kích hoạt chiếu trên tường thiết bị.</summary>
    [ObservableProperty]
    private VwSceneDto? _activeScene;

    /// <summary>Cờ trạng thái đang bận xử lý tác vụ thiết lập kịch bản.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadScreensAndSourcesCommand))]
    [NotifyCanExecuteChangedFor(nameof(SeedSampleScenesCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddSceneWindowCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyIndividualWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyOverlappingWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadSceneWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ActivateSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSceneCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSceneWindowCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedSceneWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateSceneWindowCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveAllSceneWindowsCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushToDeviceCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushWindowsOnlyToDeviceCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenSourceManagementCommand))]
    private bool _isBusy;

    /// <summary>Tham chiếu đến ViewModel kết nối và khảo sát thiết bị.</summary>
    public ConnectionViewModel Connection => _connection;

    /// <summary>Danh sách các cổng ra vật lý ISAPI đọc được từ thiết bị.</summary>
    public ObservableCollection<VwISAPIOutputItem> AvailableOutputs { get; } = [];

    /// <summary>Cổng ra ISAPI đang được chọn để gán màn hình.</summary>
    [ObservableProperty]
    private VwISAPIOutputItem? _selectedOutputChannel;

    /// <summary>Chỉ số cột lưới khi thêm màn hình mới.</summary>
    [ObservableProperty]
    private int? _newScreenGridCol = 0;

    /// <summary>Chỉ số hàng lưới khi thêm màn hình mới.</summary>
    [ObservableProperty]
    private int? _newScreenGridRow = 0;

    /// <summary>Chiều rộng màn hình mới thêm (pixel).</summary>
    [ObservableProperty]
    private int? _newScreenWidth = 1920;

    /// <summary>Chiều cao màn hình mới thêm (pixel).</summary>
    [ObservableProperty]
    private int? _newScreenHeight = 1080;

    /// <summary>Hàng cấu hình gán màn hình đang được chọn.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveScreenAssignmentCommand))]
    private ScreenAssignmentRow? _selectedScreenAssignment;

    /// <summary>Cờ chế độ cửa sổ phủ đa màn hình (Overlapping Mode).</summary>
    public bool IsOverlappingMode
    {
        get => !IsIndividualMode;
        set => IsIndividualMode = !value;
    }

    /// <summary>Chuỗi tổng hợp trạng thái kịch bản đang chọn hoặc đang tạo mới.</summary>
    public string CurrentSceneSummary =>
        IsCreatingNewScene
            ? "📝 Đang tạo kịch bản mới (nhập thông số bên dưới rồi bấm \"💾 Lưu\")"
            : CurrentScene is null
                ? "Chưa chọn kịch bản nào."
                : $"Đang chọn: {CurrentScene.Name}";

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task LoadScreensAndSources(CancellationToken cancellationToken)
    {
        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";

        if (_connection.ProbeResult == null)
        {
            StatusMessage = "Bấm Probe trước khi nạp nguồn tín hiệu.";
            return;
        }

        await Run("Đang nạp màn hình và nguồn tín hiệu...", async () =>
        {
            // PHẦN 1: Sources từ ProbeResult.InputChannels + VwLocalSourceStore
            PopulateSources(GetDeviceSourceKey(), _connection.ProbeResult?.InputChannels);

            // PHẦN 2: Screens từ VwLocalScreenStore
            var savedScreens = VwLocalScreenStore.Load(deviceKey);
            var availableOutputs = _connection.ProbeResult?.Outputs ?? [];
            var availableOutputIds = availableOutputs.Select(o => o.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            AvailableOutputs.Clear();
            foreach (var o in availableOutputs)
            {
                AvailableOutputs.Add(o);
            }

            ScreenAssignments.Clear();
            var missingOutputs = 0;
            foreach (var screen in savedScreens.OrderBy(s => s.GridRow).ThenBy(s => s.GridCol))
            {
                if (screen.OutPutPort.HasValue && !availableOutputIds.Contains(screen.OutPutPort.Value.ToString()))
                    missingOutputs++;

                var row = new ScreenAssignmentRow(screen);
                if (row.SelectedSource == null && Sources.Count > 0)
                {
                    row.SelectedSource = Sources[0];
                }
                ScreenAssignments.Add(row);
            }

            if (ScreenAssignments.Count > 0 && SelectedScreenAssignment == null)
            {
                SelectedScreenAssignment = ScreenAssignments[0];
            }

            if (AvailableOutputs.Count > 0 && (SelectedOutputChannel == null || !AvailableOutputs.Any(o => o.Id == SelectedOutputChannel.Id)))
            {
                SelectedOutputChannel = AvailableOutputs[0];
            }

            if (missingOutputs > 0)
            {
                _activityPublisher.Publish(
                    "Screen",
                    $"Có {missingOutputs} màn hình đã lưu gắn với cổng ra không tồn tại trong kết quả Probe hiện tại.",
                    ActivityLevel.Warning);
            }

            // PHẦN 3: Scenes từ VwLocalSceneStore
            var savedScenes = VwLocalSceneStore.ListScenes(deviceKey);
            Scenes.Clear();
            foreach (var s in savedScenes)
                Scenes.Add(s);

            ActiveScene = VwLocalSceneStore.GetActiveScene(deviceKey);
            if (CurrentScene == null && Scenes.Count > 0)
            {
                CurrentScene = Scenes.FirstOrDefault(s => s.ID == ActiveScene?.ID) ?? Scenes[0];
            }

            StatusMessage = $"Đã nạp {ScreenAssignments.Count} màn hình, {Sources.Count} nguồn tín hiệu, {Scenes.Count} kịch bản.";
            await Task.CompletedTask;
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanSeedSampleScenes))]
    private async Task SeedSampleScenes(CancellationToken cancellationToken)
    {
        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        var wallNo = _connection.WallNo ?? 1;
        await Run("Đang nạp kịch bản mẫu chuẩn...", async () =>
        {
            var seeded = VwLocalSceneStore.SeedSampleScenes(deviceKey, wallNo, availableSources: Sources.ToList());

            var savedScenes = VwLocalSceneStore.ListScenes(deviceKey);
            Scenes.Clear();
            foreach (var s in savedScenes)
                Scenes.Add(s);

            CurrentScene = Scenes.FirstOrDefault(s => s.Code == $"VWSCENE_SAMPLE_W{wallNo}_01")
                ?? Scenes.FirstOrDefault(s => s.Code == "VWSCENE_SAMPLE_01")
                ?? (Scenes.Count > 0 ? Scenes[0] : null);
            StatusMessage = $"Đã nạp Kịch bản Chuẩn: Giám sát Toàn tuyến ({Scenes.Count} kịch bản).";
            _activityPublisher.Publish("Scene", StatusMessage, ActivityLevel.Info);
            await Task.CompletedTask;
        }).ConfigureAwait(true);
    }

    private bool CanSeedSampleScenes() => !IsBusy;

    [RelayCommand]
    private void AddScreenAssignment()
    {
        if (SelectedOutputChannel is null)
        {
            StatusMessage = "Vui lòng chọn cổng ra (Output) từ danh sách Probe.";
            return;
        }

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        var screens = VwLocalScreenStore.Load(deviceKey);
        var newScreen = new VwScreenDto
        {
            ID = Guid.NewGuid().ToString("N"),
            Name = $"Màn hình {SelectedOutputChannel.Id}",
            OutPutPort = SelectedOutputChannel.Id,
            GridCol = NewScreenGridCol ?? 0,
            GridRow = NewScreenGridRow ?? 0,
            WidthPx = (NewScreenWidth ?? 1920).ToString(),
            HeightPx = (NewScreenHeight ?? 1080).ToString(),
            ControllerId = deviceKey,
        };

        screens.Add(newScreen);
        VwLocalScreenStore.Save(deviceKey, screens);
        var addedRow = new ScreenAssignmentRow(newScreen);
        if (addedRow.SelectedSource == null && Sources.Count > 0)
        {
            addedRow.SelectedSource = Sources[0];
        }
        ScreenAssignments.Add(addedRow);
        SelectedScreenAssignment = addedRow;

        var addedOutputId = SelectedOutputChannel.Id;
        if (AvailableOutputs.Count > 0)
        {
            var currentIndex = AvailableOutputs.IndexOf(SelectedOutputChannel);
            if (currentIndex >= 0 && currentIndex + 1 < AvailableOutputs.Count)
            {
                SelectedOutputChannel = AvailableOutputs[currentIndex + 1];
            }
        }
        NewScreenGridCol = (NewScreenGridCol ?? 0) + 1;

        StatusMessage = $"Đã thêm màn hình cho cổng ra {addedOutputId}.";
    }

    [RelayCommand(CanExecute = nameof(CanRemoveScreenAssignment))]
    private void RemoveScreenAssignment()
    {
        if (SelectedScreenAssignment is null)
        {
            StatusMessage = "Vui lòng chọn màn hình cần xoá khỏi danh sách.";
            return;
        }

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        var screens = VwLocalScreenStore.Load(deviceKey);
        screens.RemoveAll(s => s.ID == SelectedScreenAssignment.Screen.ID);
        VwLocalScreenStore.Save(deviceKey, screens);
        ScreenAssignments.Remove(SelectedScreenAssignment);
        StatusMessage = "Đã xoá màn hình khỏi cấu hình cục bộ.";
    }

    private bool CanRemoveScreenAssignment() => SelectedScreenAssignment is not null;

    [RelayCommand]
    private void StartCreateNewScene()
    {
        _previousSelectedScene = CurrentScene;
        IsCreatingNewScene = true;
        SceneName = string.Empty;
        SceneOutputId = (Scenes.Count + 1).ToString();
        GridCols = null;
        GridRows = null;
        SceneWindows.Clear();
        StatusMessage = "Nhập tên kịch bản mới rồi bấm \"💾 Lưu kịch bản\" (hoặc bấm \"❌ Huỷ bỏ\" để quay lại).";
    }

    [RelayCommand]
    private void CancelCreateNewScene()
    {
        IsCreatingNewScene = false;
        CurrentScene = _previousSelectedScene ?? Scenes.FirstOrDefault();
        if (CurrentScene is not null && !string.IsNullOrWhiteSpace(CurrentScene.ID))
        {
            SceneName = CurrentScene.Name ?? string.Empty;
            SceneOutputId = CurrentScene.OutputId ?? "1";
            GridCols = CurrentScene.GridCols ?? 8;
            GridRows = CurrentScene.GridRows ?? 4;
            ReloadSceneWindows(CurrentScene.ID);
        }
        StatusMessage = "Đã huỷ tạo mới, quay lại kịch bản trước đó.";
    }

    [RelayCommand(CanExecute = nameof(CanCreateScene))]
    private async Task CreateScene(CancellationToken cancellationToken)
    {
        var wasCreatingNew = IsCreatingNewScene;
        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";

        int nextSid = 1;
        var existingSids = Scenes.Select(s => int.TryParse(s.OutputId, out var parsed) ? parsed : 0).Where(x => x > 0).ToHashSet();
        while (existingSids.Contains(nextSid))
        {
            nextSid++;
        }

        var assignedSid = string.IsNullOrWhiteSpace(SceneOutputId) ? nextSid.ToString() : SceneOutputId.Trim();

        var scene = new VwSceneDto
        {
            Name = SceneName.Trim(),
            ControllerId = deviceKey,
            OutputId = assignedSid,
            GridCols = GridCols,
            GridRows = GridRows,
            Status = VwStatus.Enable,
            ActiveScene = VwActiveScene.DeActivate,
        };

        var maxScenes = _connection.ProbeResult?.MaxSceneNums ?? 0;
        if (maxScenes > 0 && int.TryParse(scene.OutputId, out var sid) && sid > maxScenes)
        {
            StatusMessage = $"⚠️ SID {sid} vượt quá giới hạn thiết bị (tối đa {maxScenes}). Sửa lại SID rồi tạo lại.";
            return;
        }

        await Run($"Đang tạo kịch bản {scene.Name}...", async () =>
        {
            string? deviceSyncNote = null;

            var wallNo = WallNo ?? _connection.WallNo ?? _connection.ProbeResult?.BoundWallNo ?? _connection.ProbeResult?.WallNo;
            var isapi = _connection.BuildDirectISAPIClient();

            if (isapi != null && wallNo.HasValue && int.TryParse(scene.OutputId, out var targetSid))
            {
                var setRes = await isapi.SetScene(wallNo.Value, targetSid, scene.Name, cancellationToken);
                if (!setRes.Success)
                {
                    if (setRes.RawResponse?.Contains("invalidOperation", StringComparison.OrdinalIgnoreCase) == true
                        || setRes.ErrorMessage?.Contains("invalidOperation", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var postRes = await isapi.CreateScene(wallNo.Value, scene.Name, cancellationToken);
                        if (postRes.Success && postRes.Data?.Id > 0)
                        {
                            scene.OutputId = postRes.Data.Id.ToString();
                            if (int.TryParse(scene.OutputId, out var createdSid))
                            {
                                await isapi.SaveSceneData(wallNo.Value, createdSid, cancellationToken);
                            }
                            deviceSyncNote = $"Đã lưu kịch bản lên phần cứng thiết bị (SID {scene.OutputId}).";
                        }
                        else
                        {
                            deviceSyncNote = "Thiết bị phản hồi Invalid Operation (Mã 4) do kịch bản chưa được cấp phát hoặc tường unbound. Đã lưu kịch bản vào bộ nhớ ứng dụng.";
                        }
                    }
                    else
                    {
                        deviceSyncNote = $"Chưa thể tạo trên phần cứng ({setRes.ErrorMessage}). Đã lưu vào bộ nhớ ứng dụng.";
                    }
                }
                else
                {
                    await isapi.SaveSceneData(wallNo.Value, targetSid, cancellationToken);
                    deviceSyncNote = $"Đã lưu kịch bản lên phần cứng thiết bị (SID {targetSid}).";
                }
            }
            else if (_connection.IsConnected && !wallNo.HasValue)
            {
                StatusMessage = "⚠️ Cần nhập WallNo trước khi tạo kịch bản trên thiết bị.";
                return;
            }

            var created = VwLocalSceneStore.AddScene(deviceKey, scene);
            if (wasCreatingNew && SceneWindows.Count > 0 && !string.IsNullOrWhiteSpace(created.ID))
            {
                foreach (var w in SceneWindows)
                {
                    w.Window.SceneId = created.ID;
                    w.SyncSubWindowsToDto();
                }
                VwLocalSceneStore.SaveWindowScenes(deviceKey, created.ID, SceneWindows.Select(r => r.Window));
            }

            Scenes.Add(created);
            IsCreatingNewScene = false;
            CurrentScene = created;
            OnPropertyChanged(nameof(CurrentSceneSummary));
            StatusMessage = $"Đã lưu kịch bản \"{created.Name}\" thành công ({SceneWindows.Count} ô camera). {(deviceSyncNote != null ? deviceSyncNote + " " : "")}";
        }).ConfigureAwait(true);
    }

    public int GetMaxAllowedWindows()
    {
        if (_connection.ProbeResult?.MaxWindowNums is int maxCap and > 0)
            return maxCap;

        return 16;
    }

    private bool CanAddSceneWindow() => !IsBusy && !string.IsNullOrWhiteSpace(CurrentScene?.ID) && SceneWindows.Count < GetMaxAllowedWindows();

    [RelayCommand(CanExecute = nameof(CanAddSceneWindow))]
    private void AddSceneWindow()
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
        {
            StatusMessage = "Vui lòng chọn hoặc lưu kịch bản trước khi thêm ô camera.";
            return;
        }

        var maxWindows = GetMaxAllowedWindows();
        if (SceneWindows.Count >= maxWindows)
        {
            StatusMessage = $"⚠️ Số lượng ô camera ({SceneWindows.Count}) đã đạt tối đa giới hạn của thiết bị ({maxWindows} cửa sổ).";
            return;
        }

        var nextOrder = SceneWindows.Count + 1;
        StartScreenPreset? startScreen = null;
        int x = 0, y = 0;
        int zIndex = 1;

        if (AvailableStartScreens.Count > 0)
        {
            var screenIndex = (nextOrder - 1) % AvailableStartScreens.Count;
            startScreen = AvailableStartScreens[screenIndex];
            x = startScreen.X;
            y = startScreen.Y;
            var layer = (nextOrder - 1) / AvailableStartScreens.Count + 1;
            zIndex = layer;
        }
        else
        {
            var cols = GridCols > 0 ? GridCols.Value : 8;
            var col = (nextOrder - 1) % cols;
            var row = (nextOrder - 1) / cols;
            x = col * Tile;
            y = row * Tile;
        }

        if (Sources.Count == 0)
            PopulateSources(GetDeviceSourceKey(), _connection.ProbeResult?.InputChannels);

        var defaultSource = Sources.Count > 0 ? Sources[(nextOrder - 1) % Sources.Count] : null;

        var window = new VwWindowSceneDto
        {
            ID = Guid.NewGuid().ToString("N"),
            Name = $"Ô {nextOrder}",
            Label = startScreen != null
                ? (zIndex > 1 ? $"Ô {nextOrder} ({startScreen.Name} · Lớp {zIndex})" : $"Ô {nextOrder} ({startScreen.Name})")
                : $"Ô {nextOrder}",
            SceneId = sceneId,
            SourceId = defaultSource?.ID ?? "1",
            X = x,
            Y = y,
            W = Tile,
            H = Tile,
            ZIndex = zIndex,
            OrderNo = nextOrder,
            Visible = VwWindowVisible.Visible,
        };

        var rowVm = new SceneWindowRow(window)
        {
            SelectedSource = defaultSource
        };
        rowVm.InitSubCells(Sources);
        AttachSceneWindowRowHandlers(rowVm);
        SceneWindows.Add(rowVm);
        SelectedSceneWindow = rowVm;
        PopulateReadSurveyInfo();

        DeleteSelectedSceneWindowsCommand.NotifyCanExecuteChanged();
        SaveAllSceneWindowsCommand.NotifyCanExecuteChanged();
        AddSceneWindowCommand.NotifyCanExecuteChanged();
        PushToDeviceCommand.NotifyCanExecuteChanged();
        PushWindowsOnlyToDeviceCommand.NotifyCanExecuteChanged();
        StatusMessage = $"Đã thêm \"{window.Label}\" vào kịch bản (chưa lưu/áp dụng).";

        if (!DryRun && _connection.IsConnected)
            TriggerImmediateLiveWindowUpdate(rowVm);
    }

    [RelayCommand]
    private void ApplyBigCenterTemplate()
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
        {
            StatusMessage = "Vui lòng chọn kịch bản trước khi áp dụng bố cục.";
            return;
        }

        ApplyBigCenterTemplateInternal(sceneId);
        StatusMessage = $"Đã áp dụng mẫu Trọng điểm / Sự cố (Khối 2×2 + 8 ô) cho kịch bản \"{CurrentScene?.Name}\" (chưa lưu/áp dụng).";
    }

    [RelayCommand]
    private void ApplyDual2x2Template()
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
        {
            StatusMessage = "Vui lòng chọn kịch bản trước khi áp dụng bố cục.";
            return;
        }

        ApplyDual2x2TemplateInternal(sceneId);
        StatusMessage = $"Đã áp dụng mẫu Song song 2 địa bàn Hữu Nghị - Chi Lăng (2 Khối 2×2 + 4 ô) cho kịch bản \"{CurrentScene?.Name}\" (chưa lưu/áp dụng).";
    }

    [RelayCommand]
    private void ApplyFullWallTemplate()
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
        {
            StatusMessage = "Vui lòng chọn kịch bản trước khi áp dụng bố cục.";
            return;
        }

        ApplyFullWallTemplateInternal(sceneId);
        StatusMessage = $"Đã áp dụng mẫu Toàn cảnh Bản đồ GIS (4 × 3 Toàn tường) cho kịch bản \"{CurrentScene?.Name}\" (chưa lưu/áp dụng).";
    }

    [RelayCommand]
    private void ResetSceneWindows()
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
            return;

        ReloadSceneWindows(sceneId);
        StatusMessage = $"Đã khôi phục bố cục đã lưu của kịch bản \"{CurrentScene?.Name}\".";
    }

    private static List<StartScreenPreset> BuildFallbackScreens(int cols, int rows, int tile)
    {
        var list = new List<StartScreenPreset>(cols * rows);
        var idx = 1;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                list.Add(new StartScreenPreset($"Màn {idx}", c * tile, r * tile, idx, r + 1, c + 1));
                idx++;
            }
        }
        return list;
    }

    private void ApplyBigCenterTemplateInternal(string sceneId)
    {
        var list = new List<VwWindowSceneDto>();
        var tile = Tile;
        var screens = AvailableStartScreens.Count > 0
            ? AvailableStartScreens.ToList()
            : BuildFallbackScreens(GridCols ?? 8, GridRows ?? 4, tile);

        var maxCol = screens.Max(s => s.Col);
        var maxRow = screens.Max(s => s.Row);

        if (maxCol >= 2 && maxRow >= 2 && screens.Count >= 4)
        {
            var mapSrcId = Sources.Count > 0 ? Sources[0].ID : "1";
            list.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                SceneId = sceneId,
                Name = "Khối lớn Trọng điểm (2×2)",
                Label = "Khối sự cố (2×2)",
                SourceId = mapSrcId,
                X = 0,
                Y = 0,
                W = 2 * tile,
                H = 2 * tile,
                ZIndex = 1,
                OrderNo = 1,
                Visible = VwWindowVisible.Visible,
            });

            var remainingScreens = screens.Where(s => s.X >= 2 * tile || s.Y >= 2 * tile).ToList();
            for (var i = 0; i < remainingScreens.Count; i++)
            {
                var scr = remainingScreens[i];
                var srcId = Sources.Count > i + 1 ? Sources[i + 1].ID : (i + 2).ToString();
                list.Add(new VwWindowSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    SceneId = sceneId,
                    Name = $"Camera phụ ({scr.Name})",
                    Label = scr.Name,
                    SourceId = srcId,
                    X = scr.X,
                    Y = scr.Y,
                    W = tile,
                    H = tile,
                    ZIndex = 1,
                    OrderNo = i + 2,
                    Visible = VwWindowVisible.Visible,
                });
            }
        }
        else
        {
            for (var i = 0; i < screens.Count; i++)
            {
                var scr = screens[i];
                var srcId = Sources.Count > i ? Sources[i].ID : (i + 1).ToString();
                list.Add(new VwWindowSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    SceneId = sceneId,
                    Name = $"Camera {i + 1} ({scr.Name})",
                    Label = scr.Name,
                    SourceId = srcId,
                    X = scr.X,
                    Y = scr.Y,
                    W = tile,
                    H = tile,
                    ZIndex = 1,
                    OrderNo = i + 1,
                    Visible = VwWindowVisible.Visible,
                });
            }
        }

        PopulateSceneWindowsFromDtos(list);
    }

    private void ApplyDual2x2TemplateInternal(string sceneId)
    {
        var list = new List<VwWindowSceneDto>();
        var tile = Tile;
        var screens = AvailableStartScreens.Count > 0
            ? AvailableStartScreens.ToList()
            : BuildFallbackScreens(GridCols ?? 8, GridRows ?? 4, tile);

        var maxCol = screens.Max(s => s.Col);
        var maxRow = screens.Max(s => s.Row);

        if (maxCol >= 4 && maxRow >= 2 && screens.Count >= 8)
        {
            var srcId1 = Sources.Count > 0 ? Sources[0].ID : "1";
            list.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                SceneId = sceneId,
                Name = "Khối Trọng điểm 1 (2×2)",
                Label = "Khối 1 (2×2)",
                SourceId = srcId1,
                X = 0,
                Y = 0,
                W = 2 * tile,
                H = 2 * tile,
                ZIndex = 1,
                OrderNo = 1,
                Visible = VwWindowVisible.Visible,
            });

            var srcId2 = Sources.Count > 1 ? Sources[1].ID : "2";
            list.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                SceneId = sceneId,
                Name = "Khối Trọng điểm 2 (2×2)",
                Label = "Khối 2 (2×2)",
                SourceId = srcId2,
                X = 2 * tile,
                Y = 0,
                W = 2 * tile,
                H = 2 * tile,
                ZIndex = 1,
                OrderNo = 2,
                Visible = VwWindowVisible.Visible,
            });

            var remainingScreens = screens.Where(s => s.Y >= 2 * tile).ToList();
            for (var i = 0; i < remainingScreens.Count; i++)
            {
                var scr = remainingScreens[i];
                var srcId = Sources.Count > i + 2 ? Sources[i + 2].ID : (i + 3).ToString();
                list.Add(new VwWindowSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    SceneId = sceneId,
                    Name = $"Camera phụ ({scr.Name})",
                    Label = scr.Name,
                    SourceId = srcId,
                    X = scr.X,
                    Y = scr.Y,
                    W = tile,
                    H = tile,
                    ZIndex = 1,
                    OrderNo = i + 3,
                    Visible = VwWindowVisible.Visible,
                });
            }
        }
        else
        {
            for (var i = 0; i < screens.Count; i++)
            {
                var scr = screens[i];
                var srcId = Sources.Count > i ? Sources[i].ID : (i + 1).ToString();
                list.Add(new VwWindowSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    SceneId = sceneId,
                    Name = $"Camera {i + 1} ({scr.Name})",
                    Label = scr.Name,
                    SourceId = srcId,
                    X = scr.X,
                    Y = scr.Y,
                    W = tile,
                    H = tile,
                    ZIndex = 1,
                    OrderNo = i + 1,
                    Visible = VwWindowVisible.Visible,
                });
            }
        }

        PopulateSceneWindowsFromDtos(list);
    }

    private void ApplyFullWallTemplateInternal(string sceneId)
    {
        var tile = Tile;
        var totalW = TotalWallWidth > 0 ? TotalWallWidth : (GridCols ?? 8) * tile;
        var totalH = TotalWallHeight > 0 ? TotalWallHeight : (GridRows ?? 4) * tile;
        var srcId = Sources.Count > 0 ? Sources[0].ID : "1";
        var list = new List<VwWindowSceneDto>
        {
            new()
            {
                ID = Guid.NewGuid().ToString("N"),
                SceneId = sceneId,
                Name = $"Toàn tường ({totalW}×{totalH})",
                Label = $"Toàn tường ({AvailableStartScreens.Count} màn)",
                SourceId = srcId,
                X = 0,
                Y = 0,
                W = totalW,
                H = totalH,
                ZIndex = 1,
                OrderNo = 1,
                Visible = VwWindowVisible.Visible,
            }
        };

        PopulateSceneWindowsFromDtos(list);
    }

    [RelayCommand(CanExecute = nameof(CanApplyIndividual))]
    private async Task ApplyIndividualWindows(CancellationToken cancellationToken)
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
            return;

        var rows = ScreenAssignments.Where(row => row.Include).ToList();
        var notReady = rows.Where(row => !row.IsReadyToApply).ToList();
        if (notReady.Count > 0)
        {
            StatusMessage = $"{notReady.Count} dòng chưa đủ dữ liệu (thiếu nguồn tín hiệu hoặc kích thước). Bổ sung rồi bấm lại.";
            return;
        }

        if (rows.Count == 0)
        {
            StatusMessage = "Chưa chọn màn hình nào.";
            return;
        }

        var maxWindows = _connection.ProbeResult?.MaxWindowNums ?? 0;
        if (maxWindows > 0 && rows.Count > maxWindows)
        {
            StatusMessage = $"⚠️ Số lượng cửa sổ ({rows.Count}) vượt quá giới hạn tối đa của thiết bị ({maxWindows}).";
            return;
        }

        var missingGrid = rows
            .Where(row => TryResolveWallPosition(row.Screen) is null)
            .ToList();

        if (missingGrid.Count > 0)
        {
            StatusMessage = $"{missingGrid.Count} màn hình chưa khai Cột lưới / Hàng lưới nên không xác định được vị trí trên tường: "
                + string.Join(", ", missingGrid.Select(row => row.ScreenName));
            return;
        }

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        await Run($"Đang tạo {rows.Count} cửa sổ phủ kín màn hình...", async () =>
        {
            var created = 0;
            foreach (var row in rows)
            {
                var wallPosition = TryResolveWallPosition(row.Screen)!.Value;
                var window = new VwWindowSceneDto
                {
                    Name = row.ScreenName,
                    SceneId = sceneId,
                    SourceId = row.SelectedSource!.ID,
                    Label = row.ScreenName,
                    X = wallPosition.X,
                    Y = wallPosition.Y,
                    W = row.Width,
                    H = row.Height,
                    ZIndex = 1,
                    Visible = VwWindowVisible.Visible,
                    OrderNo = created,
                };

                VwLocalSceneStore.AddWindowScene(deviceKey, window);
                created++;
            }

            StatusMessage = $"Đã tạo {created} cửa sổ cho kịch bản {CurrentScene?.Name}.";
            ReloadSceneWindows(sceneId);
            await Task.CompletedTask;
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanApplyOverlapping))]
    private async Task ApplyOverlappingWindows(CancellationToken cancellationToken)
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
            return;

        var notReady = WindowRows.Where(row => !row.IsReadyToApply).ToList();
        if (notReady.Count > 0)
        {
            StatusMessage = $"{notReady.Count} dòng cửa sổ chưa đủ dữ liệu (nguồn, toạ độ, kích thước, ZIndex).";
            return;
        }

        if (WindowRows.Count == 0)
        {
            StatusMessage = "Chưa thêm dòng cửa sổ nào.";
            return;
        }

        var maxWindows = _connection.ProbeResult?.MaxWindowNums ?? 0;
        if (maxWindows > 0 && WindowRows.Count > maxWindows)
        {
            StatusMessage = $"⚠️ Số lượng cửa sổ ({WindowRows.Count}) vượt quá giới hạn tối đa của thiết bị ({maxWindows}).";
            return;
        }

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        await Run($"Đang tạo {WindowRows.Count} cửa sổ xếp lớp...", async () =>
        {
            var created = 0;
            foreach (var row in WindowRows)
            {
                var label = string.IsNullOrWhiteSpace(row.Label) ? $"Cửa sổ {created + 1}" : row.Label.Trim();
                var window = new VwWindowSceneDto
                {
                    Name = label,
                    SceneId = sceneId,
                    SourceId = row.SelectedSource!.ID,
                    Label = label,
                    X = row.X,
                    Y = row.Y,
                    W = row.Width,
                    H = row.Height,
                    ZIndex = row.ZIndex,
                    Visible = VwWindowVisible.Visible,
                    OrderNo = created,
                };

                VwLocalSceneStore.AddWindowScene(deviceKey, window);
                created++;
            }

            StatusMessage = $"Đã tạo {created} cửa sổ xếp lớp cho kịch bản {CurrentScene?.Name}.";
            ReloadSceneWindows(sceneId);
            await Task.CompletedTask;
        }).ConfigureAwait(true);
    }

    [RelayCommand]
    private void AddWindowRow()
    {
        var nextZIndex = WindowRows.Count == 0 ? 1 : WindowRows.Max(row => row.ZIndex ?? 0) + 1;
        WindowRows.Add(new WindowSceneRow
        {
            Label = $"Cửa sổ {WindowRows.Count + 1}",
            ZIndex = nextZIndex,
            SelectedSource = Sources.FirstOrDefault(),
        });

        ApplyOverlappingWindowsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveWindowRow(WindowSceneRow? row)
    {
        if (row is not null)
            WindowRows.Remove(row);

        ApplyOverlappingWindowsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanLoadSceneWindows))]
    private async Task LoadSceneWindows(CancellationToken cancellationToken)
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
            return;

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        await Run("Đang đọc thông tin kịch bản...", async () =>
        {
            ActiveScene = VwLocalSceneStore.GetActiveScene(deviceKey);
            ReloadSceneWindows(sceneId);

            StatusMessage = ActiveScene is null
                ? $"Kịch bản đang làm việc có {SceneWindows.Count} cửa sổ. Hiện chưa có kịch bản nào đang chiếu."
                : $"Kịch bản đang làm việc có {SceneWindows.Count} cửa sổ. Đang chiếu: {ActiveScene.Name} [{ActiveScene.Code}].";
            await Task.CompletedTask;
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanActivateScene))]
    private async Task ActivateScene(CancellationToken cancellationToken)
    {
        var scene = CurrentScene;
        if (scene is null || string.IsNullOrWhiteSpace(scene.ID))
            return;

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        var sid = int.TryParse(scene.OutputId, out var parsedSid) ? parsedSid : 1;
        var effectiveWallNo = WallNo ?? _connection.WallNo ?? _connection.ProbeResult?.BoundWallNo;
        if (effectiveWallNo is null)
        {
            StatusMessage = "⚠️ Cần nhập WallNo trước khi kích hoạt kịch bản (không tự chọn để tránh nhầm tường).";
            return;
        }

        await Run($"Đang kích hoạt kịch bản {scene.Name} (SID {sid})...", async () =>
        {
            if (_connection.IsConnected)
            {
                var isapiClient = _connection.BuildDirectISAPIClient();
                if (isapiClient != null)
                {
                    var isapiRes = await isapiClient.ActivateScene(effectiveWallNo.Value, sid, cancellationToken).ConfigureAwait(true);
                    if (!isapiRes.Success)
                    {
                        StatusMessage = $"⚠️ Kích hoạt kịch bản ISAPI thất bại: {isapiRes.ErrorMessage}";
                        return;
                    }
                }
            }

            VwLocalSceneStore.SetActiveScene(deviceKey, scene.ID);
            ActiveScene = scene;
            StatusMessage = $"⚡ Đã kích hoạt thành công kịch bản '{scene.Name}' (SID {sid}) lên tường #{effectiveWallNo.Value}.";
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task SyncScenesFromDevice(CancellationToken cancellationToken)
    {
        var wallNo = WallNo ?? _connection.WallNo ?? _connection.ProbeResult?.BoundWallNo;
        if (wallNo is null)
        {
            StatusMessage = "⚠️ Cần nhập WallNo trước khi đồng bộ kịch bản từ thiết bị.";
            return;
        }

        var isapiClient = _connection.BuildDirectISAPIClient();
        if (isapiClient == null)
        {
            StatusMessage = "⚠️ Chưa kết nối tới thiết bị.";
            return;
        }

        await Run("Đang đọc danh sách kịch bản từ thiết bị...", async () =>
        {
            var res = await isapiClient.GetScenes(wallNo.Value, cancellationToken).ConfigureAwait(true);
            if (!res.Success || res.Data?.WallScene == null)
            {
                StatusMessage = $"⚠️ Không đọc được kịch bản từ thiết bị: {res.ErrorMessage}";
                return;
            }

            var summaries = res.Data.WallScene.Select(s => new VwWallSceneSummary
            {
                Id = s.Id,
                Name = s.Name,
            }).ToList();

            var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
            VwLocalSceneStore.SyncScenesFromDevice(deviceKey, wallNo.Value, summaries);

            var runningRes = await isapiClient.GetRunningScene(wallNo.Value, cancellationToken).ConfigureAwait(true);
            int? runningSid = runningRes.Success && runningRes.Data != null ? runningRes.Data.SceneId : null;

            ReloadScenesForCurrentWall();

            if (runningSid.HasValue)
            {
                var matched = Scenes.FirstOrDefault(s => s.OutputId == runningSid.Value.ToString());
                if (matched != null && !string.IsNullOrWhiteSpace(matched.ID))
                {
                    VwLocalSceneStore.SetActiveScene(deviceKey, matched.ID);
                    ActiveScene = matched;
                    CurrentScene = matched;
                }
            }

            var winRes = await isapiClient.GetWindows(wallNo.Value, cancellationToken).ConfigureAwait(true);
            var activeTarget = CurrentScene ?? Scenes.FirstOrDefault();
            if (winRes.Success && winRes.Data?.WallWindow != null && activeTarget != null && !string.IsNullOrWhiteSpace(activeTarget.ID))
            {
                var syncedWindows = VwLocalSceneStore.SyncWindowsFromDevice(deviceKey, activeTarget.ID, winRes.Data.WallWindow, Sources.ToList());
                PopulateSceneWindowsFromDtos(syncedWindows);
            }

            StatusMessage = $"Đã đồng bộ {summaries.Count} kịch bản và kích hoạt kịch bản {CurrentScene?.Name} (SID {CurrentScene?.OutputId}) từ thiết bị.";
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task PullWindowsFromDevice(CancellationToken cancellationToken)
    {
        var wallNo = WallNo ?? _connection.WallNo ?? _connection.ProbeResult?.BoundWallNo;
        if (wallNo is null)
        {
            StatusMessage = "⚠️ Cần nhập WallNo trước khi đồng bộ bố cục cửa sổ từ thiết bị.";
            return;
        }

        var isapiClient = _connection.BuildDirectISAPIClient();
        if (isapiClient == null)
        {
            StatusMessage = "⚠️ Chưa kết nối tới thiết bị.";
            return;
        }

        if (CurrentScene == null || string.IsNullOrWhiteSpace(CurrentScene.ID))
        {
            StatusMessage = "⚠️ Hãy chọn một kịch bản trước khi đồng bộ bố cục.";
            return;
        }

        await Run($"Đang đọc các ô cửa sổ thực tế từ tường #{wallNo.Value}...", async () =>
        {
            var res = await isapiClient.GetWindows(wallNo.Value, cancellationToken).ConfigureAwait(true);
            if (!res.Success || res.Data?.WallWindow == null)
            {
                StatusMessage = $"⚠️ Không đọc được cửa sổ từ thiết bị: {res.ErrorMessage}";
                return;
            }

            var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
            var syncedWindows = VwLocalSceneStore.SyncWindowsFromDevice(deviceKey, CurrentScene.ID, res.Data.WallWindow, Sources.ToList());
            PopulateSceneWindowsFromDtos(syncedWindows);
            StatusMessage = $"Đã đồng bộ thành công {syncedWindows.Count} ô cửa sổ từ thiết bị vào kịch bản '{CurrentScene.Name}'.";
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanUpdateScene))]
    private async Task UpdateScene(CancellationToken cancellationToken)
    {
        var scene = CurrentScene;
        if (scene is null || string.IsNullOrWhiteSpace(scene.ID))
            return;

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        scene.Name = string.IsNullOrWhiteSpace(SceneName) ? scene.Name : SceneName.Trim();
        scene.OutputId = string.IsNullOrWhiteSpace(SceneOutputId) ? scene.OutputId : SceneOutputId.Trim();
        scene.GridCols = GridCols ?? scene.GridCols;
        scene.GridRows = GridRows ?? scene.GridRows;

        await Run($"Đang cập nhật kịch bản {scene.Name}...", async () =>
        {
            VwLocalSceneStore.UpdateScene(deviceKey, scene);

            var index = Scenes.IndexOf(scene);
            if (index >= 0)
            {
                Scenes[index] = scene;
                CurrentScene = scene;
            }

            OnPropertyChanged(nameof(CurrentSceneSummary));
            StatusMessage = $"Đã cập nhật kịch bản {scene.Name} [{scene.Code}].";
            await Task.CompletedTask;
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteScene))]
    private async Task DeleteScene(CancellationToken cancellationToken)
    {
        var scene = CurrentScene;
        if (scene is null || string.IsNullOrWhiteSpace(scene.ID))
            return;

        if (!ConfirmSceneDeletion(scene))
        {
            StatusMessage = "Đã huỷ lệnh xoá kịch bản.";
            return;
        }

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        await Run($"Đang xoá kịch bản {scene.Name}...", async () =>
        {
            VwLocalSceneStore.DeleteScene(deviceKey, scene.ID);

            var itemToRemove = Scenes.FirstOrDefault(s => s.ID == scene.ID) ?? scene;
            Scenes.Remove(itemToRemove);
            SceneWindows.Clear();
            CurrentScene = Scenes.FirstOrDefault();
            if (ActiveScene?.ID == scene.ID)
                ActiveScene = null;

            OnPropertyChanged(nameof(CurrentSceneSummary));
            StatusMessage = $"Đã xoá kịch bản {scene.Name} [{scene.Code}].";
            await Task.CompletedTask;
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanEditSceneWindow))]
    private async Task DeleteSceneWindow(SceneWindowRow? row)
    {
        var sceneId = CurrentScene?.ID;
        var window = row?.Window;
        if (row is null || window is null || string.IsNullOrWhiteSpace(sceneId))
            return;

        var devWinId = row.ResolveDeviceWindowId();

        SceneWindows.Remove(row);
        for (var i = 0; i < SceneWindows.Count; i++)
        {
            SceneWindows[i].OrderNo = i + 1;
        }
        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        VwLocalSceneStore.SaveWindowScenes(deviceKey, sceneId, SceneWindows.Select(r => r.Window));
        PopulateReadSurveyInfo();
        DeleteSelectedSceneWindowsCommand.NotifyCanExecuteChanged();
        SaveAllSceneWindowsCommand.NotifyCanExecuteChanged();
        AddSceneWindowCommand.NotifyCanExecuteChanged();
        PushToDeviceCommand.NotifyCanExecuteChanged();
        PushWindowsOnlyToDeviceCommand.NotifyCanExecuteChanged();
        StatusMessage = $"Đã xoá {window.Label ?? window.Name}. Kịch bản còn {SceneWindows.Count} ô (đã lưu).";

        if (!DryRun)
        {
            var isapiClient = _connection.BuildDirectISAPIClient();
            if (isapiClient != null)
            {
                var effectiveWallNo = WallNo ?? _connection.WallNo ?? _connection.ProbeResult?.BoundWallNo;
                if (effectiveWallNo is null)
                {
                    StatusMessage = "⚠️ Cần nhập WallNo trước khi xoá cửa sổ trên thiết bị.";
                    await Task.CompletedTask;
                    return;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var targetId = devWinId;
                        if (!targetId.HasValue || targetId.Value <= 0)
                        {
                            var currentWins = await isapiClient.GetWindows(effectiveWallNo.Value);
                            if (currentWins.Success && currentWins.Data?.WallWindow != null)
                            {
                                var match = currentWins.Data.WallWindow.FirstOrDefault(w =>
                                    w.Rect?.Coordinate?.X == (row.X ?? 0) &&
                                    w.Rect?.Coordinate?.Y == (row.Y ?? 0));
                                if (match != null)
                                    targetId = match.Id;
                            }
                        }

                        if (targetId.HasValue && targetId.Value > 0)
                        {
                            await isapiClient.DeleteWindow(effectiveWallNo.Value, targetId.Value);
                        }
                    }
                    catch
                    {
                        // Non-fatal live hardware removal
                    }
                });
            }
        }

        await Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedSceneWindows))]
    private async Task DeleteSelectedSceneWindows()
    {
        var selected = SceneWindows.Where(w => w.IsSelected).ToList();
        if (selected.Count == 0 && SelectedSceneWindow is not null)
        {
            selected.Add(SelectedSceneWindow);
        }

        if (selected.Count == 0)
        {
            StatusMessage = "Vui lòng tick chọn hoặc nhấp chọn ít nhất một ô camera cần xoá.";
            return;
        }

        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
            return;

        var devWinIds = selected
            .Select(r => r.ResolveDeviceWindowId())
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .ToList();

        foreach (var item in selected)
        {
            SceneWindows.Remove(item);
        }
        for (var i = 0; i < SceneWindows.Count; i++)
        {
            SceneWindows[i].OrderNo = i + 1;
        }
        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        VwLocalSceneStore.SaveWindowScenes(deviceKey, sceneId, SceneWindows.Select(r => r.Window));
        PopulateReadSurveyInfo();
        IsAllSceneWindowsSelected = false;
        SelectedSceneWindow = null;
        DeleteSelectedSceneWindowsCommand.NotifyCanExecuteChanged();
        SaveAllSceneWindowsCommand.NotifyCanExecuteChanged();
        AddSceneWindowCommand.NotifyCanExecuteChanged();
        PushToDeviceCommand.NotifyCanExecuteChanged();
        PushWindowsOnlyToDeviceCommand.NotifyCanExecuteChanged();
        StatusMessage = $"Đã xoá {selected.Count} ô. Kịch bản còn {SceneWindows.Count} ô (đã lưu).";

        if (!DryRun && selected.Count > 0)
        {
            var isapiClient = _connection.BuildDirectISAPIClient();
            if (isapiClient != null)
            {
                var effectiveWallNo = WallNo ?? _connection.WallNo ?? _connection.ProbeResult?.BoundWallNo;
                if (effectiveWallNo is null)
                {
                    StatusMessage = "⚠️ Cần nhập WallNo trước khi xoá các cửa sổ trên thiết bị.";
                    return;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var targetIds = new HashSet<int>(devWinIds);
                        var missingRows = selected.Where(r => !r.ResolveDeviceWindowId().HasValue).ToList();
                        if (missingRows.Count > 0)
                        {
                            var currentWins = await isapiClient.GetWindows(effectiveWallNo.Value);
                            if (currentWins.Success && currentWins.Data?.WallWindow != null)
                            {
                                foreach (var r in missingRows)
                                {
                                    var match = currentWins.Data.WallWindow.FirstOrDefault(w =>
                                        w.Rect?.Coordinate?.X == (r.X ?? 0) &&
                                        w.Rect?.Coordinate?.Y == (r.Y ?? 0));
                                    if (match != null)
                                        targetIds.Add(match.Id);
                                }
                            }
                        }

                        foreach (var id in targetIds)
                        {
                            await isapiClient.DeleteWindow(effectiveWallNo.Value, id);
                        }
                    }
                    catch
                    {
                        // Non-fatal live hardware removal
                    }
                });
            }
        }

        await Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanEditSceneWindow))]
    private async Task UpdateSceneWindow(SceneWindowRow? row)
    {
        var sceneId = CurrentScene?.ID;
        var window = row?.Window;
        if (window is null || string.IsNullOrWhiteSpace(window.ID) || string.IsNullOrWhiteSpace(sceneId))
            return;

        if (window.W is not > 0 || window.H is not > 0 || window.X is not >= 0 || window.Y is not >= 0)
        {
            StatusMessage = "Cửa sổ phải có X, Y >= 0 và W, H > 0 mới lưu được.";
            return;
        }

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        await Run($"Đang lưu cửa sổ {window.Label ?? window.Name}...", async () =>
        {
            VwLocalSceneStore.UpdateWindowScene(deviceKey, window);
            ReloadSceneWindows(sceneId);

            StatusMessage = $"Đã lưu cửa sổ {window.Label ?? window.Name}.";
            await Task.CompletedTask;
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanSaveAllSceneWindows))]
    private async Task SaveAllSceneWindows()
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
        {
            StatusMessage = "Vui lòng chọn kịch bản trước khi lưu.";
            return;
        }

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        await Run($"Đang lưu toàn bộ {SceneWindows.Count} ô camera...", async () =>
        {
            foreach (var row in SceneWindows)
            {
                row.SyncSubWindowsToDto();
            }

            VwLocalSceneStore.SaveWindowScenes(deviceKey, sceneId, SceneWindows.Select(r => r.Window));
            StatusMessage = $"Đã lưu cấu hình {SceneWindows.Count} ô camera của kịch bản \"{CurrentScene?.Name}\" thành công.";
            await Task.CompletedTask;
        }).ConfigureAwait(true);
    }

    private bool CanSaveAllSceneWindows() => !IsBusy && !string.IsNullOrWhiteSpace(CurrentScene?.ID);

    [RelayCommand(CanExecute = nameof(CanPushToDevice))]
    private async Task PushToDevice(CancellationToken cancellationToken)
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
            return;

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        VwLocalSceneStore.SaveWindowScenes(deviceKey, sceneId, SceneWindows.Select(r => r.Window));

        var directClient = BuildDirectClientFromConnection();
        if (directClient is null)
            return;

        var sid = int.TryParse(CurrentScene?.OutputId, out var parsedSid) ? parsedSid : 1;

        var directWindows = BuildDirectWindows();

        var effectiveWallNo = WallNo ?? _connection.WallNo ?? _connection.ProbeResult?.BoundWallNo;
        if (effectiveWallNo is null)
        {
            StatusMessage = "⚠️ Cần nhập WallNo trước khi đẩy xuống thiết bị (không tự chọn để tránh đẩy nhầm tường).";
            return;
        }

        var directInput = new VwDirectPushSceneInput
        {
            SceneId = sid,
            SceneName = CurrentScene?.Name,
            WallNo = effectiveWallNo,
            DryRun = DryRun,
            ResetWindows = ResetWindows,
            Activate = ActivateAfterPush,
            Windows = directWindows,
        };

        await Run(DryRun ? "Đang chạy thử trực tiếp (DryRun)..." : "Đang đẩy thật trực tiếp xuống thiết bị...", async () =>
        {
            var result = await directClient.Execute(directInput, cancellationToken).ConfigureAwait(true);
            await VwDeviceStepLogging.PublishSteps(_publisher, _activityPublisher, "PushToDevice", result.Steps, cancellationToken).ConfigureAwait(true);

            var windowCount = result.Windows.Count;
            var failedSteps = result.Steps.Count(step => !step.Success && !step.Skipped);

            if (result.Success && result.Windows.Count > 0)
            {
                for (var i = 0; i < Math.Min(SceneWindows.Count, result.Windows.Count); i++)
                {
                    if (result.Windows[i].DeviceWindowId.HasValue)
                    {
                        SceneWindows[i].Window.DeviceWindowId = result.Windows[i].DeviceWindowId.Value.ToString();
                    }
                }
            }

            StatusMessage = result.Success
                ? $"{(DryRun ? "Chạy thử" : "Đẩy thật")} trực tiếp thành công: {windowCount} cửa sổ. Xem từng bước ở tab Log."
                : $"{(DryRun ? "Chạy thử" : "Đẩy thật")} trực tiếp thất bại: {result.Message} ({failedSteps} bước lỗi).";
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanPushToDevice))]
    private async Task PushWindowsOnlyToDevice(CancellationToken cancellationToken)
    {
        var sceneId = CurrentScene?.ID;
        if (string.IsNullOrWhiteSpace(sceneId))
            return;

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        VwLocalSceneStore.SaveWindowScenes(deviceKey, sceneId, SceneWindows.Select(r => r.Window));

        var directClient = BuildDirectClientFromConnection();
        if (directClient is null)
            return;

        var sid = int.TryParse(CurrentScene?.OutputId, out var parsedSid) ? parsedSid : 1;

        var directWindows = BuildDirectWindows();

        var effectiveWallNo = WallNo ?? _connection.WallNo ?? _connection.ProbeResult?.BoundWallNo;
        if (effectiveWallNo is null)
        {
            StatusMessage = "⚠️ Cần nhập WallNo trước khi đẩy xuống thiết bị (không tự chọn để tránh đẩy nhầm tường).";
            return;
        }

        var directInput = new VwDirectPushSceneInput
        {
            SceneId = sid,
            SceneName = CurrentScene?.Name,
            WallNo = effectiveWallNo,
            DryRun = DryRun,
            ResetWindows = ResetWindows,
            SaveData = false,
            Activate = ActivateAfterPush,
            Windows = directWindows,
        };

        await Run(DryRun ? "Đang chạy thử trực tiếp (chỉ dựng cửa sổ, DryRun)..." : "Đang đẩy thật trực tiếp (chỉ dựng cửa sổ, KHÔNG lưu scene)...", async () =>
        {
            var result = await directClient.Execute(directInput, cancellationToken).ConfigureAwait(true);
            await VwDeviceStepLogging.PublishSteps(_publisher, _activityPublisher, "PushWindowsOnlyToDevice", result.Steps, cancellationToken).ConfigureAwait(true);

            var windowCount = result.Windows.Count;
            var failedSteps = result.Steps.Count(step => !step.Success && !step.Skipped);

            if (result.Success && result.Windows.Count > 0)
            {
                for (var i = 0; i < Math.Min(SceneWindows.Count, result.Windows.Count); i++)
                {
                    if (result.Windows[i].DeviceWindowId.HasValue)
                    {
                        SceneWindows[i].Window.DeviceWindowId = result.Windows[i].DeviceWindowId.Value.ToString();
                    }
                }
            }

            StatusMessage = result.Success
                ? $"{(DryRun ? "Chạy thử" : "Đẩy thật")} trực tiếp thành công (chỉ dựng {windowCount} cửa sổ, KHÔNG lưu scene). Xem từng bước ở tab Log."
                : $"{(DryRun ? "Chạy thử" : "Đẩy thật")} trực tiếp thất bại: {result.Message} ({failedSteps} bước lỗi).";
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private void OpenSourceManagement()
    {
        var deviceKey = GetDeviceSourceKey();
        var probeSources = (_connection.ProbeResult?.InputChannels ?? [])
            .Select(ch =>
            {
                var isIpPort = !string.IsNullOrWhiteSpace(ch.InputPortType)
                               && (ch.InputPortType.Contains("IP", StringComparison.OrdinalIgnoreCase)
                                   || ch.InputPortType.Contains("Stream", StringComparison.OrdinalIgnoreCase));

                return new VwSourceDto
                {
                    ID = ch.Id.ToString(),
                    Code = isIpPort ? $"IP_{ch.Id}" : $"HDMI_{ch.Id}",
                    Name = string.IsNullOrWhiteSpace(ch.Name) ? (isIpPort ? $"Camera IP {ch.Id}" : $"Kênh {ch.Id}") : ch.Name,
                    SourceType = isIpPort ? "ip_stream" : "local_signal",
                    SignalType = isIpPort ? (ch.InputPortType ?? "RTSP") : "HDMI",
                    SignalNo = ch.Id,
                };
            })
            .ToList();

        var vm = new SourceManagementViewModel(deviceKey, probeSources);
        var dialog = new Module.VideoWall.WPF.Views.SourceManagementDialog(vm)
        {
            Owner = System.Windows.Application.Current?.MainWindow,
        };

        dialog.ShowDialog();

        PopulateSources(deviceKey, _connection.ProbeResult?.InputChannels);
    }

    private bool CanRun() => !IsBusy;

    private bool CanCreateScene() => !IsBusy && !string.IsNullOrWhiteSpace(SceneName);

    private bool CanApplyIndividual() => !IsBusy && !string.IsNullOrWhiteSpace(CurrentScene?.ID);

    private bool CanApplyOverlapping() => !IsBusy && !string.IsNullOrWhiteSpace(CurrentScene?.ID);

    private bool CanLoadSceneWindows() => !IsBusy && !string.IsNullOrWhiteSpace(CurrentScene?.ID);

    private bool CanActivateScene() => !IsBusy && !string.IsNullOrWhiteSpace(CurrentScene?.ID);

    private bool CanUpdateScene() => !IsBusy && !string.IsNullOrWhiteSpace(CurrentScene?.ID);

    private bool CanDeleteScene() => !IsBusy && !string.IsNullOrWhiteSpace(CurrentScene?.ID);

    private bool CanEditSceneWindow(SceneWindowRow? row) =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(CurrentScene?.ID)
        && !string.IsNullOrWhiteSpace(row?.ID);

    private bool CanDeleteSelectedSceneWindows() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(CurrentScene?.ID)
        && SceneWindows.Any(w => w.IsSelected);

    private bool CanPushToDevice() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(CurrentScene?.ID)
        && !string.IsNullOrWhiteSpace(_connection.AdHocIp)
        && _connection.ProbeResult != null
        && (WallNo ?? _connection.WallNo) is not null;


    private bool ConfirmSceneDeletion(VwSceneDto scene)
    {
        var message = $"Sắp XOÁ kịch bản {scene.Name} [{scene.Code}].\n\n"
            + $"Toàn bộ {SceneWindows.Count} cửa sổ thuộc kịch bản này cũng không còn dùng được.\n"
            + "Giao diện này KHÔNG có chức năng hoàn tác.\n\nTiếp tục?";

        return _userConfirmation.Confirm("Xác nhận xoá kịch bản", message);
    }

    private bool ConfirmWindowDeletion(VwWindowSceneDto window)
    {
        var message = $"Sắp XOÁ cửa sổ {window.Label ?? window.Name} ({window.Geometry}, ZIndex {window.ZIndex}).\n\n"
            + "Giao diện này KHÔNG có chức năng hoàn tác.\n\nTiếp tục?";

        return _userConfirmation.Confirm("Xác nhận xoá cửa sổ", message);
    }

    public void MarkSceneWindowsDirty()
    {
        SaveAllSceneWindowsCommand.NotifyCanExecuteChanged();
        PushToDeviceCommand.NotifyCanExecuteChanged();
        PushWindowsOnlyToDeviceCommand.NotifyCanExecuteChanged();
    }

    public void AttachSceneWindowRowHandlers(SceneWindowRow row)
    {
        row.PropertyChanged += (_, prop) =>
        {
            if (prop.PropertyName == nameof(SceneWindowRow.IsSelected))
            {
                DeleteSelectedSceneWindowsCommand.NotifyCanExecuteChanged();
            }
            if (prop.PropertyName is nameof(SceneWindowRow.WindowMode)
                or nameof(SceneWindowRow.SubCells)
                or nameof(SceneWindowRow.SelectedSource)
                or nameof(SceneWindowRow.SourceId)
                or nameof(SceneWindowRow.X)
                or nameof(SceneWindowRow.Y)
                or nameof(SceneWindowRow.W)
                or nameof(SceneWindowRow.H)
                or nameof(SceneWindowRow.ZIndex)
                or nameof(SceneWindowRow.Name)
                or nameof(SceneWindowRow.Label))
            {
                MarkSceneWindowsDirty();
            }

            if (prop.PropertyName is nameof(SceneWindowRow.X)
                or nameof(SceneWindowRow.Y)
                or nameof(SceneWindowRow.W)
                or nameof(SceneWindowRow.H)
                or nameof(SceneWindowRow.ZIndex))
            {
                if (!row.IsInteractivelyDragging)
                {
                    ScheduleLiveWindowUpdate(row);
                }
            }
            else if (prop.PropertyName is nameof(SceneWindowRow.SelectedSource)
                     or nameof(SceneWindowRow.SourceId)
                     or nameof(SceneWindowRow.WindowMode)
                     or nameof(SceneWindowRow.SubCells))
            {
                TriggerImmediateLiveWindowUpdate(row);
            }
            SaveAllSceneWindowsCommand.NotifyCanExecuteChanged();
        };
    }

    private CancellationTokenSource? _liveUpdateCts;

    public void ScheduleLiveWindowUpdate(SceneWindowRow? row)
    {
        if (row is null || DryRun)
            return;

        _liveUpdateCts?.Cancel();
        _liveUpdateCts?.Dispose();
        _liveUpdateCts = new CancellationTokenSource();
        var token = _liveUpdateCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, token);
                if (!token.IsCancellationRequested)
                {
                    await PushLiveWindowUpdate(row, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored debounced operation
            }
        }, token);
    }

    public void TriggerImmediateLiveWindowUpdate(SceneWindowRow? row)
    {
        if (row is null || DryRun)
            return;

        _liveUpdateCts?.Cancel();
        _liveUpdateCts?.Dispose();
        _liveUpdateCts = new CancellationTokenSource();
        var token = _liveUpdateCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await PushLiveWindowUpdate(row, token);
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
        }, token);
    }

    public async Task PushLiveWindowUpdate(SceneWindowRow? row, CancellationToken ct = default)
    {
        if (row is null || DryRun)
            return;

        var isapiClient = _connection.BuildDirectISAPIClient();
        if (isapiClient is null)
            return;

        // KHÔNG mặc định về tường số 1: lệnh dưới đây GHI THẬT lên phần cứng. Đoán sai số hiệu
        // tường là mở cửa sổ lên một tường khác đang chạy, người vận hành không hề yêu cầu.
        // Thà không làm gì và báo rõ, hơn là ghi đúng cú pháp vào sai chỗ.
        var resolvedWallNo = WallNo
            ?? _connection.WallNo
            ?? _connection.ProbeResult?.BoundWallNo
            ?? _connection.ProbeResult?.WallNo;

        if (resolvedWallNo is null)
        {
            StatusMessage = "⚠️ Chưa dò được số hiệu tường — chạy Kết nối / Khảo sát trước khi đẩy cửa sổ lên thiết bị.";
            return;
        }

        var effectiveWallNo = resolvedWallNo.Value;

        var devWinId = row.ResolveDeviceWindowId();
        if (!devWinId.HasValue)
        {
            var winInput = BuildDirectWindowFromRow(row);
            var req = VwDirectSetupSceneOrchestrator.BuildWindowRequest(winInput);
            var addRes = await isapiClient.AddWindow(effectiveWallNo, req, ct);
            if (addRes.Success)
            {
                if (addRes.Data != null && addRes.Data.Id > 0)
                {
                    devWinId = addRes.Data.Id;
                }
                else
                {
                    var afterAddRes = await isapiClient.GetWindows(effectiveWallNo, ct);
                    if (afterAddRes.Success && afterAddRes.Data?.WallWindow != null)
                    {
                        var match = afterAddRes.Data.WallWindow.FirstOrDefault(w =>
                            w.Rect?.Coordinate?.X == (row.X ?? 0) &&
                            w.Rect?.Coordinate?.Y == (row.Y ?? 0) &&
                            w.Rect?.Width == (row.W ?? VwDirectDeviceConstants.UniformTileSize) &&
                            w.Rect?.Height == (row.H ?? VwDirectDeviceConstants.UniformTileSize));
                        if (match != null)
                            devWinId = match.Id;
                    }
                }

                if (devWinId.HasValue)
                {
                    row.Window.DeviceWindowId = devWinId.Value.ToString();

                    var mode = winInput.WindowMode <= 0 ? 1 : winInput.WindowMode;
                    var subs = winInput.SubWindows ?? [];
                    for (var i = 0; i < mode; i++)
                    {
                        var subInput = i < subs.Count ? subs[i] : null;
                        var streamUrl = subInput?.StreamUrl ?? (i == 0 ? winInput.StreamUrl : null);
                        var isIpStream = subInput?.SourceType == "ip_stream" || !string.IsNullOrWhiteSpace(streamUrl) || (subInput?.StreamingChannelId > 0) || (winInput.StreamingChannelId > 0);
                        if (isIpStream)
                            await isapiClient.StartDynamicDecode(effectiveWallNo, devWinId.Value, i + 1, ct);
                    }

                    if (row.ZIndex.HasValue && row.ZIndex.Value > 1)
                        await isapiClient.SetDeviceWindowTop(effectiveWallNo, devWinId.Value, ct);

                    StatusMessage = $"⚡ Đã mở ô mới {row.Label ?? row.Name} lên màn hình thật (Live, ID {devWinId.Value}).";
                    return;
                }
            }
            else
            {
                StatusMessage = $"⚠️ Lỗi mở ô mới trên thiết bị: {addRes.ErrorMessage}";
                return;
            }
        }

        if (!devWinId.HasValue)
            return;

        var winInputExisting = BuildDirectWindowFromRow(row);
        row.X = winInputExisting.X;
        row.Y = winInputExisting.Y;
        row.W = winInputExisting.W;
        row.H = winInputExisting.H;

        var updateReq = VwDirectSetupSceneOrchestrator.BuildWindowRequest(winInputExisting);
        updateReq.Id = devWinId.Value;
        updateReq.IdSpecified = true;

        var result = await isapiClient.UpdateDeviceWindow(effectiveWallNo, updateReq, ct);
        if (!result.Success)
            result = await isapiClient.UpdateDeviceWindowRect(effectiveWallNo, devWinId.Value, winInputExisting.X, winInputExisting.Y, winInputExisting.W, winInputExisting.H, ct);

        if (!result.Success)
        {
            await isapiClient.DeleteWindow(effectiveWallNo, devWinId.Value, ct);
            var reAddRes = await isapiClient.AddWindow(effectiveWallNo, updateReq, ct);
            if (reAddRes.Success)
            {
                if (reAddRes.Data != null && reAddRes.Data.Id > 0)
                {
                    devWinId = reAddRes.Data.Id;
                    row.Window.DeviceWindowId = devWinId.Value.ToString();
                }
                result = reAddRes;
            }
        }

        if (result.Success)
        {
            var isIpStream = winInputExisting.SourceType == "ip_stream" || !string.IsNullOrWhiteSpace(winInputExisting.StreamUrl) || (winInputExisting.StreamingChannelId > 0);
            if (isIpStream)
            {
                for (var i = 0; i < (winInputExisting.WindowMode <= 0 ? 1 : winInputExisting.WindowMode); i++)
                {
                    await isapiClient.StartDynamicDecode(effectiveWallNo, devWinId.Value, i + 1, ct);
                }
            }

            if (row.ZIndex.HasValue && SceneWindows.Count > 1)
            {
                var maxZ = SceneWindows.Max(rw => rw.ZIndex ?? 0);
                if (row.ZIndex.Value >= maxZ && maxZ > 0)
                    await isapiClient.SetDeviceWindowTop(effectiveWallNo, devWinId.Value, ct);
            }

            StatusMessage = $"⚡ Đã cập nhật vị trí & nguồn ô {row.Label ?? row.Name ?? devWinId.Value.ToString()} lên màn hình thật (Live, chưa lưu kịch bản).";
        }
        else
        {
            StatusMessage = $"⚠️ Lỗi cập nhật Live ô {devWinId.Value}: {result.ErrorMessage}";
        }
    }

    public void PopulateSceneWindowsFromDtos(IEnumerable<VwWindowSceneDto> windows)
    {
        SceneWindows.Clear();

        foreach (var window in windows.OrderBy(item => item.ZIndex ?? 0).ThenBy(item => item.OrderNo ?? 0))
        {
            var row = new SceneWindowRow(window);
            row.InitSubCells(Sources);
            if (!string.IsNullOrWhiteSpace(row.SourceId))
                row.SelectedSource = Sources.FirstOrDefault(s => s.ID == row.SourceId || s.SignalNo.ToString() == row.SourceId);

            if (row.SelectedSource == null && Sources.Count > 0)
            {
                if (int.TryParse(row.SourceId, out var sIdx) && sIdx >= 1 && sIdx <= Sources.Count)
                    row.SelectedSource = Sources[sIdx - 1];
                else
                    row.SelectedSource = Sources[0];
            }

            AttachSceneWindowRowHandlers(row);
            SceneWindows.Add(row);
        }

        PopulateReadSurveyInfo();

        IsAllSceneWindowsSelected = false;
        SelectedSceneWindow = null;
        DeleteSelectedSceneWindowsCommand.NotifyCanExecuteChanged();
        SaveAllSceneWindowsCommand.NotifyCanExecuteChanged();
        AddSceneWindowCommand.NotifyCanExecuteChanged();
        PushToDeviceCommand.NotifyCanExecuteChanged();
        PushWindowsOnlyToDeviceCommand.NotifyCanExecuteChanged();
    }

    private void ReloadSceneWindows(string sceneId)
    {
        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        var windows = VwLocalSceneStore.ListWindowScenes(deviceKey, sceneId);
        PopulateSceneWindowsFromDtos(windows);
    }

    private async Task Run(string busyMessage, Func<Task> action)
    {
        IsBusy = true;
        StatusMessage = busyMessage;

        try
        {
            await action().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Lỗi: {exception.Message}";
            _activityPublisher.Publish("Scene", exception.Message, ActivityLevel.Error);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CurrentSceneSummary));
        }
    }

    private void InitializeDefaultData()
    {
        ScreenAssignments.Clear();
        Scenes.Clear();
        SceneWindows.Clear();
        CurrentScene = null;
        AvailableStartScreens.Clear();

        if (WallNo.HasValue || _connection.WallNo.HasValue)
        {
            ReloadScenesForCurrentWall();
            UpdateStartScreenPresets();
        }
    }

    public void ReloadScenesForCurrentWall(bool preserveCurrentScene = false, string? preferredSceneId = null, string? preferredOutputId = null)
    {
        var wallNo = WallNo ?? _connection.WallNo;
        if (!wallNo.HasValue)
        {
            Scenes.Clear();
            SceneWindows.Clear();
            CurrentScene = null;
            OnPropertyChanged(nameof(HasScenes));
            OnPropertyChanged(nameof(HasSceneWindows));
            return;
        }

        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";

        if (!VwLocalSceneStore.Exists(deviceKey))
        {
            VwLocalSceneStore.SeedSampleScenes(deviceKey, wallNo.Value, availableSources: Sources.ToList());
        }

        var savedScenes = VwLocalSceneStore.ListScenes(deviceKey);
        Scenes.Clear();
        foreach (var s in savedScenes)
            Scenes.Add(s);

        var runningSid = _connection.ProbeResult?.RunningSceneId;
        if (runningSid.HasValue)
        {
            var matchedRunning = Scenes.FirstOrDefault(s => s.OutputId == runningSid.Value.ToString());
            if (matchedRunning != null && !string.IsNullOrWhiteSpace(matchedRunning.ID))
            {
                VwLocalSceneStore.SetActiveScene(deviceKey, matchedRunning.ID);
            }
        }

        ActiveScene = VwLocalSceneStore.GetActiveScene(deviceKey);
        if (Scenes.Count > 0)
        {
            VwSceneDto? target = null;
            if (preserveCurrentScene)
            {
                var targetId = preferredSceneId ?? CurrentScene?.ID;
                var targetOutputId = preferredOutputId ?? CurrentScene?.OutputId;
                target = Scenes.FirstOrDefault(s => !string.IsNullOrWhiteSpace(targetId) && s.ID == targetId)
                         ?? Scenes.FirstOrDefault(s => !string.IsNullOrWhiteSpace(targetOutputId) && s.OutputId == targetOutputId);
            }

            CurrentScene = target ?? Scenes.FirstOrDefault(s => s.ID == ActiveScene?.ID) ?? Scenes[0];
        }
        else
        {
            CurrentScene = null;
            SceneOutputId = "1";
            GridCols = 2;
            GridRows = 2;
        }

        OnPropertyChanged(nameof(HasScenes));
        OnPropertyChanged(nameof(HasSceneWindows));
    }

    partial void OnSceneNameChanged(string value) => CreateSceneCommand.NotifyCanExecuteChanged();

    partial void OnCurrentSceneChanged(VwSceneDto? value)
    {
        OnPropertyChanged(nameof(CurrentSceneSummary));
        if (value is not null && !string.IsNullOrWhiteSpace(value.ID))
        {
            if (IsCreatingNewScene)
                IsCreatingNewScene = false;

            SceneName = value.Name ?? string.Empty;
            SceneOutputId = value.OutputId ?? "1";
            GridCols = value.GridCols ?? 2;
            GridRows = value.GridRows ?? 2;
            ReloadSceneWindows(value.ID);
        }
        else if (!IsCreatingNewScene)
        {
            SceneWindows.Clear();
        }
    }

    private VwDirectSetupSceneOrchestrator? BuildDirectClientFromConnection()
    {
        var orchestrator = _connection.BuildDirectSetupOrchestrator();
        if (orchestrator == null)
            StatusMessage = "Chưa nhập IP thiết bị ở thanh kết nối.";
        return orchestrator;
    }

    private int ResolveSignalNo(string? sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return 1;

        var source = Sources.FirstOrDefault(s => s.ID == sourceId);
        return source?.SignalNo ?? (int.TryParse(sourceId, out var sId) ? sId : 1);
    }

    private static (int X, int Y)? TryResolveWallPosition(VwScreenDto screen)
    {
        if (screen.GridCol is null || screen.GridRow is null)
            return null;

        return (screen.GridCol.Value * Module.VideoWall.WPF.Api.Direct.Isapi.VwDirectDeviceConstants.UniformTileSize,
                screen.GridRow.Value * Module.VideoWall.WPF.Api.Direct.Isapi.VwDirectDeviceConstants.UniformTileSize);
    }

    public string GetDeviceSourceKey()
    {
        return VwLocalSourceStore.NormalizeControllerKey(_connection.AdHocIp);
    }

    public void SyncFromProbeResult()
    {
        var probe = _connection.ProbeResult;
        if (probe is null)
            return;

        // Số hiệu tường là KHOÁ lưu kịch bản đọc về trong kho cục bộ. Mặc định về 1 khi chưa
        // biết sẽ xếp kịch bản của tường này vào ngăn của tường khác, và lần sau đọc ra sai
        // mà không có dấu hiệu gì. Bản dò (probe) tự mang số hiệu tường nên chuỗi này gần như
        // luôn có giá trị; không có thì bỏ qua bước đồng bộ.
        var resolvedWallNo = WallNo
            ?? _connection.WallNo
            ?? probe.BoundWallNo
            ?? probe.WallNo;

        if (resolvedWallNo is null)
        {
            StatusMessage = "⚠️ Chưa dò được số hiệu tường — chạy Kết nối / Khảo sát trước khi đồng bộ kịch bản.";
            return;
        }

        var wallNo = resolvedWallNo.Value;
        var deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        var sourceKey = GetDeviceSourceKey();

        var previousSceneId = CurrentScene?.ID;
        var previousOutputId = CurrentScene?.OutputId;
        var isPreservingScene6 = string.Equals(previousOutputId, "6", StringComparison.OrdinalIgnoreCase);

        if (probe.Scenes != null)
        {
            VwLocalSceneStore.SyncScenesFromDevice(deviceKey, wallNo, probe.Scenes);
            ReloadScenesForCurrentWall(preserveCurrentScene: isPreservingScene6, preferredSceneId: previousSceneId, preferredOutputId: previousOutputId);
        }

        if (probe.RunningSceneId.HasValue)
        {
            var matchedRunning = Scenes.FirstOrDefault(s => s.OutputId == probe.RunningSceneId.Value.ToString());
            if (matchedRunning != null && !string.IsNullOrWhiteSpace(matchedRunning.ID))
            {
                VwLocalSceneStore.SetActiveScene(deviceKey, matchedRunning.ID);
                ActiveScene = matchedRunning;
            }
        }

        PopulateSources(sourceKey, probe.InputChannels);

        foreach (var row in ScreenAssignments)
        {
            if (row.SelectedSource == null && Sources.Count > 0)
                row.SelectedSource = Sources[0];
        }

        foreach (var row in SceneWindows)
        {
            if (row.SelectedSource == null && !string.IsNullOrWhiteSpace(row.SourceId))
                row.SelectedSource = Sources.FirstOrDefault(s => s.ID == row.SourceId || s.SignalNo.ToString() == row.SourceId);
        }

        AvailableOutputs.Clear();
        var availableOutputs = probe.Outputs ?? [];
        foreach (var o in availableOutputs)
        {
            AvailableOutputs.Add(o);
        }

        if (AvailableOutputs.Count > 0 && (SelectedOutputChannel == null || !AvailableOutputs.Any(o => o.Id == SelectedOutputChannel.Id)))
        {
            SelectedOutputChannel = AvailableOutputs[0];
        }

        ScreenAssignments.Clear();
        deviceKey = _connection.GetCurrentDeviceKey() ?? "127.0.0.1";
        for (var i = 0; i < AvailableOutputs.Count; i++)
        {
            var outItem = AvailableOutputs[i];
            var col = outItem.Rect?.Coordinate?.X != null ? outItem.Rect.Coordinate.X / 1920 : i % 2;
            var row = outItem.Rect?.Coordinate?.Y != null ? outItem.Rect.Coordinate.Y / 1920 : i / 2;
            var screenDto = new VwScreenDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Name = $"Màn hình {i + 1} (H{row + 1}-C{col + 1})",
                OutPutPort = outItem.OutputId > 0 ? outItem.OutputId : i + 1,
                GridCol = col,
                GridRow = row,
                WidthPx = (outItem.Rect?.Width ?? 1920).ToString(),
                HeightPx = (outItem.Rect?.Height ?? 1080).ToString(),
                ControllerId = deviceKey,
            };
            var screenRow = new ScreenAssignmentRow(screenDto);
            if (Sources.Count > 0)
                screenRow.SelectedSource = Sources[i % Sources.Count];
            ScreenAssignments.Add(screenRow);
        }
        if (ScreenAssignments.Count > 0)
            SelectedScreenAssignment = ScreenAssignments[0];

        PopulateReadSurveyInfo();
        UpdateStartScreenPresets();

        if (probe.ActiveWindows != null && probe.ActiveWindows.Count > 0)
        {
            var runningScene = probe.RunningSceneId.HasValue
                ? Scenes.FirstOrDefault(s => s.OutputId == probe.RunningSceneId.Value.ToString())
                : null;
            var targetSceneForWindows = runningScene ?? CurrentScene ?? Scenes.FirstOrDefault();

            if (targetSceneForWindows != null && !string.IsNullOrWhiteSpace(targetSceneForWindows.ID))
            {
                var syncedWindows = VwLocalSceneStore.SyncWindowsFromDevice(deviceKey, targetSceneForWindows.ID, probe.ActiveWindows, Sources.ToList());
                if (CurrentScene?.ID == targetSceneForWindows.ID)
                {
                    PopulateSceneWindowsFromDtos(syncedWindows);
                }
            }
        }

        if (isPreservingScene6)
        {
            var scene6 = Scenes.FirstOrDefault(s => s.OutputId == "6" || (!string.IsNullOrWhiteSpace(previousSceneId) && s.ID == previousSceneId));
            if (scene6 != null && !string.IsNullOrWhiteSpace(scene6.ID))
            {
                if (CurrentScene?.ID != scene6.ID)
                    CurrentScene = scene6;
                else
                    ReloadSceneWindows(scene6.ID);
            }
        }
        else if (probe.RunningSceneId.HasValue)
        {
            var matchedRunning = Scenes.FirstOrDefault(s => s.OutputId == probe.RunningSceneId.Value.ToString());
            if (matchedRunning != null)
                CurrentScene = matchedRunning;
        }
        else if (CurrentScene == null && Scenes.Count > 0)
        {
            CurrentScene = Scenes.FirstOrDefault(s => s.ID == ActiveScene?.ID) ?? Scenes[0];
        }

        StatusMessage = $"Đã tự động nạp {Sources.Count} nguồn tín hiệu, {AvailableOutputs.Count} cổng ra, {probe.ActiveWindows?.Count ?? 0} ô cửa sổ và giữ kịch bản '{CurrentScene?.Name}'.";
    }

    public void UpdateStartScreenPresets()
    {
        AvailableStartScreens.Clear();
        var probeOutputs = _connection.ProbeResult?.Outputs;
        var tile = Tile;
        if (probeOutputs != null && probeOutputs.Count > 0)
        {
            var ordered = probeOutputs.OrderBy(o => o.Y).ThenBy(o => o.X).ToList();
            var distinctX = probeOutputs.Select(o => o.X).Distinct().OrderBy(x => x).ToList();
            var distinctY = probeOutputs.Select(o => o.Y).Distinct().OrderBy(y => y).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                var o = ordered[i];
                var col = distinctX.Count > 1 ? distinctX.IndexOf(o.X) + 1 : (tile > 0 ? (o.X / tile) + 1 : 1);
                var row = distinctY.Count > 1 ? distinctY.IndexOf(o.Y) + 1 : (tile > 0 ? (o.Y / tile) + 1 : 1);
                AvailableStartScreens.Add(new StartScreenPreset($"Màn {i + 1}", o.X, o.Y, i + 1, row, col));
            }
        }
        else
        {
            var cols = GridCols > 0 ? GridCols.Value : 8;
            var rows = GridRows > 0 ? GridRows.Value : 4;
            var fallback = BuildFallbackScreens(cols, rows, tile);
            foreach (var s in fallback)
            {
                AvailableStartScreens.Add(s);
            }
        }

        foreach (var row in SceneWindows)
        {
            row.NotifyStartScreenChanged();
        }
        AddSceneWindowCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(TotalWallWidth));
        OnPropertyChanged(nameof(TotalWallHeight));
    }

    private void PopulateReadSurveyInfo()
    {
        var probeOutputs = _connection.ProbeResult?.Outputs;
        for (var i = 0; i < SceneWindows.Count; i++)
        {
            var row = SceneWindows[i];
            VwISAPIOutputItem? matchedOutput = null;

            if (probeOutputs != null && probeOutputs.Count > 0)
            {
                matchedOutput = probeOutputs.FirstOrDefault(o => o.X == row.X && o.Y == row.Y)
                                ?? (probeOutputs.Count > i ? probeOutputs[i] : null);
            }

            if (matchedOutput != null)
            {
                row.ReadSurveyInfo = $"Cổng {matchedOutput.Id} (HW:{matchedOutput.OutputId}) · {matchedOutput.Width}×{matchedOutput.Height} · ({matchedOutput.X},{matchedOutput.Y})";
            }
            else
            {
                row.ReadSurveyInfo = $"Màn {row.OrderNo ?? (i + 1)} · {row.W ?? VwDirectDeviceConstants.UniformTileSize}×{row.H ?? VwDirectDeviceConstants.UniformTileSize} · ({row.X ?? 0},{row.Y ?? 0})";
            }
        }
    }

    private void PopulateSources(string deviceKey, IEnumerable<VwISAPIInputChannel>? channels)
    {
        var probeSources = (channels ?? [])
            .Select(ch =>
            {
                var isIpPort = !string.IsNullOrWhiteSpace(ch.InputPortType)
                               && (ch.InputPortType.Contains("IP", StringComparison.OrdinalIgnoreCase)
                                   || ch.InputPortType.Contains("Stream", StringComparison.OrdinalIgnoreCase));

                var ip = ch.PortInBoard?.IpAddress?.Trim();
                var port = ch.PortInBoard?.Port > 0 ? ch.PortInBoard.Port : 554;
                var url = !string.IsNullOrWhiteSpace(ch.StreamUrl)
                    ? ch.StreamUrl
                    : (isIpPort && !string.IsNullOrWhiteSpace(ip)
                        ? (ip.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase)
                            ? ip
                            : $"rtsp://{ip}:{port}/Streaming/Channels/101")
                        : null);

                return new VwSourceDto
                {
                    ID = ch.Id.ToString(),
                    Code = isIpPort ? $"IP_{ch.Id}" : $"HDMI_{ch.Id}",
                    Name = string.IsNullOrWhiteSpace(ch.Name)
                        ? (isIpPort ? $"Camera IP {ch.Id}" : $"Kênh {ch.Id}")
                        : ch.Name,
                    SourceType = isIpPort ? "ip_stream" : "local_signal",
                    SignalType = isIpPort ? (ch.InputPortType ?? "RTSP") : "HDMI",
                    SignalNo = ch.Id,
                    OrderNo = ch.Id,
                    Url = url,
                    StreamingChannelId = ch.StreamingChannelId,
                };
            })
            .ToList();

        var controllerKey = VwLocalSourceStore.NormalizeControllerKey(deviceKey);
        var customSources = VwLocalSourceStore.Load(controllerKey);
        var merged = VwLocalSourceStore.Merge(probeSources, customSources);

        Sources.Clear();
        foreach (var s in merged)
        {
            Sources.Add(s);
        }

        foreach (var row in SceneWindows)
        {
            row.RebuildSubCells(Sources);
            var matched = Sources.FirstOrDefault(s => s.ID == row.SourceId || s.SignalNo.ToString() == row.SourceId);
            if (matched != null)
                row.SelectedSource = matched;
            else if (row.SelectedSource == null && Sources.Count > 0)
            {
                if (int.TryParse(row.SourceId, out var sIdx) && sIdx >= 1 && sIdx <= Sources.Count)
                    row.SelectedSource = Sources[sIdx - 1];
                else
                    row.SelectedSource = Sources[0];
            }
        }
    }

    private List<VwDirectWindowInput> BuildDirectWindows()
    {
        return SceneWindows
            .OrderBy(w => w.ZIndex ?? 1)
            .ThenBy(w => w.OrderNo ?? 0)
            .Select(BuildDirectWindowFromRow)
            .ToList();
    }

    private VwDirectWindowInput BuildDirectWindowFromRow(SceneWindowRow w)
    {
        var tileSize = VwDirectDeviceConstants.UniformTileSize;
        var snappedX = Math.Max(0, VisualWallCanvas.SnapToGrid(w.X ?? 0, tileSize));
        var snappedY = Math.Max(0, VisualWallCanvas.SnapToGrid(w.Y ?? 0, tileSize));
        var snappedW = Math.Max(tileSize, VisualWallCanvas.SnapSize(w.W ?? tileSize, tileSize));
        var snappedH = Math.Max(tileSize, VisualWallCanvas.SnapSize(w.H ?? tileSize, tileSize));

        var maxWallW = TotalWallWidth > 0 ? TotalWallWidth : 3840;
        var maxWallH = TotalWallHeight > 0 ? TotalWallHeight : 3840;
        var clampedW = Math.Min(snappedW, maxWallW);
        var clampedH = Math.Min(snappedH, maxWallH);
        var clampedX = Math.Min(snappedX, Math.Max(0, maxWallW - clampedW));
        var clampedY = Math.Min(snappedY, Math.Max(0, maxWallH - clampedH));

        return new VwDirectWindowInput
        {
            X = clampedX,
            Y = clampedY,
            W = clampedW,
            H = clampedH,
            ZIndex = w.ZIndex ?? 1,
            SignalNo = ResolveSignalNo(w.SourceId),
            Label = w.Label ?? w.Name,
            WindowMode = w.WindowMode,
            SubSignalNos = w.WindowMode > 1
                ? w.SubCells.Select(cell => ResolveSignalNo(cell.SelectedSource?.ID)).ToList()
                : [ResolveSignalNo(w.SourceId)],
            SourceType = w.SelectedSource?.SourceType,
            StreamUrl = w.SelectedSource?.Url,
            StreamProtocol = w.SelectedSource?.SignalType,
            StreamingChannelId = w.SelectedSource?.StreamingChannelId,
            SubWindows = w.WindowMode > 1
                ? w.SubCells.Select(cell => new VwDirectSubWindowInput
                {
                    SubId = cell.Index,
                    SignalNo = ResolveSignalNo(cell.SelectedSource?.ID),
                    SourceType = cell.SelectedSource?.SourceType,
                    StreamUrl = cell.SelectedSource?.Url,
                    StreamProtocol = cell.SelectedSource?.SignalType,
                    StreamingChannelId = cell.SelectedSource?.StreamingChannelId,
                }).ToList()
                : [new VwDirectSubWindowInput
                {
                    SubId = 1,
                    SignalNo = ResolveSignalNo(w.SourceId),
                    SourceType = w.SelectedSource?.SourceType,
                    StreamUrl = w.SelectedSource?.Url,
                    StreamProtocol = w.SelectedSource?.SignalType,
                    StreamingChannelId = w.SelectedSource?.StreamingChannelId,
                }],
        };
    }
}
