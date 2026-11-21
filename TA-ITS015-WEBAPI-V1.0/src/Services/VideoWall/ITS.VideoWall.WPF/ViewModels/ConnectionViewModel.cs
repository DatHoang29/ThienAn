using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Services.Shared.Events;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.ViewModels.Isapi;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Description: ViewModel quản lý kết nối trực tiếp thiết bị VideoWall, khảo sát Probe và gửi lệnh ISAPI
/// Created date: 13/09/2026
/// </summary>
public sealed partial class ConnectionViewModel(
    ActivityPublisher activityPublisher,
    IPublisher publisher) : ObservableObject
{
    public ConnectionViewModel(ActivityPublisher activityPublisher, IPublisher publisher, IUserConfirmation? userConfirmation)
        : this(activityPublisher, publisher)
    {
    }

    /// <summary>Mật khẩu xác thực kết nối thiết bị.</summary>
    [ObservableProperty]
    private string _adHocPassword = string.Empty;

    public const string RealDeviceIp = "";
    public const int RealDevicePort = 80;
    public const string RealDeviceAccount = "admin";
    public const string RealDevicePassword = "Tcp@2025";

    public const string LocalMockIp = "127.0.0.1";
    public const int LocalMockPort = 18080;
    public const string LocalMockAccount = "admin";
    public const string LocalMockPassword = "Password123!";

    /// <summary>Cờ chọn cấu hình kết nối thiết bị thật.</summary>
    [ObservableProperty]
    private bool _isRealDeviceSelected;

    partial void OnIsRealDeviceSelectedChanged(bool value)
    {
        if (value)
        {
            if (_isLocalMockSelected)
            {
                _isLocalMockSelected = false;
                OnPropertyChanged(nameof(IsLocalMockSelected));
            }
            ApplyRealDevicePreset();
        }
    }

    /// <summary>Cờ chọn cấu hình kết nối thiết bị Mock giả lập local.</summary>
    [ObservableProperty]
    private bool _isLocalMockSelected;

    partial void OnIsLocalMockSelectedChanged(bool value)
    {
        if (value)
        {
            if (_isRealDeviceSelected)
            {
                _isRealDeviceSelected = false;
                OnPropertyChanged(nameof(IsRealDeviceSelected));
            }
            ApplyLocalMockPreset();
        }
    }

    public void ApplyRealDevicePreset()
    {
        AdHocIp = RealDeviceIp;
        AdHocPort = RealDevicePort;
        AdHocAccount = RealDeviceAccount;
        AdHocPassword = RealDevicePassword;
    }

    public void ApplyLocalMockPreset()
    {
        AdHocIp = LocalMockIp;
        AdHocPort = LocalMockPort;
        AdHocAccount = LocalMockAccount;
        AdHocPassword = LocalMockPassword;
    }

    /// <summary>Cờ xác nhận chế độ làm việc trực tiếp với thiết bị.</summary>
    public bool IsDirectMode => true;

    /// <summary>Số thứ tự WallNo của tường màn hình.</summary>
    [ObservableProperty]
    private int? _wallNo;

    /// <summary>Địa chỉ IP kết nối thiết bị.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ProbeCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendIsapiCommand))]
    private string _adHocIp = "127.0.0.1";

    /// <summary>Cổng mạng kết nối thiết bị.</summary>
    [ObservableProperty]
    private int? _adHocPort = 18080;

    /// <summary>Tài khoản xác thực kết nối thiết bị.</summary>
    [ObservableProperty]
    private string _adHocAccount = string.Empty;

    /// <summary>Danh sách các mẫu API ISAPI có sẵn.</summary>
    public IReadOnlyList<VwIsapiPresetItem> IsapiPresets => VwIsapiPresetList.Presets;

    /// <summary>Mẫu API ISAPI đang được chọn.</summary>
    [ObservableProperty]
    private VwIsapiPresetItem? _selectedIsapiPreset;

    /// <summary>Form nhập tham số cho mẫu API ISAPI đang chọn.</summary>
    [ObservableProperty]
    private VwIsapiFormViewModel? _activeIsapiForm;

    private bool _isSyncingPath;

    partial void OnSelectedIsapiPresetChanged(VwIsapiPresetItem? value)
    {
        if (value is not null)
        {
            IsapiMethod = value.Method;
            ActiveIsapiForm = new VwIsapiFormViewModel(value);

            // Tự động điền WallNo hiện tại vào trường videoWallID nếu có
            if (WallNo.HasValue)
            {
                var wallField = ActiveIsapiForm.PathFields.FirstOrDefault(f =>
                    f.Definition.Key.Equals("videoWallID", StringComparison.OrdinalIgnoreCase));
                if (wallField != null)
                {
                    wallField.Value = WallNo.Value.ToString();
                }
            }

            ActiveIsapiForm.FormChanged += (_, _) => UpdateIsapiPathAndBody();
            UpdateIsapiPathAndBody();
        }
        else
        {
            ActiveIsapiForm = null;
        }
    }

    public void UpdateIsapiPathAndBody()
    {
        if (_isSyncingPath || ActiveIsapiForm == null)
            return;

        try
        {
            _isSyncingPath = true;
            IsapiPath = ActiveIsapiForm.BuildPath();
            IsapiBody = ActiveIsapiForm.RawBody;
        }
        finally
        {
            _isSyncingPath = false;
        }
    }

    /// <summary>Phương thức HTTP gửi yêu cầu ISAPI (GET, POST, PUT, DELETE).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBodyRequired))]
    private string _isapiMethod = "GET";

    /// <summary>Cờ kiểm tra phương thức HTTP có yêu cầu gửi Body XML hay không.</summary>
    public bool IsBodyRequired =>
        string.Equals(IsapiMethod, "POST", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(IsapiMethod, "PUT", StringComparison.OrdinalIgnoreCase);

    /// <summary>Đường dẫn URL gửi yêu cầu ISAPI tới thiết bị.</summary>
    [ObservableProperty]
    private string _isapiPath = "ISAPI/System/Board/status/capabilities";

    partial void OnIsapiPathChanged(string value)
    {
        SendIsapiCommand.NotifyCanExecuteChanged();

        if (_isSyncingPath || ActiveIsapiForm == null)
            return;

        try
        {
            _isSyncingPath = true;
            ActiveIsapiForm.TryExtractPathParameters(value);
        }
        finally
        {
            _isSyncingPath = false;
        }
    }

    /// <summary>Nội dung Body XML gửi kèm yêu cầu ISAPI.</summary>
    [ObservableProperty]
    private string? _isapiBody;

    /// <summary>Nội dung XML phản hồi trả về từ thiết bị.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIsapiResponse))]
    [NotifyPropertyChangedFor(nameof(IsapiResponseStatusBadge))]
    [NotifyPropertyChangedFor(nameof(IsapiResponseSuccess))]
    [NotifyPropertyChangedFor(nameof(IsapiResponseBadges))]
    private string? _isapiResponse;

    /// <summary>Đối tượng tóm tắt phân tích phản hồi ISAPI.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsapiResponseStatusBadge))]
    [NotifyPropertyChangedFor(nameof(IsapiResponseSuccess))]
    [NotifyPropertyChangedFor(nameof(IsapiResponseBadges))]
    private VwIsapiResponseSummary _isapiResponseSummary = new();

    /// <summary>Cờ kiểm tra có nội dung phản hồi ISAPI hay không.</summary>
    public bool HasIsapiResponse => !string.IsNullOrWhiteSpace(IsapiResponse);

    /// <summary>Huy hiệu trạng thái kết quả phản hồi ISAPI (Success, Error...).</summary>
    public string IsapiResponseStatusBadge => IsapiResponseSummary.StatusBadge;

    /// <summary>Cờ kiểm tra kết quả phản hồi ISAPI có thành công hay không.</summary>
    public bool IsapiResponseSuccess => IsapiResponseSummary.IsSuccess;

    /// <summary>Danh sách các huy hiệu thông tin bóc tách từ phản hồi ISAPI.</summary>
    public IReadOnlyList<string> IsapiResponseBadges => IsapiResponseSummary.Badges;

    partial void OnIsapiResponseChanged(string? value)
    {
        IsapiResponseSummary = VwIsapiResponseSummary.Parse(value);
    }

    [RelayCommand]
    private void CopyIsapiResponse()
    {
        if (!string.IsNullOrEmpty(IsapiResponse))
        {
            try
            {
                Clipboard.SetText(IsapiResponse);
                activityPublisher.Publish("ISAPI", "Đã sao chép phản hồi XML vào Clipboard.", ActivityLevel.Info);
            }
            catch
            {
            }
        }
    }

    /// <summary>Thông báo trạng thái kết nối hoặc tiến trình trên giao diện.</summary>
    [ObservableProperty]
    private string _statusMessage = "";

    /// <summary>Cờ trạng thái kết nối thành công tới thiết bị.</summary>
    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProbeWalls))]
    [NotifyPropertyChangedFor(nameof(ProbeSummary))]
    [NotifyPropertyChangedFor(nameof(ProbeTotalWidth))]
    [NotifyPropertyChangedFor(nameof(ProbeTotalHeight))]
    [NotifyPropertyChangedFor(nameof(ProbeColumnCount))]
    [NotifyPropertyChangedFor(nameof(ProbeRowCount))]
    [NotifyPropertyChangedFor(nameof(ProbeOutputCount))]
    [NotifyPropertyChangedFor(nameof(ProbeInputCount))]
    [NotifyPropertyChangedFor(nameof(ProbeMaxWindowNums))]
    [NotifyPropertyChangedFor(nameof(ProbeMaxSceneNums))]
    [NotifyPropertyChangedFor(nameof(ProbeBaseOutputSize))]
    [NotifyPropertyChangedFor(nameof(ProbeIsSupportScene))]
    [NotifyPropertyChangedFor(nameof(ProbeWallDimensionsFormatted))]
    [NotifyPropertyChangedFor(nameof(ProbeCapabilitiesFormatted))]
    [NotifyPropertyChangedFor(nameof(ProbeOutputsList))]
    [NotifyPropertyChangedFor(nameof(ProbeInputsList))]
    [NotifyPropertyChangedFor(nameof(ProbeTotalDimensionText))]
    [NotifyPropertyChangedFor(nameof(ProbeOutputCountText))]
    [NotifyPropertyChangedFor(nameof(ProbeInputCountText))]
    [NotifyPropertyChangedFor(nameof(ProbeMaxWindowNumsText))]
    [NotifyPropertyChangedFor(nameof(ProbeMaxSceneNumsText))]
    [NotifyPropertyChangedFor(nameof(ProbeOutputResolutionSummary))]
    /// <summary>Kết quả khảo sát năng lực thiết bị (VwProbeDeviceOutput).</summary>
    private VwProbeDeviceOutput? _probeResult;

    /// <summary>Danh sách tóm tắt các tường màn hình phát hiện được.</summary>
    public ObservableCollection<VwWallSummary> Walls { get; } = new();

    /// <summary>Cờ kiểm tra có tường màn hình nào trong kết quả khảo sát hay không.</summary>
    public bool HasProbeWalls => Walls.Count > 0 || (ProbeResult?.Walls != null && ProbeResult.Walls.Count > 0);

    /// <summary>Cờ kiểm tra đã có kết quả khảo sát thành công từ thiết bị hay chưa.</summary>
    public bool HasProbeResult => ProbeResult != null && ProbeResult.Reachable;

    /// <summary>Tổng chiều rộng của tường màn hình đo được từ thiết bị (pixel).</summary>
    public int ProbeTotalWidth =>
        ProbeResult?.Outputs != null && ProbeResult.Outputs.Count > 0
            ? ProbeResult.Outputs.Max(o => o.X + o.Width)
            : 0;

    /// <summary>Tổng chiều cao của tường màn hình đo được từ thiết bị (pixel).</summary>
    public int ProbeTotalHeight =>
        ProbeResult?.Outputs != null && ProbeResult.Outputs.Count > 0
            ? ProbeResult.Outputs.Max(o => o.Y + o.Height)
            : 0;

    /// <summary>Số cột màn hình vật lý ghép thành tường.</summary>
    public int ProbeColumnCount =>
        ProbeResult?.Outputs != null && ProbeResult.Outputs.Count > 0
            ? Math.Max(1, ProbeResult.Outputs.Select(o => o.X).Distinct().Count())
            : 0;

    /// <summary>Số hàng màn hình vật lý ghép thành tường.</summary>
    public int ProbeRowCount =>
        ProbeResult?.Outputs != null && ProbeResult.Outputs.Count > 0
            ? Math.Max(1, ProbeResult.Outputs.Select(o => o.Y).Distinct().Count())
            : 0;

    /// <summary>Tổng số cổng ra màn hình phát hiện được.</summary>
    public int ProbeOutputCount => ProbeResult?.Outputs?.Count ?? 0;

    /// <summary>Tổng số kênh đầu vào phát hiện được.</summary>
    public int ProbeInputCount => ProbeResult?.InputChannels?.Count ?? 0;

    /// <summary>Số lượng cửa sổ tối đa thiết bị hỗ trợ mở đồng thời.</summary>
    public int ProbeMaxWindowNums => ProbeResult?.MaxWindowNums ?? 0;

    /// <summary>Số lượng kịch bản tối đa thiết bị cho phép lưu trữ.</summary>
    public int ProbeMaxSceneNums => ProbeResult?.MaxSceneNums ?? 0;

    /// <summary>Kích thước độ phân giải cơ sở của cổng ra màn hình (pixel).</summary>
    public int ProbeBaseOutputSize => ProbeResult?.BaseOutputSize ?? 1920;

    /// <summary>Cờ cho biết thiết bị có hỗ trợ tính năng kịch bản hay không.</summary>
    public bool ProbeIsSupportScene => ProbeResult?.IsSupportScene ?? true;

    /// <summary>Chuỗi định dạng kích thước tổng của tường màn hình.</summary>
    public string ProbeTotalDimensionText =>
        HasProbeResult && ProbeTotalWidth > 0 && ProbeTotalHeight > 0
            ? $"{ProbeTotalWidth} × {ProbeTotalHeight} px"
            : "-- × --";

    /// <summary>Chuỗi định dạng số lượng cổng ra màn hình.</summary>
    public string ProbeOutputCountText =>
        HasProbeResult
            ? $"{ProbeOutputCount} Cổng (Màn hình)"
            : "-- Cổng";

    /// <summary>Chuỗi định dạng số lượng kênh đầu vào.</summary>
    public string ProbeInputCountText =>
        HasProbeResult
            ? $"{ProbeInputCount} Kênh Camera / HDMI"
            : "-- Kênh";

    /// <summary>Chuỗi định dạng số cửa sổ tối đa.</summary>
    public string ProbeMaxWindowNumsText =>
        HasProbeResult
            ? $"{ProbeMaxWindowNums}"
            : "--";

    /// <summary>Chuỗi định dạng số kịch bản tối đa.</summary>
    public string ProbeMaxSceneNumsText =>
        HasProbeResult
            ? $"{ProbeMaxSceneNums}"
            : "--";

    /// <summary>Chuỗi tóm tắt độ phân giải các cổng ra màn hình.</summary>
    public string ProbeOutputResolutionSummary
    {
        get
        {
            if (!HasProbeResult || ProbeResult?.Outputs == null || ProbeResult.Outputs.Count == 0)
                return "(Chưa khảo sát)";

            var distinctResolutions = ProbeResult.Outputs
                .Select(o => $"{o.Width} × {o.Height} px")
                .Distinct()
                .ToList();

            return distinctResolutions.Count switch
            {
                1 => $"{distinctResolutions[0]} / cổng ra",
                > 1 => $"Đa độ phân giải ({string.Join(", ", distinctResolutions)})",
                _ => "(Chưa khảo sát)"
            };
        }
    }

    /// <summary>Chuỗi định dạng ma trận và số màn hình của tường ghép.</summary>
    public string ProbeWallDimensionsFormatted =>
        HasProbeResult
            ? (ProbeColumnCount > 0 && ProbeRowCount > 0
                ? $"Lưới {ProbeColumnCount} × {ProbeRowCount} — {ProbeOutputCount} màn hình"
                : $"{ProbeOutputCount} màn hình")
            : "(Chưa khảo sát)";

    /// <summary>Chuỗi tóm tắt thông số cơ bản dạng badge sau khảo sát.</summary>
    public string ProbeBadgeSummary =>
        HasProbeResult
            ? $"Kích thước: {ProbeTotalWidth} × {ProbeTotalHeight} px ({ProbeOutputCount} Màn hình) | Max: {ProbeMaxWindowNums} windows"
            : "(Chưa kết nối & khảo sát thiết bị)";

    /// <summary>Chuỗi tóm tắt năng lực tối đa của thiết bị.</summary>
    public string ProbeCapabilitiesFormatted =>
        HasProbeResult
            ? $"Tối đa {ProbeMaxWindowNums} cửa sổ · {ProbeMaxSceneNums} kịch bản"
            : "(Chưa khảo sát)";

    /// <summary>Danh sách chi tiết các cổng ra màn hình từ kết quả khảo sát.</summary>
    public IReadOnlyList<VwISAPIOutputItem> ProbeOutputsList =>
        ProbeResult?.Outputs ?? [];

    /// <summary>Danh sách chi tiết các kênh đầu vào từ kết quả khảo sát.</summary>
    public IReadOnlyList<VwISAPIInputChannel> ProbeInputsList =>
        ProbeResult?.InputChannels ?? [];

    /// <summary>Cờ trạng thái đang bận thực thi lệnh kết nối, khảo sát hoặc gửi ISAPI.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ProbeCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendIsapiCommand))]
    private bool _isBusy;

    /// <summary>Chuỗi tóm tắt toàn diện cấu hình phần cứng thiết bị sau khi khảo sát.</summary>
    public string ProbeSummary =>
        ProbeResult is null
            ? string.Empty
            : $"{ProbeResult.ControllerName} ({ProbeResult.Ip}) · "
                + $"{(ProbeResult.Reachable ? "kết nối được" : "KHÔNG kết nối được")} · "
                + $"model {ProbeResult.AssumedModel} · "
                + $"WallNo {ProbeResult.WallNo} · "
                + $"max {ProbeResult.MaxWindowNums} windows / {ProbeResult.MaxSceneNums} scenes";

    [RelayCommand(CanExecute = nameof(CanConnectOrProbe))]
    private async Task Connect(CancellationToken cancellationToken)
    {
        var directClient = BuildDirectClient();
        if (directClient is null)
            return;

        await Run("Đang kết nối & khảo sát thiết bị...", async () =>
        {
            var result = await directClient.Probe(WallNo, cancellationToken).ConfigureAwait(true);

            ProbeResult = result;
            IsConnected = result.Reachable;

            if (result.Walls != null && result.Walls.Count > 0 && !WallNo.HasValue)
            {
                WallNo = result.BoundWallNo ?? result.WallNo ?? (result.Walls.Count == 1 ? result.Walls[0].Id : null);
            }

            OnPropertyChanged(nameof(ProbeSummary));
            OnPropertyChanged(nameof(HasProbeWalls));
            StatusMessage = result.Reachable
                ? (WallNo.HasValue
                    ? $"Kết nối & khảo sát thành công tới {AdHocIp}:{AdHocPort}: Tường #{WallNo}, {result.Outputs?.Count ?? 0} cổng màn hình, max {result.MaxWindowNums} cửa sổ."
                    : $"Kết nối & khảo sát thành công tới {AdHocIp}:{AdHocPort}: tìm thấy {result.Walls?.Count ?? 0} tường. Hãy chọn WallNo.")
                : $"Kết nối thất bại: Không kết nối được tới thiết bị ({result.Steps?.LastOrDefault()?.Message})";
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanConnectOrProbe))]
    private async Task Probe(CancellationToken cancellationToken)
    {
        var directClient = BuildDirectClient();
        if (directClient is null)
            return;

        await Run("Đang khảo sát lại thiết bị...", async () =>
        {
            var result = await directClient.Probe(WallNo, cancellationToken).ConfigureAwait(true);

            ProbeResult = result;
            IsConnected = result.Reachable;

            if (result.Walls != null && result.Walls.Count > 0 && !WallNo.HasValue)
            {
                WallNo = result.BoundWallNo ?? result.WallNo ?? (result.Walls.Count == 1 ? result.Walls[0].Id : null);
            }

            OnPropertyChanged(nameof(ProbeSummary));
            OnPropertyChanged(nameof(HasProbeWalls));
            StatusMessage = result.Reachable
                ? (WallNo.HasValue
                    ? $"Khảo sát lại xong: Tường #{WallNo}, {result.Outputs?.Count ?? 0} cổng màn hình, max {result.MaxWindowNums} cửa sổ."
                    : $"Khảo sát lại xong: tìm thấy {result.Walls?.Count ?? 0} tường. Hãy chọn WallNo.")
                : $"Khảo sát thất bại: Không kết nối được tới thiết bị ({result.Steps?.LastOrDefault()?.Message})";
        }).ConfigureAwait(true);
    }

    partial void OnProbeResultChanged(VwProbeDeviceOutput? value)
    {
        Walls.Clear();
        if (value?.Walls != null)
        {
            foreach (var wall in value.Walls)
            {
                Walls.Add(wall);
            }
        }

        if (value?.Walls != null && value.Walls.Count > 0 && !WallNo.HasValue)
        {
            WallNo = value.BoundWallNo ?? value.WallNo ?? (value.Walls.Count == 1 ? value.Walls[0].Id : null);
        }

        NotifyProbeMetrics();
    }

    partial void OnWallNoChanged(int? value)
    {
        if (value.HasValue && ActiveIsapiForm != null)
        {
            var wallField = ActiveIsapiForm.PathFields.FirstOrDefault(f =>
                f.Definition.Key.Equals("videoWallID", StringComparison.OrdinalIgnoreCase));
            if (wallField != null && !string.Equals(wallField.Value, value.Value.ToString(), StringComparison.Ordinal))
            {
                wallField.Value = value.Value.ToString();
            }
        }

        if (value.HasValue && ProbeResult != null && ProbeResult.Reachable && ProbeResult.WallNo != value.Value)
        {
            var targetWallNo = value.Value;
            _ = Task.Run(async () =>
            {
                try
                {
                    var isapiClient = BuildDirectISAPIClient();
                    if (isapiClient is null)
                        return;

                    var outputsRes = await isapiClient.GetOutputs(targetWallNo, CancellationToken.None).ConfigureAwait(false);
                    if (outputsRes.Success && outputsRes.Data?.WallOutput != null)
                    {
                        var newOutputs = outputsRes.Data.WallOutput.Select(o => new VwISAPIOutputItem
                        {
                            Id = o.Id,
                            OutputId = o.OutputId,
                            OutputWinNum = o.OutputWinNum,
                            CoordinateMode = o.CoordinateMode,
                            Rect = o.Rect == null ? null : new VwISAPIRect
                            {
                                Width = o.Rect.Width,
                                Height = o.Rect.Height,
                                Coordinate = o.Rect.Coordinate == null ? null : new VwISAPICoordinate
                                {
                                    X = o.Rect.Coordinate.X,
                                    Y = o.Rect.Coordinate.Y,
                                }
                            }
                        }).ToList();

                        void ApplyOutputs()
                        {
                            if (ProbeResult != null && WallNo == targetWallNo)
                            {
                                ProbeResult.WallNo = targetWallNo;
                                ProbeResult.Outputs = newOutputs;
                                NotifyProbeMetrics();
                            }
                        }

                        if (Application.Current?.Dispatcher != null)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(ApplyOutputs);
                        }
                        else
                        {
                            ApplyOutputs();
                        }
                    }
                }
                catch
                {
                    // Ignore background reload failure
                }
            });
        }
    }

    private void NotifyProbeMetrics()
    {
        OnPropertyChanged(nameof(HasProbeResult));
        OnPropertyChanged(nameof(HasProbeWalls));
        OnPropertyChanged(nameof(ProbeBadgeSummary));
        OnPropertyChanged(nameof(ProbeTotalWidth));
        OnPropertyChanged(nameof(ProbeTotalHeight));
        OnPropertyChanged(nameof(ProbeColumnCount));
        OnPropertyChanged(nameof(ProbeRowCount));
        OnPropertyChanged(nameof(ProbeOutputCount));
        OnPropertyChanged(nameof(ProbeInputCount));
        OnPropertyChanged(nameof(ProbeMaxWindowNums));
        OnPropertyChanged(nameof(ProbeMaxSceneNums));
        OnPropertyChanged(nameof(ProbeTotalDimensionText));
        OnPropertyChanged(nameof(ProbeOutputCountText));
        OnPropertyChanged(nameof(ProbeInputCountText));
        OnPropertyChanged(nameof(ProbeMaxWindowNumsText));
        OnPropertyChanged(nameof(ProbeMaxSceneNumsText));
        OnPropertyChanged(nameof(ProbeOutputResolutionSummary));
        OnPropertyChanged(nameof(ProbeWallDimensionsFormatted));
        OnPropertyChanged(nameof(ProbeCapabilitiesFormatted));
        OnPropertyChanged(nameof(ProbeOutputsList));
        OnPropertyChanged(nameof(ProbeInputsList));
        OnPropertyChanged(nameof(ProbeSummary));
        OnPropertyChanged(nameof(ProbeResult));
    }

    private string? ResolveContentType() =>
        string.IsNullOrWhiteSpace(IsapiBody)
            ? null
            : ActiveIsapiForm?.Preset.Section is "9.7.2.10" or "9.7.2.13" ? "application/json" : "application/xml";

    [RelayCommand(CanExecute = nameof(CanSendIsapi))]
    private async Task SendIsapi(CancellationToken cancellationToken)
    {
        if (ActiveIsapiForm != null)
        {
            if (!ActiveIsapiForm.Validate(out var validationError))
            {
                StatusMessage = $"⚠️ {validationError}";
                activityPublisher.Publish("Connection", $"Chưa đủ tham số bắt buộc: {validationError}", ActivityLevel.Warning);
                return;
            }

            UpdateIsapiPathAndBody();
        }

        var directClient = BuildDirectClient();
        if (directClient is null)
        {
            StatusMessage = "⚠️ Chưa cấu hình kết nối thiết bị.";
            return;
        }

        var method = string.IsNullOrWhiteSpace(IsapiMethod) ? "GET" : IsapiMethod.Trim().ToUpperInvariant();
        var path = IsapiPath.Trim().TrimStart('/');

        if (System.Text.RegularExpressions.Regex.IsMatch(path, @"\{[a-zA-Z0-9_]+\}"))
        {
            StatusMessage = $"⚠️ Đường dẫn còn chứa tham số chưa nhập: {path}";
            activityPublisher.Publish("Connection", $"Đường dẫn còn chứa tham số: {path}", ActivityLevel.Warning);
            return;
        }

        var previousStatus = StatusMessage;
        IsBusy = true;
        try
        {
            var step = await directClient.SendIsapi(
                method, path,
                string.IsNullOrWhiteSpace(IsapiBody) ? null : IsapiBody,
                ResolveContentType(),
                cancellationToken).ConfigureAwait(true);

            IsapiResponse = step.ResponseXml;
            if (!step.Success)
            {
                StatusMessage = $"ISAPI thất bại: {step.Message}";
                activityPublisher.Publish("ISAPI", $"ISAPI {step.Method} {step.Endpoint} thất bại: {step.Message}", ActivityLevel.Error);
            }
            else
            {
                StatusMessage = previousStatus;
                activityPublisher.Publish("ISAPI", $"ISAPI {step.Method} {step.Endpoint} → OK ({step.DurationMs} ms)", ActivityLevel.Success);
            }
        }
        catch (Exception exception)
        {
            var msg = exception.InnerException?.Message ?? exception.Message;
            StatusMessage = $"Lỗi kết nối / thực thi: {msg}";
            activityPublisher.Publish("Connection", $"Lỗi: {msg}", ActivityLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanConnectOrProbe() => !IsBusy && !string.IsNullOrWhiteSpace(AdHocIp);

    private bool CanSendIsapi()
    {
        if (IsBusy)
            return false;

        if (string.IsNullOrWhiteSpace(IsapiPath))
            return false;

        return !string.IsNullOrWhiteSpace(AdHocIp);
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
            var msg = exception.InnerException?.Message ?? exception.Message;
            StatusMessage = $"Lỗi kết nối / thực thi: {msg}";
            activityPublisher.Publish("Connection", $"Lỗi: {msg}", ActivityLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnAdHocIpChanged(string value)
    {
        SendIsapiCommand.NotifyCanExecuteChanged();
        ConnectCommand.NotifyCanExecuteChanged();
        ProbeCommand.NotifyCanExecuteChanged();
    }


    internal VwDirectDeviceConnectionClient? BuildDirectClient()
    {
        if (string.IsNullOrWhiteSpace(AdHocIp))
        {
            StatusMessage = "Nhập IP thiết bị trước khi dùng chế độ trực tiếp.";
            return null;
        }

        var ip = !string.IsNullOrWhiteSpace(AdHocIp) ? AdHocIp.Trim() : "127.0.0.1";
        var credentials = new VwDirectDeviceCredentials(
            ip,
            AdHocPort ?? 18080,
            !string.IsNullOrWhiteSpace(AdHocAccount) ? AdHocAccount.Trim() : "admin",
            AdHocPassword);

        return VwDirectClientFactory.CreateConnectionClient(
            credentials,
            publisher);
    }

    internal VwDirectSetupSceneOrchestrator? BuildDirectSetupOrchestrator()
    {
        if (string.IsNullOrWhiteSpace(AdHocIp))
        {
            StatusMessage = "Nhập IP thiết bị trước khi dùng chế độ trực tiếp.";
            return null;
        }

        var ip = !string.IsNullOrWhiteSpace(AdHocIp) ? AdHocIp.Trim() : "127.0.0.1";
        var credentials = new VwDirectDeviceCredentials(
            ip,
            AdHocPort ?? 18080,
            !string.IsNullOrWhiteSpace(AdHocAccount) ? AdHocAccount.Trim() : "admin",
            AdHocPassword);

        return VwDirectClientFactory.CreateSetupSceneOrchestrator(credentials);
    }

    public VwDirectISAPIClient? BuildDirectISAPIClient()
    {
        if (string.IsNullOrWhiteSpace(AdHocIp))
        {
            StatusMessage = "Nhập IP thiết bị trước khi dùng chế độ trực tiếp.";
            return null;
        }

        var ip = !string.IsNullOrWhiteSpace(AdHocIp) ? AdHocIp.Trim() : "127.0.0.1";
        var credentials = new VwDirectDeviceCredentials(
            ip,
            AdHocPort ?? 18080,
            !string.IsNullOrWhiteSpace(AdHocAccount) ? AdHocAccount.Trim() : "admin",
            AdHocPassword);

        return VwDirectClientFactory.CreateISAPIClient(credentials);
    }

    /// <summary>Khóa định danh thiết bị dựa trên IP và WallNo.</summary>
    public string DeviceKey => GetCurrentDeviceKey() ?? "default";

    internal string? GetCurrentDeviceKey()
    {
        if (string.IsNullOrWhiteSpace(AdHocIp))
            return null;

        var ip = AdHocIp.Trim();
        return WallNo.HasValue ? $"{ip}_wall_{WallNo.Value}" : ip;
    }
}
