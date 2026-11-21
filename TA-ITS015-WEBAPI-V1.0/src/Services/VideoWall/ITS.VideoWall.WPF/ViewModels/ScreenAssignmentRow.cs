using CommunityToolkit.Mvvm.ComponentModel;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Author: Đạt
/// Description: Một dòng trong bảng "từng cổng ra": gán nguồn tín hiệu cho đúng một màn hình,
/// cửa sổ sinh ra sẽ phủ kín màn hình đó.
///
/// Kích thước pixel lấy theo thứ tự WidthPx/HeightPx → suy từ Resolution. Nếu cả hai đều không
/// đọc được thì để trống cho người vận hành tự nhập (VwScreen lưu các trường này dạng chuỗi nên
/// dữ liệu thực tế có thể rỗng hoặc sai định dạng).
/// Created date: 25/08/2026
/// </summary>
public sealed partial class ScreenAssignmentRow : ObservableObject
{
    public ScreenAssignmentRow(VwScreenDto screen)
    {
        Screen = screen;

        var size = screen.TryResolvePixelSize();
        if (size is not null)
        {
            _width = size.Value.Width;
            _height = size.Value.Height;
        }
    }

    /// <summary>DTO dữ liệu màn hình tương ứng.</summary>
    public VwScreenDto Screen { get; }

    /// <summary>Tên hiển thị của màn hình.</summary>
    public string ScreenName => Screen.DisplayName;

    /// <summary>Tên cổng ra vật lý của màn hình.</summary>
    public string OutputPort => Screen.PortLabel;

    /// <summary>Tọa độ ô cột × hàng trên ma trận tường ghép.</summary>
    public string Grid => $"{Screen.GridCol ?? 0}×{Screen.GridRow ?? 0}";

    /// <summary>Độ phân giải hiển thị của màn hình.</summary>
    public string Resolution => Screen.Resolution ?? string.Empty;

    /// <summary>Kích thước không suy ra được từ dữ liệu màn hình — người vận hành phải tự nhập.</summary>
    public bool NeedsManualSize => Screen.TryResolvePixelSize() is null;

    [ObservableProperty]
    private bool _include = true;

    [ObservableProperty]
    private VwSourceDto? _selectedSource;

    [ObservableProperty]
    private int? _width;

    [ObservableProperty]
    private int? _height;

    /// <summary>Cho biết dòng gán nguồn đã đầy đủ thông tin để áp dụng kịch bản hay chưa.</summary>
    public bool IsReadyToApply =>
        Include
        && SelectedSource is not null
        && Width is > 0
        && Height is > 0;
}
