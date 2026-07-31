using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using ITS.Sync.Infrastructure.Messaging;
using ITS.Sync.Infrastructure.Persistence;
using ITS.Sync.Wpf.Infrastructure;

namespace ITS.Sync.Wpf.ViewModels;

/// <summary>
/// ViewModel cửa sổ chính. Mỗi bảng có log CHI TIẾT RIÊNG (StrategyItemViewModel.Logs);
/// khung "Nhật ký chung" (Control) chỉ ghi sự kiện engine + test kết nối.
/// Toàn bộ logic điều khiển chu kỳ nằm ở <see cref="ISyncController"/>; ViewModel chỉ lo UI.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ISyncController _controller;
    private readonly SqlSugarFactory _dbFactory;
    private readonly NatsSyncListener _natsListener;

    // Progress được tạo trên UI thread => callback tự marshal về UI thread,
    // nên cập nhật lưới/log từ đây luôn an toàn.
    private readonly Progress<SyncRunReport> _cycleProgress;
    private readonly Progress<Exception> _errorProgress;

    // Danh sách bảng hiển thị sẵn (đúng thứ tự đăng ký trong AddSync).
    private static readonly string[] KnownTables =
    {
        "SysConfigType", "SysConfigData", "SysOpConfig",
        "TmsEquipmentType", "TmsEventType", "TmsWorkUnit", "TmsZone", "TmsMap",
        "TmsEquipment", "TmsMapDetail", "TmsZoneEquipment", "TmsIncident", "TmsZoneStatus"
    };

    public MainViewModel(ISyncController controller, SqlSugarFactory dbFactory, NatsSyncListener natsListener)
    {
        _controller = controller;
        _natsListener = natsListener;
        _dbFactory = dbFactory;

        // Tạo trên UI thread để bắt SynchronizationContext hiện tại.
        _cycleProgress = new Progress<SyncRunReport>(OnCycleReport);
        _errorProgress = new Progress<Exception>(OnCycleError);

        StartCommand          = new RelayCommand(async () => await OnStartAsync(), () => !IsRunning);
        StopCommand           = new RelayCommand(OnStop, () => IsRunning);
        RunOnceCommand        = new RelayCommand(async () => await OnRunOnceAsync(), () => !IsRunning);
        TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
        ClearLogCommand       = new RelayCommand(() => SelectedStrategy?.Logs.Clear());

        foreach (var name in KnownTables)
            Strategies.Add(new StrategyItemViewModel { IsEnabled = true, TableName = name });

        SelectedStrategy = Strategies.FirstOrDefault();

        // Lắng nghe tín hiệu NATS để đồng bộ một bảng lẻ (độc lập vòng lặp định kỳ).
        // Chỉ chạy khi Nats:Enabled = true; báo cáo đi qua cùng Progress nên UI cập nhật an toàn.
        var natsStatusProgress = new Progress<string>(s =>
        {
            NatsStatus = s;
            Log(Control, "NATS: " + s);
        });
        _natsListener.Start(_cycleProgress, _errorProgress, natsStatusProgress);
    }

    // ================== TRẠNG THÁI ENGINE ==================
    private string _status = "Idle";
    public string Status
    {
        get => _status;
        set { if (Set(ref _status, value)) OnPropertyChanged(nameof(StatusBrush)); }
    }

    public Brush StatusBrush => _status switch
    {
        "Running" => Brushes.LimeGreen,
        "Stopped" => Brushes.Gray,
        "Error"   => Brushes.Red,
        _         => Brushes.Goldenrod
    };

    // ================== THÔNG TIN DB ==================
    public string SourceDbName { get; set; } = "Dev_ITS10";
    public string TargetDbName { get; set; } = "DEV_ITS015_WP";

    // ================== TRẠNG THÁI NATS ==================
    private string _natsStatus = "Chưa khởi động";
    /// <summary>Mô tả trạng thái kết nối NATS để bind lên UI.</summary>
    public string NatsStatus
    {
        get => _natsStatus;
        private set { if (Set(ref _natsStatus, value)) OnPropertyChanged(nameof(NatsStatusBrush)); }
    }

    /// <summary>Xanh khi đã kết nối, đỏ khi lỗi, xám khi tắt/chưa chạy.</summary>
    public Brush NatsStatusBrush => _natsListener.IsConnected
        ? Brushes.LimeGreen
        : _natsStatus.StartsWith("LỖI", StringComparison.OrdinalIgnoreCase) ? Brushes.Red : Brushes.Gray;

    // ================== CẤU HÌNH CHẠY ==================
    private string _checkIntervalSeconds = "60";
    public string CheckIntervalSeconds { get => _checkIntervalSeconds; set => Set(ref _checkIntervalSeconds, value); }

    private int _progress;
    public int Progress { get => _progress; set => Set(ref _progress, value); }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set { if (Set(ref _isRunning, value)) CommandManager.InvalidateRequerySuggested(); }
    }

    // ================== DỮ LIỆU HIỂN THỊ ==================
    public ObservableCollection<StrategyItemViewModel> Strategies { get; } = new();

    private StrategyItemViewModel? _selectedStrategy;
    /// <summary>Bảng đang chọn trên lưới - khung chi tiết hiển thị log riêng của bảng này.</summary>
    public StrategyItemViewModel? SelectedStrategy { get => _selectedStrategy; set => Set(ref _selectedStrategy, value); }

    /// <summary>Nhật ký chung: sự kiện engine, test kết nối, tổng kết chu kỳ.</summary>
    public ObservableCollection<string> Control { get; } = new();

    // ================== LỆNH ==================
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RunOnceCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand ClearLogCommand { get; }

    // ================== XỬ LÝ ==================
    private async Task OnStartAsync()
    {
        if (IsRunning) return;
        if (!int.TryParse(CheckIntervalSeconds, out var sec) || sec < 1)
        {
            Log(Control, "Chu kỳ không hợp lệ (phải là số giây > 0).");
            return;
        }

        // Init: kiểm tra kết nối 2 DB trước — lỗi thì KHÔNG cho Start.
        if (!await InitOkAsync()) return;

        IsRunning = true;
        Status = "Running";
        Log(Control, $"START - đồng bộ định kỳ mỗi {sec} giây.");

        // Bộ điều khiển sở hữu vòng lặp: chạy ngay một chu kỳ rồi lặp mỗi sec giây.
        _controller.Start(sec, _cycleProgress, _errorProgress);
    }

    private async Task OnRunOnceAsync()
    {
        // Chạy 1 lần cũng phải Init (kiểm tra kết nối) trước.
        if (!await InitOkAsync()) return;
        await _controller.RunOnceAsync(_cycleProgress, _errorProgress);
    }

    /// <summary>Chạy Init (test kết nối 2 DB), ghi log kết quả, trả về true nếu sẵn sàng chạy.</summary>
    private async Task<bool> InitOkAsync()
    {
        Log(Control, "Init - kiểm tra kết nối 2 DB...");
        var init = await _controller.InitAsync();

        Log(Control, init.SourceOk ? $"Nguồn ({SourceDbName}): OK" : $"Nguồn ({SourceDbName}): LỖI - {init.SourceError}");
        Log(Control, init.TargetOk ? $"Đích ({TargetDbName}): OK" : $"Đích ({TargetDbName}): LỖI - {init.TargetError}");

        if (!init.Ok)
        {
            Status = "Error";
            Log(Control, "Init THẤT BẠI - không thể bắt đầu đồng bộ.");
        }
        return init.Ok;
    }

    private void OnStop()
    {
        _controller.Stop();
        IsRunning = false;
        Status = "Stopped";
        Log(Control, "STOP - đã dừng.");
    }

    /// <summary>
    /// Callback báo cáo một chu kỳ (chạy trên UI thread vì Progress được tạo trên UI thread).
    /// Cập nhật lưới + log chi tiết, ghi dòng tổng kết vào Nhật ký chung và đặt Progress=100.
    /// </summary>
    private void OnCycleReport(SyncRunReport report)
    {
        ApplyReport(report);
        Progress = 100;

        Log(Control, $"Chu kỳ xong: +{report.TotalInserted} ~{report.TotalUpdated} -{report.TotalDeleted}"
                    + (report.AllSucceeded ? "" : " (có bảng LỖI)"));

        if (!report.AllSucceeded && !IsRunning) Status = "Error";
    }

    /// <summary>Callback báo lỗi một chu kỳ (chạy trên UI thread).</summary>
    private void OnCycleError(Exception ex)
    {
        Log(Control, "Lỗi khi đồng bộ: " + ex.Message);
        if (!IsRunning) Status = "Error";
    }

    private void ApplyReport(SyncRunReport report)
    {
        const int maxDetail = 200;

        foreach (var r in report.Results)
        {
            var row = Strategies.FirstOrDefault(s => s.TableName == r.TableName);
            if (row is null)
            {
                row = new StrategyItemViewModel { TableName = r.TableName, IsEnabled = true };
                Strategies.Add(row);
            }

            row.Inserted = r.Inserted;
            row.Updated = r.Updated;
            row.Deleted = r.Deleted;
            row.Unchanged = r.Unchanged;
            row.Status = r.Success ? "Thành công" : "Lỗi";
            row.LastRunAt = DateTime.Now.ToString("HH:mm:ss");

            // Ghi log CHI TIẾT RIÊNG cho bảng này (làm mới mỗi lần chạy).
            row.Logs.Clear();
            row.Logs.Add($"[{row.LastRunAt}] +{r.Inserted} ~{r.Updated} -{r.Deleted} ={r.Unchanged} ({r.Duration.TotalSeconds:0.00}s)"
                         + (r.Success ? "" : $"  LỖI: {r.Error}"));

            var shown = 0;
            foreach (var change in r.Changes)
            {
                if (shown >= maxDetail)
                {
                    row.Logs.Add($"   ...(và {r.Changes.Count - maxDetail} thay đổi nữa)");
                    break;
                }
                shown++;

                var line = $"   [{change.Action}] ID={change.Id}";
                if (!string.IsNullOrEmpty(change.Detail)) line += $" | {change.Detail}";
                row.Logs.Add(line);
            }
        }
    }

    private async Task TestConnectionAsync()
    {
        Log(Control, "Kiểm tra kết nối 2 DB...");

        var src = await _dbFactory.TestConnectionAsync("Source");
        Log(Control, src.Ok ? $"Nguồn ({SourceDbName}): OK" : $"Nguồn ({SourceDbName}): LỖI - {src.Error}");

        var tgt = await _dbFactory.TestConnectionAsync("Target");
        Log(Control, tgt.Ok ? $"Đích ({TargetDbName}): OK" : $"Đích ({TargetDbName}): LỖI - {tgt.Error}");
    }

    private static void Log(ObservableCollection<string> target, string message)
    {
        target.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        while (target.Count > 1000) target.RemoveAt(target.Count - 1);
    }

    // ================== INotifyPropertyChanged ==================
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name!);
        return true;
    }
}
