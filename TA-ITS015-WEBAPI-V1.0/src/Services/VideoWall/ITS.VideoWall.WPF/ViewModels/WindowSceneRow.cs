using CommunityToolkit.Mvvm.ComponentModel;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Author: Đạt
/// Description: Một dòng trong bảng "cửa sổ xếp lớp": nhập trực tiếp toạ độ/kích thước/ZIndex
/// theo dạng biểu mẫu (không kéo-thả như bên web Vue).
/// Created date: 25/08/2026
/// </summary>
public sealed partial class WindowSceneRow : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private VwSourceDto? _selectedSource;

    [ObservableProperty]
    private int? _x = 0;

    [ObservableProperty]
    private int? _y = 0;

    [ObservableProperty]
    private int? _width;

    [ObservableProperty]
    private int? _height;

    [ObservableProperty]
    private int? _zIndex = 1;

    /// <summary>Cho biết cửa sổ đã sẵn sàng để áp dụng hay chưa (đầy đủ nguồn, kích thước, tọa độ).</summary>
    public bool IsReadyToApply =>
        SelectedSource is not null
        && X is >= 0
        && Y is >= 0
        && Width is > 0
        && Height is > 0
        && ZIndex is >= 0;
}
