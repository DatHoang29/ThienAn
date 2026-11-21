using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Api.Direct.Isapi;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.Storage;
using Services.Shared.Events;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Description: Mục hiển thị trong danh sách kịch bản kiểm thử (kịch bản dựng sẵn hoặc file đã lưu)
/// Created date: 13/09/2026
/// </summary>
public sealed class VwScenarioListItem
{
    /// <summary>Tên hiển thị của mục kịch bản trong danh sách.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Cờ cho biết đây có phải là kịch bản dựng sẵn hay không.</summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>Đối tượng kịch bản dựng sẵn tương ứng nếu có.</summary>
    public VwBuiltInScenario? BuiltIn { get; init; }

    /// <summary>Tên file lưu trữ kịch bản nếu là kịch bản người dùng lưu.</summary>
    public string? SavedFileName { get; init; }
}

/// <summary>
/// Description: ViewModel quản lý danh mục API ISAPI, biên soạn và chạy chuỗi kịch bản kiểm thử thiết bị
/// Created date: 13/09/2026
/// </summary>
public sealed partial class ScenarioViewModel : ObservableObject
{
    private readonly ConnectionViewModel _connection;
    private readonly ActivityPublisher _activityPublisher;
    private readonly IPublisher _publisher;
    private readonly IUserConfirmation? _userConfirmation;

    /// <summary>Toàn bộ danh mục API ISAPI mẫu được hỗ trợ.</summary>
    public IReadOnlyList<VwIsapiPresetItem> AllPresets => VwIsapiPresetList.Presets;

    /// <summary>Danh sách các nhóm phân loại API ISAPI (Board, Decoding, Window...).</summary>
    public ObservableCollection<string> ApiGroups { get; } =
    [
        "Tất cả",
        "Board",
        "Decoding",
        "OutputChannel",
        "SignalSource",
        "VideoWall",
        "Plan",
        "Scene",
        "Screen",
        "TextLed",
        "Wallpaper",
        "Window"
    ];

    /// <summary>Nhóm API ISAPI đang được lọc hiển thị.</summary>
    [ObservableProperty]
    private string _selectedApiGroup = "Tất cả";

    /// <summary>Từ khóa tìm kiếm API trong danh mục mẫu.</summary>
    [ObservableProperty]
    private string _apiSearchText = string.Empty;

    /// <summary>Danh sách các API ISAPI mẫu sau khi lọc theo nhóm và từ khóa.</summary>
    public ObservableCollection<VwIsapiPresetItem> FilteredPresets { get; } = [];

    /// <summary>Mẫu API ISAPI đang được chọn trong danh mục.</summary>
    [ObservableProperty]
    private VwIsapiPresetItem? _selectedPresetInCatalog;

    /// <summary>Danh sách các bước (Step) trong kịch bản đang biên soạn.</summary>
    public ObservableCollection<VwScenarioStepViewModel> Steps { get; } = [];

    /// <summary>Danh sách các kịch bản có sẵn và kịch bản đã lưu.</summary>
    public ObservableCollection<VwScenarioListItem> ScenarioList { get; } = [];

    /// <summary>Bước kịch bản đang được chọn để xem hoặc chỉnh sửa thông số.</summary>
    [ObservableProperty]
    private VwScenarioStepViewModel? _selectedStep;

    /// <summary>Tên của kịch bản hiện tại.</summary>
    [ObservableProperty]
    private string _scenarioName = "Kịch bản mới";

    /// <summary>Mục kịch bản đang được chọn từ danh sách kịch bản.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunSelectedScenarioCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteScenarioCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResumeScenarioCommand))]
    [NotifyPropertyChangedFor(nameof(IsBuiltInSelected))]
    private VwScenarioListItem? _selectedScenarioItem;

    /// <summary>Cờ cho biết kịch bản đang chọn có phải là kịch bản dựng sẵn hay không.</summary>
    public bool IsBuiltInSelected => SelectedScenarioItem?.IsBuiltIn ?? false;

    /// <summary>Cờ trạng thái kịch bản đang được thực thi.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunSelectedScenarioCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResumeScenarioCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteScenarioCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunOverlappingSizeTestCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunErrorValidationSuiteCommand))]
    private bool _isRunning;

    /// <summary>Thông báo trạng thái thực thi hoặc hướng dẫn trên giao diện kịch bản.</summary>
    [ObservableProperty]
    private string _statusMessage = "Chọn API ở danh mục giữa rồi bấm \"+ Thêm vào kịch bản\" hoặc nhấp đúp chuột để bắt đầu.";

    // ── 1. Kịch bản dựng sẵn ─────────────────────────────────────────────
    /// <summary>Danh sách các kịch bản dựng sẵn của hệ thống.</summary>
    public ObservableCollection<VwBuiltInScenario> BuiltInScenarios { get; } = [];

    /// <summary>Tên Scene sử dụng cho kịch bản dựng sẵn.</summary>
    [ObservableProperty]
    private string _builtInSceneName = "Scene_San_Sang";

    /// <summary>Số thứ tự WallNo áp dụng trong kịch bản dựng sẵn.</summary>
    [ObservableProperty]
    private int? _builtInWallNo = 1;

    /// <summary>Số cột ma trận lưới cho kịch bản dựng sẵn.</summary>
    [ObservableProperty]
    private int? _builtInGridCols = 2;

    /// <summary>Số hàng ma trận lưới cho kịch bản dựng sẵn.</summary>
    [ObservableProperty]
    private int? _builtInGridRows = 2;

