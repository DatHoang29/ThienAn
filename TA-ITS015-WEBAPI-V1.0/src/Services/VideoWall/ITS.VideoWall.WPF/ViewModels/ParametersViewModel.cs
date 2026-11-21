using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Author: Đạt
/// Description: Tab Tham số: xem (chỉ đọc) thông số bộ điều khiển đang chọn kèm danh sách màn hình
/// và cổng khe cắm. Tự nạp lại khi tab Kết nối đổi bộ điều khiển.
/// Created date: 25/08/2026
/// </summary>
public sealed partial class ParametersViewModel : ObservableObject
{
    private readonly ConnectionViewModel _connection;

    public ParametersViewModel(ConnectionViewModel connection)
    {
        _connection = connection;
    }

    /// <summary>Danh sách cấu hình màn hình hiển thị (VwScreenDto).</summary>
    public ObservableCollection<VwScreenDto> Screens { get; } = [];

    /// <summary>Danh sách cổng ra của bộ điều khiển (VwControllerOutputDto, đồng bộ từ thiết bị).</summary>
    public ObservableCollection<VwControllerOutputDto> ControllerOutputs { get; } = [];

    /// <summary>Thông báo trạng thái hiển thị trên giao diện tham số.</summary>
    [ObservableProperty]
    private string _statusMessage = "Chế độ trực tiếp: xem thông số tại tab Kết nối & Khảo sát (Probe).";

    /// <summary>Cờ trạng thái đang tải dữ liệu tham số.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private bool _isBusy;

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task Load(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        StatusMessage = "Chế độ trực tiếp: xem thông số tại tab Kết nối & Khảo sát (Probe).";
    }

    private bool CanLoad() => !IsBusy;
}
