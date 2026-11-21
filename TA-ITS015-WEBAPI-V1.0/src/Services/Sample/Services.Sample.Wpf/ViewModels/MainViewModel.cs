using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Services.Shared.Events;
using Services.Shared.Hosting;

namespace Services.Sample.Wpf.ViewModels;

/// <summary>
/// Author: TAC Team
/// Description: ViewModel điều khiển luồng demo trên WPF qua IServiceManager facade + IServiceSimulator:
/// Nạp cấu hình (Init) -> Start -> Stop, và Simulate để test pipeline khi không có device/NATS thật.
/// Hoạt động hiển thị trực tiếp qua MediatR ActivityNotification.
/// Dùng CommunityToolkit.Mvvm ([ObservableProperty] + [RelayCommand]) — bỏ boilerplate INPC + ICommand thủ công.
/// Created date: 17/07/2026
/// </summary>
public sealed partial class MainViewModel : ObservableObject, INotificationHandler<ActivityNotification>
{
    private const int MaxActivities = 200;

    private readonly IServiceManager _manager;
    private readonly IServiceSimulator _simulator;
    // Capture UI dispatcher tại constructor (chạy trên UI thread). ActivityNotification có thể đến
    // từ background thread (Wolverine consumer, device runner) — phải marshal về UI thread trước khi
    // sửa ObservableCollection. KHÔNG dùng Dispatcher.CurrentDispatcher inline trong Handle() vì
    // nó trả về dispatcher của thread đang chạy (background), không phải UI thread.
    private readonly Dispatcher _dispatcher;

    /// <param name="manager">Facade lifecycle (Init/Start/Stop).</param>
    /// <param name="simulator">Facade giả lập (SimulateBatchAsync).</param>
    public MainViewModel(IServiceManager manager, IServiceSimulator simulator)
    {
        _manager = manager;
        _simulator = simulator;
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Bindable state — [ObservableProperty] sinh INPC tự động, không cần SetProperty thủ công.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Feed hoạt động (mới nhất trên cùng, cap MaxActivities).</summary>
    public ObservableCollection<Activity> Activities { get; } = new();

    [ObservableProperty]
    private string _statusMessage = "Chưa khởi tạo. Bấm \"Khởi tạo\" để bắt đầu.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InitCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(SimulateCommand))]
    private bool _isBusy;

    // Service State — SimulateCommand canExecute phụ thuộc State != Created.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SimulateCommand))]
    private ServiceManagerState _serviceState = ServiceManagerState.Created;

    // ════════════════════════════════════════════════════════════════════════
    // Commands — [RelayCommand] sinh ICommand property (vd. InitCommand từ InitAsync).
    // canExecute qua lambda; tự raise khi _isBusy / _isRunning / _serviceState đổi (qua attribute trên trên).
    // ════════════════════════════════════════════════════════════════════════

    [RelayCommand(CanExecute = nameof(CanInit))]
    private async Task InitAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang nạp cấu hình...";
        try
        {
            await _manager.InitAsync();
            ServiceState = _manager.State;
            StatusMessage = $"Đã khởi tạo service. Trạng thái: {ServiceState}. Bấm Start để chạy hoặc Simulate để test pipeline.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Không thể khởi tạo: {exception.Message}";
        }
        finally { IsBusy = false; }
    }
    private bool CanInit() => !IsBusy && !IsRunning;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang khởi động...";
        try
        {
            await _manager.StartAsync();
            ServiceState = _manager.State;
            IsRunning = true;
            StatusMessage = "Đang chạy luồng thu thập Device A + Device B...";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Không thể bắt đầu: {exception.Message}";
        }
        finally { IsBusy = false; }
    }
    private bool CanStart() => !IsBusy && !IsRunning;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        IsBusy = true;
        try
        {
            await _manager.StopAsync();
            ServiceState = _manager.State;
            IsRunning = false;
            StatusMessage = "Đã dừng.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Không thể dừng: {exception.Message}";
        }
        finally { IsBusy = false; }
    }
    private bool CanStop() => IsRunning && !IsBusy;

    // Simulate: sinh batch data giả lập. Không đổi IsRunning (runner thật không ảnh hưởng).
    [RelayCommand(CanExecute = nameof(CanSimulate))]
    private async Task SimulateAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang sinh batch dữ liệu giả lập...";
        try
        {
            await _simulator.SimulateBatchAsync();
            StatusMessage = "Đã sinh batch giả lập. Xem activity feed bên dưới.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Không thể sinh batch: {exception.Message}";
        }
        finally { IsBusy = false; }
    }
    private bool CanSimulate() => !IsBusy && ServiceState != ServiceManagerState.Created;

    [RelayCommand(CanExecute = nameof(CanClear))]
    private Task ClearActivitiesAsync()
    {
        Activities.Clear();
        ClearActivitiesCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }
    private bool CanClear() => Activities.Count > 0;

    // ════════════════════════════════════════════════════════════════════════
    // Activity feed (MediatR) — deliver notification từ mọi producer về đây; marshal về UI thread.
    // ════════════════════════════════════════════════════════════════════════

    public Task Handle(ActivityNotification notification, CancellationToken cancellationToken)
    {
        var activity = notification.Activity;
        if (_dispatcher.CheckAccess())
            AppendActivity(activity);
        else
            _dispatcher.BeginInvoke(new Action(() => AppendActivity(activity)));

        return Task.CompletedTask;
    }

    private void AppendActivity(Activity activity)
    {
        Activities.Insert(0, activity);
        while (Activities.Count > MaxActivities)
            Activities.RemoveAt(Activities.Count - 1);

        ClearActivitiesCommand.NotifyCanExecuteChanged();
    }
}