    /// <summary>Mã Code của kịch bản dựng sẵn đang được chọn.</summary>
    [ObservableProperty]
    private string _builtInSelectedSceneCode = string.Empty;

    /// <summary>Cờ chạy thử nghiệm DryRun không gửi lệnh thật xuống thiết bị.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunOverlappingSizeTestCommand))]
    private bool _builtInDryRun = true;

    /// <summary>Đường dẫn lưu trữ file ảnh snapshot gần nhất.</summary>
    [ObservableProperty]
    private string _lastSnapshotSavedPath = string.Empty;

    // ── 2. Thời gian chờ giữa các bước (Delay) ───────────────────────────
    /// <summary>Thời gian trễ (mili-giây) giữa các bước thực thi kịch bản.</summary>
    [ObservableProperty]
    private int _delayBetweenStepsMs = 400;

    // ── 3. Hai nguồn tranh vùng — canh kích thước ─────────────────────────
    /// <summary>ID nguồn tín hiệu A trong bài test tranh chấp vùng hiển thị.</summary>
    [ObservableProperty]
    private string _overlapSourceIdA = "1";

    /// <summary>ID nguồn tín hiệu B trong bài test tranh chấp vùng hiển thị.</summary>
    [ObservableProperty]
    private string _overlapSourceIdB = "2";

    /// <summary>Chiều rộng mục tiêu cho bài test tranh chấp vùng hiển thị.</summary>
    [ObservableProperty]
    private int _overlapTargetWidth = 1920;

    /// <summary>Chiều cao mục tiêu cho bài test tranh chấp vùng hiển thị.</summary>
    [ObservableProperty]
    private int _overlapTargetHeight = 1080;

    /// <summary>ID của Scene trong bài test tranh chấp vùng hiển thị.</summary>
    [ObservableProperty]
    private int _overlapSceneId = 1;

    // ── 4. Mất kết nối & Chạy tiếp từ bước N ─────────────────────────────
    /// <summary>Chỉ số của bước thất bại gần nhất phục vụ việc chạy tiếp (Resume).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResumeScenarioCommand))]
    private int? _lastFailedStepIndex;

    /// <summary>Nội dung nhãn nút bấm tiếp tục chạy kịch bản từ bước lỗi.</summary>
    [ObservableProperty]
    private string _resumeButtonText = "▶ Chạy tiếp từ bước N";

    /// <summary>Cờ gửi request thật xuống thiết bị khi chạy bộ kiểm thử bắt lỗi (Error Suite).</summary>
    [ObservableProperty]
    private bool _errorSuiteSendReal;

