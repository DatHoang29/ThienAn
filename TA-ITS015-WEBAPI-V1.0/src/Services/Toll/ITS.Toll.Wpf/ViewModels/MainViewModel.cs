using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITS.Toll.Core.Configuration;
using ITS.Toll.Core.Logging;
using ITS.Toll.Core.Models;
using ITS.Toll.Core.Persistence;
using ITS.Toll.Core.Services;

namespace ITS.Toll.Wpf.ViewModels;

/// <summary>
/// ViewModel màn hình test API thu phí. Người dùng nhập/sửa danh sách API trực tiếp trên form
/// Author: ChuongLP
/// Created date: 24/08/2026
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly TollApiClient _apiClient;
    private readonly TollDataStore _dataStore;
    private readonly TollLogger _logger;
    private readonly TollSyncCursorStore _cursorStore;
    private readonly TollServiceOptions _serviceOptions;
    private readonly DispatcherTimer _autoTimer;

    public MainViewModel(TollApiClient apiClient, TollDataStore dataStore, TollOptions options, TollLogger logger,
        TollSyncCursorStore cursorStore, TollServiceOptions serviceOptions)
    {
        _apiClient = apiClient;
        _dataStore = dataStore;
        _logger = logger;
        _cursorStore = cursorStore;
        _serviceOptions = serviceOptions;

        // Form chỉ hiển thị: mỗi dòng log Core phát ra sẽ được đưa lên danh sách Logs để vẽ view.
        _logger.EntryLogged += OnLogEntry;

        BaseUrl = options.BaseUrl ?? string.Empty;
        TimeoutSeconds = options.TimeoutSeconds;
        DateFormat = options.DateFormat;
        AutoCallIntervalSeconds = options.AutoCallIntervalSeconds > 0 ? options.AutoCallIntervalSeconds : 60;

        foreach (var ep in options.Endpoints)
            Endpoints.Add(new EndpointItemViewModel(ep));

        SelectedEndpoint = Endpoints.FirstOrDefault();

        // Mặc định: khoảng thời gian là HÔM QUA (dịch vụ chỉ lấy đến hết ngày hôm qua, không lấy hôm nay).
        // Với endpoint có khoảng ngày, khoảng thực tế khi gọi được tính lại theo watermark (xem CallEndpointAsync).
        var now = DateTime.Now;
        FromDate = now.Date.AddDays(-1);
        ToDate = now.Date.AddSeconds(-1);

        // Timer tự động gọi (khởi tạo, chỉ chạy khi bấm "Tự động").
        _autoTimer = new DispatcherTimer();
        _autoTimer.Tick += OnAutoTimerTick;

        Log(LogCategory.Config, "Info", $"Sẵn sàng. Đã nạp {Endpoints.Count} API từ cấu hình hoạt động (SysOpConfig, nhóm '{TollOpConfigReader.GroupCode}').");
    }

    // ================== CẤU HÌNH ĐỘNG ==================
    public ObservableCollection<EndpointItemViewModel> Endpoints { get; } = new();

    [ObservableProperty] private EndpointItemViewModel? _selectedEndpoint;

    [ObservableProperty] private string _baseUrl = string.Empty;
    [ObservableProperty] private int _timeoutSeconds = 30;
    [ObservableProperty] private string _dateFormat = "yyyy-MM-dd HH:mm:ss";

    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;

    [ObservableProperty] private int _autoCallIntervalSeconds = 60;

    [ObservableProperty] private bool _isAutoRunning;
    [ObservableProperty] private string _autoStatus = "Tự động: TẮT";

    // ================== TRẠNG THÁI / KẾT QUẢ ==================
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CallSelectedApiCommand))]
    [NotifyCanExecuteChangedFor(nameof(CallAllApisCommand))]
    private bool _isBusy;

    [ObservableProperty] private string _responseText = string.Empty;
    [ObservableProperty] private string _statusText = "Chưa gọi";
    [ObservableProperty] private string _requestUrlText = string.Empty;

    /// <summary>Bật = tự động lưu dữ liệu xuống DB sau mỗi lần gọi thành công.</summary>
    [ObservableProperty] private bool _saveToDb;

    // ================== NHẬT KÝ ==================
    public ObservableCollection<LogItem> Logs { get; } = new();

    // ================== LỆNH ==================

    /// <summary>Gọi API đang chọn trên lưới.</summary>
    [RelayCommand(CanExecute = nameof(CanCall))]
    private async Task CallSelectedApiAsync()
    {
        if (SelectedEndpoint is null)
        {
            Log(LogCategory.Sync, "Warning", "Chưa chọn API nào trên lưới.");
            return;
        }

        // Tôn trọng checkbox "Bật": API đang tắt thì KHÔNG gọi (nhất quán với "Gọi tất cả").
        if (!SelectedEndpoint.Enabled)
        {
            Log(LogCategory.Sync, "Warning", $"API '{SelectedEndpoint.Name}' đang TẮT (bỏ chọn 'Bật') — không gọi. Tích 'Bật' rồi thử lại.");
            return;
        }

        // manual = true: tôn trọng đúng FromDate/ToDate người dùng nhập trên form.
        await CallEndpointAsync(SelectedEndpoint, manual: true);
    }

    /// <summary>Gọi tuần tự tất cả API đang bật (Enabled) — bấm nút "Gọi tất cả" (thủ công).</summary>
    [RelayCommand(CanExecute = nameof(CanCall))]
    private Task CallAllApisAsync() => CallAllApisCoreAsync(manual: true);

    /// <summary>
    /// Lõi gọi tất cả API đang bật. <paramref name="manual"/> = true khi người dùng bấm nút
    /// (dùng khoảng ngày trên form); = false khi timer tự động gọi (dùng cửa sổ theo watermark).
    /// </summary>
    private async Task CallAllApisCoreAsync(bool manual)
    {
        var enabled = Endpoints.Where(e => e.Enabled).ToList();
        if (enabled.Count == 0)
        {
            Log(LogCategory.Sync, "Warning", "Không có API nào đang bật.");
            return;
        }

        Log(LogCategory.Sync, "Info", $"===== Bắt đầu lượt đồng bộ: {enabled.Count} API =====");
        var ok = 0;
        foreach (var ep in enabled)
        {
            if (await CallEndpointAsync(ep, manual)) ok++;
        }
        Log(LogCategory.Sync, "Info", $"===== Kết thúc lượt đồng bộ: {ok}/{enabled.Count} API thành công =====");
    }

    private bool CanCall() => !IsBusy;

    /// <summary>
    /// Gọi 1 endpoint (Core tự ghi log luồng gọi/nhận/lưu DB); form chỉ CẬP NHẬT HIỂN THỊ
    /// (panel phản hồi + dòng lưới). Trả về true nếu gọi thành công.
    /// </summary>
    private async Task<bool> CallEndpointAsync(EndpointItemViewModel item, bool manual)
    {
        IsBusy = true;
        var requestId = Guid.NewGuid().ToString("N");
        var now = DateTime.Now;
        try
        {
            var options = BuildOptions();
            var endpoint = item.ToEndpoint();

            // Xác định khoảng thời gian gửi lên API:
            //   - Thủ công (bấm nút): dùng ĐÚNG FromDate/ToDate người dùng nhập trên form, KHÔNG đụng watermark
            //     (tránh làm lệch con trỏ đồng bộ tự động).
            //   - Tự động (timer) & endpoint CẦN khoảng ngày — Hướng B, cửa sổ cuộn theo watermark:
            //     To = now; From = (watermark ?? now - InitialLookbackHours) - Overlap. Watermark CHỈ nhích khi
            //     LƯU DB thành công, nên service lỗi/tắt giữa chừng sẽ lặp lại đúng khoảng → không miss.
            //   - Endpoint KHÔNG cần ngày (master/danh mục): gọi full, không dùng khoảng ngày/watermark.
            TollSyncCursor? cursor = null;
            DateTime? from = null;
            DateTime? to = null;
            if (endpoint.RequiresDateRange)
            {
                if (manual)
                {
                    // Dùng đúng khoảng thời gian trên form. cursor để null → không nhích watermark.
                    from = FromDate;
                    to = ToDate;
                    if (from is null || to is null)
                        Log(LogCategory.Sync, "Warning", $"API '{item.Name}' cần khoảng ngày nhưng FromDate/ToDate đang trống — sẽ gọi không kèm khoảng ngày.");
                }
                else
                {
                    cursor = _cursorStore.Load(endpoint.DataType);
                    to = now;
                    from = _cursorStore.ComputeFrom(cursor, now);
                    // Hiển thị cửa sổ thực tế lên form cho người dùng theo dõi.
                    FromDate = from;
                    ToDate = to;
                }
            }

            var result = await _apiClient.CallAsync(endpoint, options, from, to);

            // Cập nhật vùng hiển thị phản hồi.
            RequestUrlText = result.RequestUrl;
            ResponseText = string.IsNullOrWhiteSpace(result.PrettyContent) ? result.RawContent : result.PrettyContent;
            StatusText = result.Success
                ? $"OK · HTTP {result.StatusCode} · {result.DurationMs} ms · {result.RecordCount?.ToString() ?? "?"} bản ghi"
                : $"LỖI · HTTP {result.StatusCode} · {result.DurationMs} ms · {result.Error}";

            // Cập nhật dòng trên lưới.
            item.LastStatus = result.Success ? "OK" : "Lỗi";
            item.LastDurationMs = result.DurationMs;
            item.LastRecordCount = result.RecordCount?.ToString() ?? "-";
            item.LastCalledAt = result.CalledAt.ToString("HH:mm:ss");

            // Tùy chọn: lưu dữ liệu xuống DB sau khi gọi thành công (Core tự ghi log kết quả lưu).
            TollSaveResult? save = null;
            if (SaveToDb && result.Success)
            {
                save = await _dataStore.SaveAsync(result);

                if (endpoint.RequiresDateRange && cursor != null && to.HasValue)
                {
                    // Chỉ nhích watermark khi LƯU DB THÀNH CÔNG.
                    if (save.Ok)
                        _cursorStore.MarkSuccess(cursor, to.Value, save.Received, save.Inserted, save.Updated, save.Unchanged, save.Failed);
                    else
                        _cursorStore.MarkFailed(cursor, save.Message);
                }
            }
            else if (endpoint.RequiresDateRange && cursor != null && !result.Success)
            {
                // Gọi API lỗi → ghi nhận, KHÔNG nhích watermark.
                _cursorStore.MarkFailed(cursor, result.Error);
            }

            // Ghi 1 dòng nhật ký file theo ngày cho MỌI lượt gọi (lưu vết dài hạn).
            _cursorStore.AppendHistory(new TollSyncHistoryEntry
            {
                At = now,
                DataType = endpoint.DataType,
                From = from,
                To = to,
                Received = save?.Received ?? result.RecordCount ?? 0,
                Inserted = save?.Inserted ?? 0,
                Updated = save?.Updated ?? 0,
                Unchanged = save?.Unchanged ?? 0,
                Failed = save?.Failed ?? 0,
                Deleted = save?.Deleted ?? 0,
                DurationMs = result.DurationMs,
                HttpStatus = result.StatusCode,
                Status = !result.Success ? "CallFailed" : save is null ? "CalledOnly" : save.Ok ? "Success" : "SaveFailed",
                Error = !result.Success ? result.Error : save is { Ok: false } ? save.Message : null,
                Url = result.RequestUrl,
                RequestId = requestId,
            });

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.Error(LogCategory.Sync, $"Lỗi không mong đợi khi gọi '{item.Name}': {ex.Message}");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Bật/tắt chế độ tự động gọi tất cả API theo chu kỳ cấu hình.</summary>
    [RelayCommand]
    private void ToggleAutoCall()
    {
        if (IsAutoRunning)
        {
            StopAutoCall();
            return;
        }

        var seconds = AutoCallIntervalSeconds > 0 ? AutoCallIntervalSeconds : 60;
        _autoTimer.Interval = TimeSpan.FromSeconds(seconds);
        _autoTimer.Start();
        IsAutoRunning = true;
        AutoStatus = $"Tự động: BẬT (mỗi {seconds}s)";
        Log(LogCategory.Sync, "Info", $"Bật tự động gọi mỗi {seconds} giây.");

        // Gọi ngay một lượt để không phải chờ hết chu kỳ đầu (tự động → theo watermark).
        _ = CallAllApisCoreAsync(manual: false);
    }

    private void StopAutoCall()
    {
        _autoTimer.Stop();
        IsAutoRunning = false;
        AutoStatus = "Tự động: TẮT";
        Log(LogCategory.Sync, "Info", "Đã tắt tự động gọi.");
    }

    private async void OnAutoTimerTick(object? sender, EventArgs e)
    {
        if (IsBusy)
        {
            Log(LogCategory.Sync, "Warning", "Bỏ qua nhịp tự động: đang có lượt gọi chưa xong.");
            return;
        }
        await CallAllApisCoreAsync(manual: false);
    }

    /// <summary>Thêm một dòng API trống để người dùng nhập trực tiếp trên form.</summary>
    [RelayCommand]
    private void AddEndpoint()
    {
        var ep = new EndpointItemViewModel { Name = "API mới", Enabled = true };
        Endpoints.Add(ep);
        SelectedEndpoint = ep;
        Log(LogCategory.Config, "Info", "Đã thêm 1 dòng API mới — nhập Tên/Url rồi bấm 'Gọi API đang chọn' (không cần lưu file).");
    }

    /// <summary>Xóa dòng API đang chọn khỏi danh sách (chỉ trong bộ nhớ; lưu file nếu muốn giữ).</summary>
    [RelayCommand]
    private void RemoveEndpoint()
    {
        if (SelectedEndpoint is null)
        {
            Log(LogCategory.Config, "Warning", "Chưa chọn API để xóa.");
            return;
        }

        var name = SelectedEndpoint.Name;
        Endpoints.Remove(SelectedEndpoint);
        SelectedEndpoint = Endpoints.FirstOrDefault();
        Log(LogCategory.Config, "Info", $"Đã xóa API '{name}' khỏi danh sách (bấm 'Lưu cấu hình' nếu muốn lưu lại).");
    }

    /// <summary>
    /// Cấu hình được quản lý tập trung ở bảng SysOpConfig (màn hình "Cấu hình hoạt động", nhóm toll_config),
    /// nên form KHÔNG lưu cấu hình tại chỗ. Chỉnh sửa trên lưới chỉ có tác dụng tạm thời để test.
    /// </summary>
    [RelayCommand]
    private void SaveConfig()
    {
        Log(LogCategory.Config, "Warning",
            $"Cấu hình lấy từ 'Cấu hình hoạt động' (SysOpConfig, nhóm '{TollOpConfigReader.GroupCode}'). " +
            "Sửa trên lưới chỉ tạm thời để test; muốn lưu vĩnh viễn hãy chỉnh trên màn hình Cấu hình hoạt động.");
    }

    [RelayCommand]
    private void ClearLog() => Logs.Clear();

    [RelayCommand]
    private void ClearResponse()
    {
        ResponseText = string.Empty;
        RequestUrlText = string.Empty;
        StatusText = "Chưa gọi";
    }

    // ================== HÀM PHỤ ==================

    /// <summary>Dựng TollOptions từ trạng thái hiện tại của form.</summary>
    private TollOptions BuildOptions() => new()
    {
        BaseUrl = string.IsNullOrWhiteSpace(BaseUrl) ? null : BaseUrl.Trim(),
        TimeoutSeconds = TimeoutSeconds,
        DateFormat = DateFormat,
        AutoCallIntervalSeconds = AutoCallIntervalSeconds,
        Endpoints = Endpoints.Select(e => e.ToEndpoint()).ToList(),
    };

    /// <summary>
    /// Chuyển log cho tầng Core (<see cref="TollLogger"/>) — nơi GHI FILE. Việc hiển thị lên view
    /// do <see cref="OnLogEntry"/> đảm nhiệm khi Core phát sự kiện, nên KHÔNG chèn Logs trực tiếp ở đây.
    /// </summary>
    private void Log(LogCategory category, string level, string message) => _logger.Log(category, level, message);

    /// <summary>
    /// Nhận mỗi dòng log Core phát ra và đưa lên danh sách hiển thị (mới nhất ở trên).
    /// Marshal về UI thread vì Core có thể phát log từ thread nền (async HTTP).
    /// </summary>
    private void OnLogEntry(TollLogEntry entry)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => AddLogToView(entry));
            return;
        }
        AddLogToView(entry);
    }

    private void AddLogToView(TollLogEntry entry)
    {
        Logs.Insert(0, new LogItem { Timestamp = entry.Timestamp, Level = entry.Level, Message = entry.Message });
        while (Logs.Count > 1000) Logs.RemoveAt(Logs.Count - 1);
    }
}
