using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Win32;
using Services.Shared.Events;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Auth;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Description: ViewModel trung tâm của ứng dụng WPF VideoWall điều phối các tab chức năng và ghi nhận log
/// Created date: 13/09/2026
/// </summary>
public sealed partial class MainViewModel
    : ObservableObject, INotificationHandler<ActivityNotification>, INotificationHandler<DeviceStepNotification>
{
    private const int MaxActivities = 500;

    private readonly SessionState _sessionState;
    private readonly ActivityPublisher _activityPublisher;
    private readonly string _logDir;
    private bool _fileLogWarned;
    private static readonly object _fileLock = new();

    // Chụp Dispatcher của UI thread tại constructor. Notification có thể tới từ luồng nền
    // (HttpClient continuation) — phải marshal về UI thread trước khi sửa ObservableCollection.
    private readonly Dispatcher _dispatcher;

    public MainViewModel(
        SessionState sessionState,
        ActivityPublisher activityPublisher,
        ConnectionViewModel connection,
        ParametersViewModel parameters,
        SceneSetupViewModel sceneSetup,
        ScheduleViewModel schedule,
        ScenarioViewModel scenario)
    {
        _sessionState = sessionState;
        _activityPublisher = activityPublisher;
        _dispatcher = Dispatcher.CurrentDispatcher;

        _logDir = ResolveLogDirectory();

        if (!Directory.Exists(_logDir))
            Directory.CreateDirectory(_logDir);

        Connection = connection;
        Parameters = parameters;
        SceneSetup = sceneSetup;
        Schedule = schedule;
        Scenario = scenario;

        Activities.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasActivities));
    }

    /// <summary>ViewModel quản lý kết nối thiết bị và khảo sát Probe.</summary>
    public ConnectionViewModel Connection { get; }

    /// <summary>ViewModel quản lý thông số cấu hình màn hình và cổng kết nối.</summary>
    public ParametersViewModel Parameters { get; }

    /// <summary>ViewModel thiết lập ma trận hiển thị và phân bổ cửa sổ kịch bản.</summary>
    public SceneSetupViewModel SceneSetup { get; }

    /// <summary>ViewModel quản lý lịch trình bật/tắt kịch bản tự động.</summary>
    public ScheduleViewModel Schedule { get; }

    /// <summary>ViewModel kiểm thử và thực thi các kịch bản gọi chuỗi ISAPI.</summary>
    public ScenarioViewModel Scenario { get; }

    /// <summary>Log hoạt động: mới nhất trên cùng, giữ tối đa MaxActivities dòng.</summary>
    public ObservableCollection<EventTriggerLogEntry> Activities { get; } = [];

    /// <summary>Cờ kiểm tra có bản ghi hoạt động log nào trong danh sách hay không.</summary>
    public bool HasActivities => Activities.Count > 0;

    /// <summary>Chuỗi tổng hợp trạng thái phiên làm việc hoặc tài khoản người dùng.</summary>
    public string SessionSummary =>
        _sessionState.IsAuthenticated
            ? $"Đã đăng nhập: {_sessionState.Username}"
            : "Chế độ: Đấu nối trực tiếp";

    /// <summary>Bản ghi log hoạt động đang được người dùng chọn.</summary>
    [ObservableProperty]
    private EventTriggerLogEntry? _selectedActivity;

    /// <summary>Cờ tự động cuộn danh sách log khi có dòng thông báo mới.</summary>
    [ObservableProperty]
    private bool _autoScrollLog = true;

    /// <summary>Chỉ số index của tab chức năng đang hiển thị trên giao diện chính.</summary>
    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>Cờ xác định vùng hiển thị kết quả phản hồi có đang mở hay không.</summary>
    public bool IsResponseVisible => SelectedTabIndex >= 1;

    private System.Windows.GridLength _savedResponseHeight = new(1, System.Windows.GridUnitType.Star);

    /// <summary>Chiều cao hàng hiển thị kết quả phản hồi ISAPI trên giao diện.</summary>
    public System.Windows.GridLength ResponseRowHeight
    {
        get => IsResponseVisible ? _savedResponseHeight : new System.Windows.GridLength(0);
        set
        {
            if (IsResponseVisible && value.Value > 0)
            {
                _savedResponseHeight = value;
            }
            OnPropertyChanged();
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsResponseVisible));
        OnPropertyChanged(nameof(ResponseRowHeight));
    }

    public Task Handle(ActivityNotification notification, CancellationToken cancellationToken)
    {
        Append(new EventTriggerLogEntry(notification.Activity));
        return Task.CompletedTask;
    }

    public Task Handle(DeviceStepNotification notification, CancellationToken cancellationToken)
    {
        Append(new EventTriggerLogEntry(notification.Activity, notification.Step));
        return Task.CompletedTask;
    }

    /// <summary>Cập nhật tóm tắt phiên khi khởi động hoặc nạp lại.</summary>
    public void ReportReady()
    {
        OnPropertyChanged(nameof(SessionSummary));
    }

    [RelayCommand(CanExecute = nameof(CanClearActivities))]
    private void ClearActivities()
    {
        Activities.Clear();
        SelectedActivity = null;
        ClearActivitiesCommand.NotifyCanExecuteChanged();
        ExportLogCommand.NotifyCanExecuteChanged();
    }

    private bool CanClearActivities() => Activities.Count > 0;

    public static string FormatActivitiesAsJson(IEnumerable<EventTriggerLogEntry> activities)
    {
        var rows = activities
            .Select(a => new
            {
                Time = a.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                a.Stage,
                Level = a.Level.ToString(),
                a.Detail,
                Method = a.Step?.Method,
                Endpoint = a.Step?.Endpoint,
                HttpStatus = a.Step?.HttpStatus,
                Success = a.Step?.Success,
                Message = a.Step?.Message,
                RequestPayload = a.Step?.RequestXml,
                ResponsePayload = a.Step?.ResponseXml,
            })
            .ToList();

        return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string FormatActivitiesAsJsonl(IEnumerable<EventTriggerLogEntry> activities)
    {
        var sb = new StringBuilder();
        foreach (var a in activities)
        {
            var row = new
            {
                Time = a.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                a.Stage,
                Level = a.Level.ToString(),
                a.Detail,
                Method = a.Step?.Method,
                Endpoint = a.Step?.Endpoint,
                HttpStatus = a.Step?.HttpStatus,
                Success = a.Step?.Success,
                Message = a.Step?.Message,
                RequestPayload = a.Step?.RequestXml,
                ResponsePayload = a.Step?.ResponseXml,
            };
            sb.AppendLine(JsonSerializer.Serialize(row));
        }
        return sb.ToString();
    }

    public static void AppendSessionLogRecord(string sessionLogPath, string recordJson)
    {
        lock (_fileLock)
        {
            if (!File.Exists(sessionLogPath) || new FileInfo(sessionLogPath).Length == 0)
            {
                File.WriteAllText(sessionLogPath, "[\n  " + recordJson + "\n]", Encoding.UTF8);
            }
            else
            {
                var text = File.ReadAllText(sessionLogPath, Encoding.UTF8).TrimEnd();
                if (text.EndsWith(']'))
                {
                    var trimmed = text[..^1].TrimEnd();
                    var delimiter = trimmed == "[" ? "\n  " : ",\n  ";
                    File.WriteAllText(sessionLogPath, trimmed + delimiter + recordJson + "\n]", Encoding.UTF8);
                }
                else
                {
                    var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var sb = new StringBuilder();
                    sb.AppendLine("[");
                    for (int i = 0; i < lines.Length; i++)
                    {
                        sb.Append("  ").Append(lines[i]).AppendLine(",");
                    }
                    sb.Append("  ").AppendLine(recordJson);
                    sb.AppendLine("]");
                    File.WriteAllText(sessionLogPath, sb.ToString(), Encoding.UTF8);
                }
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportLog))]
    private void ExportLog()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Xuất log phiên đang chạy",
            Filter = "JSON (*.json)|*.json|JSON Lines (*.jsonl)|*.jsonl",
            FileName = $"videowall-log_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            DefaultExt = ".json",
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            if (dialog.FileName.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
            {
                var jsonl = FormatActivitiesAsJsonl(Activities);
                File.WriteAllText(dialog.FileName, jsonl, Encoding.UTF8);
            }
            else
            {
                var json = FormatActivitiesAsJson(Activities);
                File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
            }

            _activityPublisher.Publish("Log", $"Đã xuất {Activities.Count} dòng log ra {dialog.FileName}.", ActivityLevel.Success);
        }
        catch (Exception ex)
        {
            _activityPublisher.Publish("Log", $"Xuất log thất bại: {ex.Message}", ActivityLevel.Error);
        }
    }

    private bool CanExportLog() => Activities.Count > 0;

    private void Append(EventTriggerLogEntry entry)
    {
        if (_dispatcher.CheckAccess())
            AppendCore(entry);
        else
            _dispatcher.BeginInvoke(new Action(() => AppendCore(entry)));
    }

    private void AppendCore(EventTriggerLogEntry entry)
    {
        Activities.Insert(0, entry);
        while (Activities.Count > MaxActivities)
            Activities.RemoveAt(Activities.Count - 1);

        Console.WriteLine($"[{entry.TimestampLocal}] [{entry.Level}] [{entry.Stage}] {entry.Detail}");

        // Chỉ ghi log ra file khi có thao tác gọi API thực tế (GET, POST, PUT, DELETE)
        if (entry.Step is not null && !string.IsNullOrWhiteSpace(entry.Step.Method))
        {
            try
            {
                var logRecord = new
                {
                    Time = entry.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    entry.Stage,
                    Level = entry.Level.ToString(),
                    entry.Detail,
                    entry.Step.Name,
                    entry.Step.Method,
                    entry.Step.Endpoint,
                    entry.Step.HttpStatus,
                    entry.Step.Success,
                    entry.Step.Message,
                    RequestPayload = entry.Step.RequestXml,
                    ResponsePayload = entry.Step.ResponseXml,
                };

                var sessionLogPath = Path.Combine(_logDir, $"session_{DateTime.Now:yyyyMMdd}.json");
                var recordJson = JsonSerializer.Serialize(logRecord);
                AppendSessionLogRecord(sessionLogPath, recordJson);
            }
            catch (Exception ex)
            {
                if (!_fileLogWarned)
                {
                    _fileLogWarned = true;
                    _activityPublisher.Publish("Log", $"Không ghi được log ra file: {ex.Message}", ActivityLevel.Warning);
                }
            }
        }

        ClearActivitiesCommand.NotifyCanExecuteChanged();
        ExportLogCommand.NotifyCanExecuteChanged();
    }

    private static string ResolveLogDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (string.Equals(dir.Name, "ThienAn", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(dir.FullName, "logs");
            dir = dir.Parent;
        }

        if (Directory.Exists(@"C:\ThienAn"))
            return Path.Combine(@"C:\ThienAn", "logs");

        return Path.Combine(AppContext.BaseDirectory, "logs");
    }
}