    public ScenarioViewModel(
        ConnectionViewModel connection,
        ActivityPublisher activityPublisher,
        IPublisher publisher,
        IUserConfirmation? userConfirmation = null)
    {
        _connection = connection;
        _activityPublisher = activityPublisher;
        _publisher = publisher;
        _userConfirmation = userConfirmation;

        ApplyPresetFilter();
        SelectedPresetInCatalog = FilteredPresets.FirstOrDefault();

        FilteredPresets.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFilteredPresets));
        Steps.CollectionChanged += OnStepsCollectionChanged;

        _connection.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConnectionViewModel.AdHocIp))
            {
                RunOverlappingSizeTestCommand.NotifyCanExecuteChanged();
                RunErrorValidationSuiteCommand.NotifyCanExecuteChanged();
            }
        };

        RefreshScenarioList();
    }

    public void RefreshScenarioList()
    {
        VwScenarioStore.EnsureDefaultsSeeded();
        var currentSelection = SelectedScenarioItem;
        ScenarioList.Clear();

        foreach (var name in VwScenarioStore.ListAll())
        {
            ScenarioList.Add(new VwScenarioListItem
            {
                DisplayName = name,
                IsBuiltIn = false,
                BuiltIn = null,
                SavedFileName = name,
            });
        }

        if (currentSelection != null)
        {
            var match = ScenarioList.FirstOrDefault(item => item.SavedFileName == currentSelection.SavedFileName);
            if (match != null)
            {
                SelectedScenarioItem = match;
                return;
            }
        }

        if (ScenarioList.Count > 0 && SelectedScenarioItem == null && !_suppressAutoLoad)
        {
            SelectedScenarioItem = ScenarioList[0];
        }
    }

    partial void OnSelectedApiGroupChanged(string value) => ApplyPresetFilter();

    partial void OnApiSearchTextChanged(string value) => ApplyPresetFilter();

    private void ApplyPresetFilter()
    {
        FilteredPresets.Clear();
        var search = ApiSearchText?.Trim();
        var group = SelectedApiGroup;

        var query = AllPresets.AsEnumerable();

        if (!string.IsNullOrEmpty(group) && group != "Tất cả")
        {
            query = query.Where(p => p.Group.ToString().Equals(group, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.Section.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Url.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Method.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
            FilteredPresets.Add(item);

        OnPropertyChanged(nameof(HasFilteredPresets));

        if (SelectedPresetInCatalog == null || !FilteredPresets.Contains(SelectedPresetInCatalog))
            SelectedPresetInCatalog = FilteredPresets.FirstOrDefault();
    }

    /// <summary>Cờ kiểm tra danh sách API lọc theo điều kiện có phần tử hay không.</summary>
    public bool HasFilteredPresets => FilteredPresets.Count > 0;

    private bool _suppressAutoLoad;

    partial void OnSelectedScenarioItemChanged(VwScenarioListItem? value)
    {
        if (_suppressAutoLoad || value == null)
            return;

        if (!string.IsNullOrWhiteSpace(value.SavedFileName))
        {
            LoadScenarioByName(value.SavedFileName);
        }
        else
        {
            ScenarioName = value.DisplayName;
        }
    }

    [RelayCommand]
    private void AddStep(VwIsapiPresetItem? preset)
    {
        var targetPreset = preset ?? SelectedPresetInCatalog;
        if (targetPreset == null)
            return;

        var step = new VwScenarioStepViewModel(targetPreset);
        Steps.Add(step);
        SelectedStep = step;
        StatusMessage = $"Đã thêm bước #{Steps.Count}: [{targetPreset.Method}] {targetPreset.Section} — {targetPreset.Name}";
    }

    [RelayCommand]
    private void RemoveStep(VwScenarioStepViewModel? step)
    {
        if (step != null)
        {
            Steps.Remove(step);
            StatusMessage = $"Đã xoá bước. Kịch bản còn {Steps.Count} bước.";
        }
    }

    [RelayCommand]
    private void MoveStepUp(VwScenarioStepViewModel? step)
    {
        if (step == null)
            return;

        var index = Steps.IndexOf(step);
        if (index > 0)
        {
            Steps.Move(index, index - 1);
            SelectedStep = step;
        }
    }

    [RelayCommand]
    private void MoveStepDown(VwScenarioStepViewModel? step)
    {
        if (step == null)
            return;

        var index = Steps.IndexOf(step);
        if (index >= 0 && index < Steps.Count - 1)
        {
            Steps.Move(index, index + 1);
            SelectedStep = step;
        }
    }

    [RelayCommand]
    private void NewScenario()
    {
        Steps.Clear();
        var defaultName = $"Kịch bản {DateTime.Now:HH:mm:ss}";
        ScenarioName = defaultName;
        _suppressAutoLoad = true;
        SelectedScenarioItem = null;
        _suppressAutoLoad = false;
        StatusMessage = $"Đã tạo \"{defaultName}\". Chọn API từ thư viện ở giữa để thêm bước.";
    }

    [RelayCommand]
    private void SaveScenario()
    {
        var name = ScenarioName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "⚠️ Vui lòng nhập tên kịch bản trước khi lưu.";
            return;
        }

        for (int idx = 0; idx < Steps.Count; idx++)
        {
            var step = Steps[idx];
            if (!step.Form.Validate(out var stepError))
            {
                StatusMessage = $"⚠️ Bước #{idx + 1} ({step.Preset.Name}) thiếu tham số bắt buộc: {stepError}";
                return;
            }
        }

        var data = new VwScenarioData
        {
            Name = name,
            Steps = Steps.Select(s => new VwScenarioStepData
            {
                Section = s.Preset.Section,
                PathFieldValues = s.Form.PathFields.ToDictionary(f => f.Definition.Key, f => f.Value ?? string.Empty),
                Body = s.RawBody,
            }).ToList(),
        };

        VwScenarioStore.Save(data);
        _suppressAutoLoad = true;
        RefreshScenarioList();
        SelectedScenarioItem = ScenarioList.FirstOrDefault(item => item.SavedFileName == data.Name);
        _suppressAutoLoad = false;

        StatusMessage = $"Đã lưu kịch bản \"{data.Name}\" ({data.Steps.Count} bước).";
        _activityPublisher.Publish("Scenario", $"Đã lưu kịch bản \"{data.Name}\" ({data.Steps.Count} bước).", ActivityLevel.Success);
    }

    public void LoadScenarioByName(string name)
    {
        var data = VwScenarioStore.Load(name);
        if (data == null)
        {
            StatusMessage = $"⚠️ Không thể đọc file kịch bản \"{name}\".";
            return;
        }

        Steps.Clear();
        ScenarioName = data.Name;

        foreach (var stepData in data.Steps)
        {
            var preset = VwIsapiPresetList.Presets.FirstOrDefault(p => p.Section == stepData.Section);
            if (preset == null)
                continue;

            var stepVm = new VwScenarioStepViewModel(preset, stepData.Body);
            if (stepData.PathFieldValues != null)
            {
                foreach (var kvp in stepData.PathFieldValues)
                {
                    var field = stepVm.Form.PathFields.FirstOrDefault(f => f.Definition.Key.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                    if (field != null)
                        field.Value = kvp.Value;
                }
            }
            Steps.Add(stepVm);
        }

        StatusMessage = $"Đã nạp kịch bản \"{data.Name}\" ({Steps.Count} bước).";
        SelectedStep = Steps.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteScenario))]
    private void DeleteScenario()
    {
        if (SelectedScenarioItem == null || SelectedScenarioItem.IsBuiltIn || string.IsNullOrWhiteSpace(SelectedScenarioItem.SavedFileName))
            return;

        var name = SelectedScenarioItem.SavedFileName;
        VwScenarioStore.Delete(name);
        _suppressAutoLoad = true;
        SelectedScenarioItem = null;
        _suppressAutoLoad = false;
        RefreshScenarioList();

        StatusMessage = $"Đã xoá kịch bản \"{name}\".";
        _activityPublisher.Publish("Scenario", $"Đã xoá kịch bản \"{name}\".", ActivityLevel.Warning);
    }

    [RelayCommand(CanExecute = nameof(CanRunSelected))]
    private Task RunSelectedScenario(CancellationToken ct) =>
        RunScenarioCore(0, ct);

    [RelayCommand(CanExecute = nameof(CanResumeScenario))]
    private Task ResumeScenario(CancellationToken cancellationToken) =>
        RunScenarioCore(LastFailedStepIndex ?? 0, cancellationToken);

    private async Task RunScenarioCore(int fromStep, CancellationToken cancellationToken)
    {
        if (Steps.Count == 0 || fromStep >= Steps.Count)
            return;

        var directClient = _connection.BuildDirectClient();
        if (directClient == null)
        {
            StatusMessage = "⚠️ Nhập IP thiết bị ở thanh kết nối phía trên trước khi chạy.";
            _activityPublisher.Publish("Scenario", "Chưa nhập thông tin kết nối trực tiếp thiết bị.", ActivityLevel.Error);
            return;
        }
        var client = directClient;

        IsRunning = true;
        RunSelectedScenarioCommand.NotifyCanExecuteChanged();
        ResumeScenarioCommand.NotifyCanExecuteChanged();

        StatusMessage = $"Đang chạy kịch bản \"{ScenarioName}\" từ bước {fromStep + 1}/{Steps.Count}...";
        _activityPublisher.Publish("Scenario", $"Bắt đầu chạy kịch bản \"{ScenarioName}\" từ bước {fromStep + 1}/{Steps.Count}...", ActivityLevel.Info);

        var completedCount = 0;
        try
        {
            for (var i = fromStep; i < Steps.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (i > fromStep && DelayBetweenStepsMs > 0)
                {
                    var delay = Math.Clamp(DelayBetweenStepsMs, 0, 60000);
                    _activityPublisher.Publish("Scenario", $"Chờ {delay} ms trước bước kế tiếp...", ActivityLevel.Info);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(true);
                }

                var stepVm = Steps[i];
                if (!stepVm.Form.Validate(out var stepError))
                {
                    LastFailedStepIndex = i;
                    ResumeButtonText = $"▶ Chạy tiếp từ bước #{i + 1}";
                    StatusMessage = $"⚠️ Bước #{i + 1}/{Steps.Count} ({stepVm.Preset.Name}) thiếu tham số bắt buộc: {stepError}";
                    _activityPublisher.Publish(
                        "Scenario",
                        $"Kịch bản \"{ScenarioName}\" DỪNG tại bước #{i + 1}/{Steps.Count} do thiếu tham số: {stepError}",
                        ActivityLevel.Warning);
                    return;
                }

                var path = stepVm.Form.BuildPath();
                if (System.Text.RegularExpressions.Regex.IsMatch(path, @"\{[a-zA-Z0-9_]+\}"))
                {
                    LastFailedStepIndex = i;
                    ResumeButtonText = $"▶ Chạy tiếp từ bước #{i + 1}";
                    StatusMessage = $"⚠️ Bước #{i + 1}/{Steps.Count} ({stepVm.Preset.Name}) đường dẫn còn chứa tham số chưa nhập: {path}";
                    _activityPublisher.Publish(
                        "Scenario",
                        $"Kịch bản \"{ScenarioName}\" DỪNG tại bước #{i + 1}/{Steps.Count} do đường dẫn còn chứa tham số: {path}",
                        ActivityLevel.Warning);
                    return;
                }

                var method = string.IsNullOrWhiteSpace(stepVm.Preset.Method) ? "GET" : stepVm.Preset.Method.Trim().ToUpperInvariant();
                var body = string.IsNullOrWhiteSpace(stepVm.RawBody) ? null : stepVm.RawBody;
                var contentType = body != null
                    ? (stepVm.Preset.Section is "9.7.2.10" or "9.7.2.13" ? "application/json" : "application/xml")
                    : null;

                var stepResult = await client.SendIsapi(method, path, body, contentType, cancellationToken).ConfigureAwait(true);

                if (!stepResult.Success)
                {
                    LastFailedStepIndex = i;
                    ResumeButtonText = $"▶ Chạy tiếp từ bước #{i + 1}";
                    StatusMessage = $"❌ Dừng tại bước {i + 1}/{Steps.Count}: [{method}] {stepVm.Preset.Section} — {stepResult.Message}";
                    _activityPublisher.Publish(
                        "Scenario",
                        $"Kịch bản \"{ScenarioName}\" DỪNG tại bước {i + 1}/{Steps.Count} ({stepVm.Preset.Name}) do lỗi: {stepResult.Message}",
                        ActivityLevel.Error);
                    return;
                }

                completedCount++;
            }

            LastFailedStepIndex = null;
            StatusMessage = $"✅ Hoàn thành kịch bản \"{ScenarioName}\": {completedCount} bước thành công.";
            _activityPublisher.Publish(
                "Scenario",
                $"Hoàn thành kịch bản \"{ScenarioName}\": {completedCount} bước thành công.",
                ActivityLevel.Success);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusMessage = "⚠️ Đã huỷ chạy kịch bản.";
            _activityPublisher.Publish("Scenario", "Đã huỷ chạy kịch bản.", ActivityLevel.Warning);
        }
        catch (Exception ex)
        {
            LastFailedStepIndex = fromStep + completedCount;
            ResumeButtonText = $"▶ Chạy tiếp từ bước #{(LastFailedStepIndex ?? 0) + 1}";
            StatusMessage = $"❌ Lỗi khi chạy kịch bản: {ex.Message}";
            _activityPublisher.Publish("Scenario", $"Lỗi kịch bản tại bước #{(LastFailedStepIndex ?? 0) + 1}: {ex.Message}", ActivityLevel.Error);
        }
        finally
        {
            IsRunning = false;
            RunSelectedScenarioCommand.NotifyCanExecuteChanged();
            ResumeScenarioCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunOverlappingSizeTest))]
    private async Task RunOverlappingSizeTest(CancellationToken ct)
    {
        IsRunning = true;
        try
        {
            _activityPublisher.Publish("Scenario", "Bắt đầu kịch bản: 2 nguồn tranh vùng — canh kích thước...", ActivityLevel.Info);

            if (!BuiltInDryRun && _userConfirmation != null && !_userConfirmation.Confirm("Xác nhận đẩy thật", "Bạn có chắc chắn muốn đẩy cấu hình thật xuống thiết bị?"))
            {
                StatusMessage = "Đã huỷ thao tác đẩy thật.";
                return;
            }

            var w1 = new VwDirectWindowInput
            {
                X = 0,
                Y = 0,
                W = OverlapTargetWidth,
                H = OverlapTargetHeight,
                ZIndex = 1,
                SignalNo = int.TryParse(OverlapSourceIdA, out var sA) ? sA : 1,
                Label = "Cửa sổ Lớn (Nền)",
            };

            var w2 = new VwDirectWindowInput
            {
                X = 0,
                Y = 0,
                W = Math.Max(100, OverlapTargetWidth / 2),
                H = Math.Max(100, OverlapTargetHeight / 2),
                ZIndex = 2,
                SignalNo = int.TryParse(OverlapSourceIdB, out var sB) ? sB : 2,
                Label = "Cửa sổ Nhỏ (Đè lên)",
            };

            var orchestrator = _connection.BuildDirectSetupOrchestrator();
            if (orchestrator == null)
            {
                StatusMessage = "⚠️ Nhập IP thiết bị ở thanh kết nối phía trên trước khi chạy.";
                return;
            }

            var effectiveWallNo = BuiltInWallNo ?? _connection.WallNo;
            if (effectiveWallNo is null)
            {
                StatusMessage = "⚠️ Cần nhập WallNo trước khi chạy kiểm thử tranh vùng (không tự chọn để tránh đẩy nhầm tường).";
                return;
            }

            var input = new VwDirectPushSceneInput
            {
                SceneId = OverlapSceneId,
                WallNo = effectiveWallNo,
                DryRun = BuiltInDryRun,
                ResetWindows = false,
                Activate = false,
                Windows = [w1, w2],
            };

            var result = await orchestrator.Execute(input, ct).ConfigureAwait(true);
            await VwDeviceStepLogging.PublishSteps(_publisher, _activityPublisher, "OverlapTest", result.Steps, ct).ConfigureAwait(true);

            var failedSteps = result.Steps.Count(s => !s.Success && !s.Skipped);
            StatusMessage = result.Success
                ? $"✅ {(BuiltInDryRun ? "Chạy thử" : "Đẩy thật")} thành công: {result.Windows.Count} cửa sổ tranh vùng."
                : $"❌ Thất bại: {result.Message} ({failedSteps} bước lỗi).";

            _activityPublisher.Publish("Scenario",
                result.Success
                    ? $"Đã dựng {result.Windows.Count} cửa sổ tranh vùng (SID {OverlapSceneId}): {string.Join(", ", result.Windows.Select(w => $"{w.Label} {w.W}x{w.H}"))}"
                    : $"Kịch bản 2 nguồn tranh vùng thất bại: {result.Message}",
                result.Success ? ActivityLevel.Success : ActivityLevel.Error);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Lỗi kịch bản 2 nguồn tranh vùng: {ex.Message}";
            _activityPublisher.Publish("Scenario", $"Lỗi: {ex.Message}", ActivityLevel.Error);
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunErrorValidationSuite))]
    private async Task RunErrorValidationSuite(CancellationToken ct)
    {
        IsRunning = true;
        try
        {
            if (ErrorSuiteSendReal)
            {
                var userConfirmed = (_userConfirmation ?? new MessageBoxUserConfirmation()).Confirm(
                    "Gửi thật xuống thiết bị",
                    "Case B sẽ THẬT SỰ tạo 3 cửa sổ trên tường (không phải thử). Case A/C dự kiến bị thiết bị từ chối nhưng vẫn là request thật. Tiếp tục?");
                if (!userConfirmed)
                {
                    StatusMessage = "Đã huỷ chạy bộ kiểm thử lỗi.";
                    return;
                }
            }

            var skipGuardrails = ErrorSuiteSendReal;
            var dryRun = !ErrorSuiteSendReal;

            _activityPublisher.Publish("ErrorSuite", "Bắt đầu chạy bộ kiểm thử lỗi (3 cases)...", ActivityLevel.Info);

            var orchestrator = _connection.BuildDirectSetupOrchestrator();
            if (orchestrator == null)
            {
                StatusMessage = "⚠️ Nhập IP thiết bị trước khi chạy bộ kiểm thử lỗi.";
                return;
            }

            var effectiveWallNo = BuiltInWallNo ?? _connection.WallNo;
            if (effectiveWallNo is null)
            {
                StatusMessage = "⚠️ Cần nhập WallNo trước khi chạy bộ kiểm thử lỗi (không tự chọn để tránh đẩy nhầm tường).";
                return;
            }

            // Case A: vượt maxWindowNums thật của thiết bị (guardrail VwDirectSetupSceneOrchestrator.cs:88-91)
            var caseA = await orchestrator.Execute(new VwDirectPushSceneInput
            {
                SceneId = 1,
                WallNo = effectiveWallNo,
                Windows = Enumerable.Range(0, 999)
                    .Select(i => new VwDirectWindowInput
                    {
                        X = 0,
                        Y = 0,
                        W = 100,
                        H = 100,
                        ZIndex = 1,
                        SignalNo = 1,
                        Label = $"W{i}",
                    })
                    .ToList(),
                DryRun = dryRun,
                SkipGuardrails = skipGuardrails,
            }, ct).ConfigureAwait(true);
            await VwDeviceStepLogging.PublishSteps(_publisher, _activityPublisher, "ErrorSuite-CaseA", caseA.Steps, ct).ConfigureAwait(true);
            LogCase("A", "999 cửa sổ vượt maxWindowNums", expectReject: true, caseA);

            if (DelayBetweenStepsMs > 0)
                await Task.Delay(Math.Clamp(DelayBetweenStepsMs, 0, 60000), ct).ConfigureAwait(true);

            // Case B: 1 nguồn gán cho 3 cửa sổ — datasheet cho phép copy nguồn, KỲ VỌNG KHÔNG bị chặn
            var caseB = await orchestrator.Execute(new VwDirectPushSceneInput
            {
                SceneId = 1,
                WallNo = effectiveWallNo,
                Windows =
                [
                    new VwDirectWindowInput
                    {
                        X = 0,
                        Y = 0,
                        W = 100,
                        H = 100,
                        ZIndex = 1,
                        SignalNo = 1,
                        Label = "B1",
                    },
                    new VwDirectWindowInput
                    {
                        X = 200,
                        Y = 0,
                        W = 100,
                        H = 100,
                        ZIndex = 1,
                        SignalNo = 1,
                        Label = "B2",
                    },
                    new VwDirectWindowInput
                    {
                        X = 400,
                        Y = 0,
                        W = 100,
                        H = 100,
                        ZIndex = 1,
                        SignalNo = 1,
                        Label = "B3",
                    },
                ],
                DryRun = dryRun,
                SkipGuardrails = skipGuardrails,
            }, ct).ConfigureAwait(true);
            await VwDeviceStepLogging.PublishSteps(_publisher, _activityPublisher, "ErrorSuite-CaseB", caseB.Steps, ct).ConfigureAwait(true);
            LogCase("B", "1 nguồn gán 3 cửa sổ", expectReject: false, caseB);

            if (DelayBetweenStepsMs > 0)
                await Task.Delay(Math.Clamp(DelayBetweenStepsMs, 0, 60000), ct).ConfigureAwait(true);

            // Case C: SceneId ngoài dải maxSceneNums thật (guardrail VwDirectSetupSceneOrchestrator.cs:83-87)
            var caseC = await orchestrator.Execute(new VwDirectPushSceneInput
            {
                SceneId = 99999,
                WallNo = effectiveWallNo,
                Windows =
                [
                    new VwDirectWindowInput
                    {
                        X = 0,
                        Y = 0,
                        W = 100,
                        H = 100,
                        ZIndex = 1,
                        SignalNo = 1,
                        Label = "C1",
                    },
                ],
                DryRun = dryRun,
                SkipGuardrails = skipGuardrails,
            }, ct).ConfigureAwait(true);
            await VwDeviceStepLogging.PublishSteps(_publisher, _activityPublisher, "ErrorSuite-CaseC", caseC.Steps, ct).ConfigureAwait(true);
            LogCase("C", "SceneId 99999 ngoài dải maxSceneNums", expectReject: true, caseC);

            StatusMessage = "✅ Đã chạy xong bộ kiểm thử lỗi (3/3 case, kết quả thật từ thiết bị). Xem chi tiết tại tab Log.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Lỗi khi chạy bộ kiểm thử: {ex.Message}";
            _activityPublisher.Publish("ErrorSuite", $"Lỗi: {ex.Message}", ActivityLevel.Error);
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void LogCase(string label, string desc, bool expectReject, VwDirectSetupResult result)
    {
        var rejected = !result.Success;
        var pass = rejected == expectReject;
        var tag = pass ? "PASS" : "FAIL";
        var level = pass ? ActivityLevel.Success : ActivityLevel.Warning;
        _activityPublisher.Publish("ErrorSuite",
            $"[{tag}] Case {label} ({desc}): thiết bị trả {(rejected ? "TỪ CHỐI" : "CHẤP NHẬN")} — {result.Message ?? "(không có message)"}",
            level);
    }

    private void InitializeBuiltInScenarios()
    {
        BuiltInScenarios.Clear();

        BuiltInScenarios.Add(new VwBuiltInScenario
        {
            Name = "1. Thiết lập scene (không chụp hình)",
            Description = "Tạo cấu hình scene hoàn chỉnh gồm màn hình, nguồn, cửa sổ và đẩy xuống thiết bị",
            ApiSteps =
            [
                "GET /ISAPI/DisplayDev/VideoWall/{wallNo}/windows",
                "POST /ISAPI/DisplayDev/VideoWall/{wallNo}/windows (×N)"
            ],
            ExecuteAction = async (ct) => await RunSetupSceneBuiltIn(captureSnapshot: false, ct).ConfigureAwait(true),
        });

        BuiltInScenarios.Add(new VwBuiltInScenario
        {
            Name = "2. Thiết lập scene (có chụp hình)",
            Description = "Tạo cấu hình scene, đẩy xuống thiết bị và lưu snapshot XML của các cửa sổ ra file",
            ApiSteps =
            [
                "GET /ISAPI/DisplayDev/VideoWall/{wallNo}/windows",
                "POST /ISAPI/DisplayDev/VideoWall/{wallNo}/windows (×N)",
                "GET /ISAPI/DisplayDev/VideoWall/{wallNo}/windows (Lưu snapshot XML)"
            ],
            ExecuteAction = async (ct) => await RunSetupSceneBuiltIn(captureSnapshot: true, ct).ConfigureAwait(true),
        });

        BuiltInScenarios.Add(new VwBuiltInScenario
        {
            Name = "3. Active scene",
            Description = "Kích hoạt scene trực tiếp theo Scene Code / SID",
            ApiSteps =
            [
                "PUT /ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{sid}/activate"
            ],
            ExecuteAction = async (ct) => await RunActiveSceneBuiltIn(ct).ConfigureAwait(true),
        });
    }

    private async Task RunSetupSceneBuiltIn(bool captureSnapshot, CancellationToken ct)
    {
        try
        {
            _activityPublisher.Publish("Scenario", $"Bắt đầu kịch bản: {(captureSnapshot ? "Thiết lập scene có chụp hình" : "Thiết lập scene không chụp hình")}...", ActivityLevel.Info);

            if (!BuiltInDryRun && _userConfirmation != null && !_userConfirmation.Confirm("Xác nhận đẩy thật", "Bạn có chắc chắn muốn đẩy cấu hình thật xuống thiết bị?"))
            {
                StatusMessage = "Đã huỷ thao tác đẩy thật.";
                return;
            }

            var orchestrator = _connection.BuildDirectSetupOrchestrator();
            if (orchestrator == null)
            {
                StatusMessage = "⚠️ Nhập IP thiết bị ở thanh kết nối phía trên trước khi chạy.";
                return;
            }

            var effectiveWallNo = BuiltInWallNo ?? _connection.WallNo;
            if (effectiveWallNo is null)
            {
                StatusMessage = "⚠️ Cần nhập WallNo trước khi chạy kịch bản (không tự chọn để tránh đẩy nhầm tường).";
                return;
            }

            var sceneName = string.IsNullOrWhiteSpace(BuiltInSceneName) ? "Scene_Tu_Dong" : BuiltInSceneName.Trim();
            var deviceKey = _connection.DeviceKey;

            var scenes = VwLocalSceneStore.ListScenes(deviceKey);
            var matchedScene = scenes.FirstOrDefault(s => s.Code == BuiltInSelectedSceneCode || s.Name == sceneName || s.ID == BuiltInSelectedSceneCode)
                ?? VwLocalSceneStore.GetActiveScene(deviceKey)
                ?? scenes.FirstOrDefault();

            var targetScene = matchedScene ?? new VwSceneDto
            {
                ID = "1",
                Name = sceneName,
                OutputId = effectiveWallNo.ToString(),
                GridCols = 2,
                GridRows = 1,
            };

            var windows = matchedScene != null
                ? VwLocalSceneStore.ListWindowScenes(deviceKey, matchedScene.ID!)
                : new List<VwWindowSceneDto>();

            if (windows.Count == 0)
            {
                windows =
                [
                    new VwWindowSceneDto
                    {
                        X = 0,
                        Y = 0,
                        W = VwDirectDeviceConstants.UniformTileSize,
                        H = VwDirectDeviceConstants.UniformTileSize,
                        ZIndex = 1,
                        SourceId = "1",
                    },
                    new VwWindowSceneDto
                    {
                        X = VwDirectDeviceConstants.UniformTileSize,
                        Y = 0,
                        W = VwDirectDeviceConstants.UniformTileSize,
                        H = VwDirectDeviceConstants.UniformTileSize,
                        ZIndex = 1,
                        SourceId = "2",
                    }
                ];
            }

            var sceneId = int.TryParse(targetScene.OutputId, out var sid) && sid > 0 ? sid : effectiveWallNo.Value;
            var input = new VwDirectPushSceneInput
            {
                SceneId = sceneId,
                WallNo = effectiveWallNo,
                DryRun = BuiltInDryRun,
                ResetWindows = true,
                Activate = true,
                Windows = windows.Select(w => new VwDirectWindowInput
                {
                    X = w.X ?? 0,
                    Y = w.Y ?? 0,
                    W = w.W ?? VwDirectDeviceConstants.UniformTileSize,
                    H = w.H ?? VwDirectDeviceConstants.UniformTileSize,
                    SignalNo = int.TryParse(w.SourceId, out var sig) ? sig : 1,
                }).ToList(),
            };

            var result = await orchestrator.Execute(input, ct).ConfigureAwait(true);
            await VwDeviceStepLogging.PublishSteps(_publisher, _activityPublisher, "BuiltInSetupScene", result.Steps, ct).ConfigureAwait(true);

            if (!result.Success)
            {
                StatusMessage = $"❌ Đẩy cấu hình scene {targetScene.Name} thất bại: {result.Message}";
                _activityPublisher.Publish("Scenario", StatusMessage, ActivityLevel.Error);
                return;
            }

            if (DelayBetweenStepsMs > 0)
                await Task.Delay(Math.Clamp(DelayBetweenStepsMs, 0, 60000), ct).ConfigureAwait(true);

            if (captureSnapshot)
            {
                var directClient = _connection.BuildDirectClient();
                if (directClient != null)
                {
                    var snapResult = await directClient.SendIsapi("GET", $"ISAPI/DisplayDev/VideoWall/{effectiveWallNo}/windows", null, null, ct).ConfigureAwait(true);
                    if (snapResult.Success && !string.IsNullOrWhiteSpace(snapResult.ResponseXml))
                    {
                        SaveSnapshotToFile(targetScene.Name ?? sceneName, snapResult.ResponseXml);
                    }
                    else if (!string.IsNullOrWhiteSpace(snapResult.Message))
                    {
                        _activityPublisher.Publish("Snapshot", $"Không lấy được snapshot XML: {snapResult.Message}", ActivityLevel.Warning);
                    }
                }
            }

            StatusMessage = $"✅ Đã hoàn thành kịch bản {targetScene.Name} (DryRun={BuiltInDryRun}).";
            _activityPublisher.Publish("Scenario", StatusMessage, ActivityLevel.Success);
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            StatusMessage = $"❌ Lỗi chạy kịch bản mẫu: {msg}";
            _activityPublisher.Publish("Scenario", $"Lỗi kịch bản mẫu: {msg}", ActivityLevel.Error);
        }
    }

    private async Task RunActiveSceneBuiltIn(CancellationToken ct)
    {
        try
        {
            var code = BuiltInSelectedSceneCode?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                StatusMessage = "⚠️ Vui lòng nhập Scene SID / Code cần kích hoạt.";
                return;
            }

            _activityPublisher.Publish("Scenario", $"Kích hoạt kịch bản theo SID: {code} (DryRun={BuiltInDryRun})...", ActivityLevel.Info);

            if (!BuiltInDryRun)
            {
                if (_userConfirmation != null && !_userConfirmation.Confirm("Xác nhận kích hoạt", $"Bạn có chắc muốn kích hoạt kịch bản SID {code} thật trên thiết bị?"))
                {
                    StatusMessage = "Đã huỷ kích hoạt.";
                    return;
                }

                var directClient = _connection.BuildDirectClient();
                if (directClient == null)
                {
                    StatusMessage = "⚠️ Nhập IP thiết bị ở thanh kết nối phía trên trước khi chạy.";
                    return;
                }

                var effectiveWallNo = BuiltInWallNo ?? _connection.WallNo;
                if (effectiveWallNo is null)
                {
                    StatusMessage = "⚠️ Cần nhập WallNo trước khi kích hoạt kịch bản (không tự chọn để tránh đẩy nhầm tường).";
                    return;
                }

                var sid = int.TryParse(code, out var parsedSid) ? parsedSid : 1;
                var result = await directClient.SendIsapi("PUT", $"ISAPI/DisplayDev/VideoWall/{effectiveWallNo}/scene/{sid}/activate", null, null, ct).ConfigureAwait(true);

                StatusMessage = result.Success
                    ? $"✅ Đã kích hoạt kịch bản SID {sid} trên thiết bị."
                    : $"❌ Kích hoạt kịch bản SID {sid} thất bại: {result.Message}";
                _activityPublisher.Publish("Scenario", StatusMessage, result.Success ? ActivityLevel.Success : ActivityLevel.Error);
            }
            else
            {
                StatusMessage = $"[DryRun] Mô phỏng kích hoạt kịch bản SID {code} thành công.";
                _activityPublisher.Publish("Scenario", $"[DryRun] Mô phỏng kích hoạt kịch bản SID {code}.", ActivityLevel.Success);
            }
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            StatusMessage = $"❌ Lỗi kích hoạt kịch bản: {msg}";
            _activityPublisher.Publish("Scenario", $"Lỗi kích hoạt kịch bản: {msg}", ActivityLevel.Error);
        }
    }

    private void SaveSnapshotToFile(string scenarioName, string xmlContent)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Module.VideoWall.WPF",
                "Logs");

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            var safeName = VwScenarioStore.SanitizeFileName(scenarioName);
            var fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}_snapshot.xml";
            var filePath = Path.Combine(logDir, fileName);

            File.WriteAllText(filePath, xmlContent);
            LastSnapshotSavedPath = filePath;

            _activityPublisher.Publish("Snapshot", $"Đã lưu snapshot XML tại: {filePath}", ActivityLevel.Success);
        }
        catch (Exception ex)
        {
            _activityPublisher.Publish("Snapshot", $"Lỗi khi lưu snapshot XML: {ex.Message}", ActivityLevel.Warning);
        }
    }

    private bool CanRunSelected() =>
        !IsRunning && Steps.Count > 0;

    private bool CanResumeScenario() =>
        !IsRunning && LastFailedStepIndex.HasValue && Steps.Count > 0;

    private bool CanDeleteScenario() =>
        !IsRunning && SelectedScenarioItem != null && !string.IsNullOrWhiteSpace(SelectedScenarioItem.SavedFileName);

    private bool CanRunOverlappingSizeTest() => !IsRunning && !string.IsNullOrWhiteSpace(_connection.AdHocIp);

    private bool CanRunErrorValidationSuite() => !IsRunning && !string.IsNullOrWhiteSpace(_connection.AdHocIp);

    private void OnStepsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RunSelectedScenarioCommand.NotifyCanExecuteChanged();
        ResumeScenarioCommand.NotifyCanExecuteChanged();
    }
}
