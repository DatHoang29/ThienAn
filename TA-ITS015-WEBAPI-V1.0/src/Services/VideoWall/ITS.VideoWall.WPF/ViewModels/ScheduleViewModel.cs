using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services.Shared.Events;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Author: Đạt
/// Description: Tab Lịch: CRUD thuần trên VwSchedule qua các route Page/Add/Update/Delete.
/// Sửa bản ghi thì gửi TRỌN bản ghi (backend map cả đối tượng vào entity, thiếu trường nào
/// là trường đó bị ghi null).
/// Created date: 25/08/2026
/// </summary>
public sealed partial class ScheduleViewModel(
    VideoWallApiClient videoWallApiClient,
    ActivityPublisher activityPublisher) : ObservableObject
{
    private const string ScheduleCodePrefix = "VWSCHED";

    /// <summary>Danh sách lịch biểu hiển thị (VwScheduleDto).</summary>
    public ObservableCollection<VwScheduleDto> Schedules { get; } = [];

    /// <summary>Danh sách kịch bản hiển thị (VwSceneDto) dùng cho lựa chọn lịch.</summary>
    public ObservableCollection<VwSceneDto> Scenes { get; } = [];

    /// <summary>Danh sách tùy chọn trạng thái hoạt động (Enable, Disable).</summary>
    public IReadOnlyList<VwStatus> StatusOptions { get; } = [VwStatus.Enable, VwStatus.Disable];

    /// <summary>Bản ghi lịch biểu đang được chọn trong danh sách.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteScheduleCommand))]
    private VwScheduleDto? _selectedSchedule;

    /// <summary>Tên lịch biểu trong form nhập liệu.</summary>
    [ObservableProperty]
    private string _formName = string.Empty;

    /// <summary>Kịch bản hiển thị mục tiêu được gán trong form lịch.</summary>
    [ObservableProperty]
    private VwSceneDto? _formTargetScene;

    /// <summary>Loại lịch biểu (1: lặp lại theo tuần, 2: một lần, 3: cron).</summary>
    [ObservableProperty]
    private int? _formScheduleType = 1;

    /// <summary>Thời gian kích hoạt lịch biểu (HH:mm).</summary>
    [ObservableProperty]
    private string _formTime = "07:00";

    /// <summary>Danh sách các ngày trong tuần áp dụng lịch (ví dụ: 1,2,3,4,5).</summary>
    [ObservableProperty]
    private string _formWeekdays = "1,2,3,4,5";

    /// <summary>Ngày kích hoạt cụ thể đối với loại lịch một lần (yyyy-MM-dd).</summary>
    [ObservableProperty]
    private string _formOnceDate = string.Empty;

    /// <summary>Biểu thức Cron áp dụng cho lịch chạy nâng cao.</summary>
    [ObservableProperty]
    private string _formCronExpr = string.Empty;

    /// <summary>Hành động thực hiện khi tới lịch (on/off).</summary>
    [ObservableProperty]
    private string _formAction = "on";

    /// <summary>Trạng thái kích hoạt của lịch (Enable/Disable).</summary>
    [ObservableProperty]
    private VwStatus _formStatus = VwStatus.Enable;

    /// <summary>Thông báo trạng thái phản hồi trên giao diện lịch biểu.</summary>
    [ObservableProperty]
    private string _statusMessage = "";

    /// <summary>Cờ trạng thái đang xử lý tác vụ lịch biểu.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveScheduleCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteScheduleCommand))]
    private bool _isBusy;

    /// <summary>Đang sửa bản ghi có sẵn (khác với tạo mới).</summary>
    public bool IsEditing => SelectedSchedule is not null;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task Refresh(CancellationToken cancellationToken)
    {
        await Run("Đang nạp danh sách lịch...", async () =>
        {
            var total = await Reload(cancellationToken).ConfigureAwait(true);
            StatusMessage = $"Đã nạp {Schedules.Count} lịch trên tổng {total} bản ghi.";
        }).ConfigureAwait(true);
    }

    [RelayCommand]
    private void NewSchedule()
    {
        SelectedSchedule = null;
        FormName = string.Empty;
        FormTargetScene = null;
        FormScheduleType = 1;
        FormTime = "07:00";
        FormWeekdays = "1,2,3,4,5";
        FormOnceDate = string.Empty;
        FormCronExpr = string.Empty;
        FormAction = "on";
        FormStatus = VwStatus.Enable;
        StatusMessage = "Đang tạo lịch mới.";
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveSchedule(CancellationToken cancellationToken)
    {
        var editing = SelectedSchedule;
        var schedule = new VwScheduleDto
        {
            ID = editing?.ID,
            Code = editing?.Code ?? VideoWallApiClient.NewCode(ScheduleCodePrefix),
            Name = FormName.Trim(),
            TargetSceneId = FormTargetScene?.ID,
            ScheduleType = FormScheduleType,
            Time = NullIfBlank(FormTime),
            Weekdays = NullIfBlank(FormWeekdays),
            OnceDate = NullIfBlank(FormOnceDate),
            CronExpr = NullIfBlank(FormCronExpr),
            Action = NullIfBlank(FormAction),
            Status = FormStatus,
            Remark = editing?.Remark,
        };

        await Run(editing is null ? "Đang thêm lịch..." : "Đang cập nhật lịch...", async () =>
        {
            if (editing is null)
                await videoWallApiClient.AddSchedule(schedule, cancellationToken).ConfigureAwait(true);
            else
                await videoWallApiClient.UpdateSchedule(schedule, cancellationToken).ConfigureAwait(true);

            await Reload(cancellationToken).ConfigureAwait(true);

            StatusMessage = editing is null
                ? $"Đã thêm lịch {schedule.Name} [{schedule.Code}]."
                : $"Đã cập nhật lịch {schedule.Name}.";
        }).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteSchedule(CancellationToken cancellationToken)
    {
        var scheduleId = SelectedSchedule?.ID;
        if (string.IsNullOrWhiteSpace(scheduleId))
            return;

        await Run("Đang xoá lịch...", async () =>
        {
            await videoWallApiClient.DeleteSchedule(scheduleId, cancellationToken).ConfigureAwait(true);
            SelectedSchedule = null;
            await Reload(cancellationToken).ConfigureAwait(true);
            StatusMessage = "Đã xoá lịch.";
        }).ConfigureAwait(true);
    }

    private bool CanRun() => !IsBusy;

    private bool CanSave() => !IsBusy && !string.IsNullOrWhiteSpace(FormName);

    private bool CanDelete() => !IsBusy && !string.IsNullOrWhiteSpace(SelectedSchedule?.ID);

    private async Task Run(string busyMessage, Func<Task> action)
    {
        IsBusy = true;
        StatusMessage = busyMessage;

        try
        {
            await action().ConfigureAwait(true);
        }
        catch (ApiException exception)
        {
            StatusMessage = $"Thất bại: {exception.Message}";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Lỗi không lường trước: {exception.Message}";
            activityPublisher.Publish("Schedule", exception.Message, ActivityLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Nạp lại lịch + kịch bản. Tách khỏi Refresh để Save/Delete gọi lại được mà không lồng
    /// hai lần Run (lồng vào nhau sẽ ghi đè StatusMessage và nhả IsBusy sớm).
    /// Trả về tổng số bản ghi phía backend.
    /// </summary>
    private async Task<int> Reload(CancellationToken cancellationToken)
    {
        var paged = await videoWallApiClient
            .PageSchedules(new VwSchedulePageInput { Page = 1, PageSize = 200 }, cancellationToken)
            .ConfigureAwait(true);

        Schedules.Clear();
        foreach (var schedule in paged.Records ?? [])
            Schedules.Add(schedule);

        // Nạp kèm kịch bản để dropdown "Kịch bản đích" hiển thị tên thay vì ID trần.
        var scenes = await videoWallApiClient.ListScenes(new VwSceneFilter(), cancellationToken).ConfigureAwait(true);
        Scenes.Clear();
        foreach (var scene in scenes)
            Scenes.Add(scene);

        return paged.Total;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Chọn một dòng trên bảng thì đổ dữ liệu xuống biểu mẫu để sửa.</summary>
    partial void OnSelectedScheduleChanged(VwScheduleDto? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        if (value is null)
            return;

        FormName = value.Name ?? string.Empty;
        FormTargetScene = Scenes.FirstOrDefault(scene => scene.ID == value.TargetSceneId);
        FormScheduleType = value.ScheduleType;
        FormTime = value.Time ?? string.Empty;
        FormWeekdays = value.Weekdays ?? string.Empty;
        FormOnceDate = value.OnceDate ?? string.Empty;
        FormCronExpr = value.CronExpr ?? string.Empty;
        FormAction = value.Action ?? string.Empty;
        FormStatus = value.Status ?? VwStatus.Enable;
        StatusMessage = $"Đang sửa lịch {value.Name} [{value.Code}].";
    }

    partial void OnFormNameChanged(string value) => SaveScheduleCommand.NotifyCanExecuteChanged();
}
