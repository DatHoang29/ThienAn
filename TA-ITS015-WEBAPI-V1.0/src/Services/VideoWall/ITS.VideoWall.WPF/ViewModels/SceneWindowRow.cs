using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Author: Đạt
/// Description: Một dòng trong bảng "3. Cửa sổ đã lưu của Kịch bản": hỗ trợ chọn nhiều (Multi-Select)
/// để xoá hàng loạt và chỉnh sửa thông số từng cửa sổ.
/// Created date: 30/08/2026
/// </summary>
public sealed partial class SceneWindowRow : ObservableObject
{
    public SceneWindowRow(VwWindowSceneDto window)
    {
        Window = window;
        InitSubCells(null);
    }

    /// <summary>DTO dữ liệu gốc của cửa sổ kịch bản.</summary>
    public VwWindowSceneDto Window { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isActiveSelected;

    [ObservableProperty]
    private string? _readSurveyInfo;

    /// <summary>Cờ đánh dấu cửa sổ đang được kéo thả tương tác trên Canvas.</summary>
    public bool IsInteractivelyDragging { get; set; }

    public void SetGeometry(int x, int y, int w, int h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public void UpdateLabelFromCurrentPosition()
    {
        var screen = SelectedStartScreen;
        if (screen != null && screen.ScreenNo > 0)
        {
            Label = $"Ô {OrderNo} ({screen.Name})";
        }
    }

    /// <summary>ID bản ghi cửa sổ kịch bản.</summary>
    public string? ID => Window.ID;

    /// <summary>ID cửa sổ trên thiết bị ISAPI.</summary>
    public string? DeviceWindowId
    {
        get => Window.DeviceWindowId;
        set
        {
            Window.DeviceWindowId = value;
            OnPropertyChanged();
        }
    }

    public int? ResolveDeviceWindowId()
    {
        if (int.TryParse(Window.DeviceWindowId, out var devId) && devId > 0)
            return devId;
        if (int.TryParse(Window.ID, out var id) && id > 0)
            return id;
        if (!string.IsNullOrWhiteSpace(Name))
        {
            var match = System.Text.RegularExpressions.Regex.Match(Name, @"\b(\d{7,10})\b");
            if (match.Success && int.TryParse(match.Value, out var matchId) && matchId > 0)
                return matchId;
        }
        return null;
    }

    /// <summary>Thứ tự hiển thị của cửa sổ.</summary>
    public int? OrderNo
    {
        get => Window.OrderNo;
        set
        {
            Window.OrderNo = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Nhãn hiển thị trên cửa sổ.</summary>
    public string? Label
    {
        get => Window.Label;
        set
        {
            Window.Label = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Tên cửa sổ kịch bản.</summary>
    public string? Name
    {
        get => Window.Name;
        set
        {
            Window.Name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>ID nguồn tín hiệu gán cho cửa sổ.</summary>
    public string? SourceId
    {
        get => Window.SourceId;
        set
        {
            Window.SourceId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Tọa độ hoành độ X của cửa sổ trên tường.</summary>
    public int? X
    {
        get => Window.X;
        set
        {
            Window.X = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedStartScreen));
        }
    }

    /// <summary>Tọa độ tung độ Y của cửa sổ trên tường.</summary>
    public int? Y
    {
        get => Window.Y;
        set
        {
            Window.Y = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedStartScreen));
        }
    }

    /// <summary>Chiều rộng của cửa sổ (pixel).</summary>
    public int? W
    {
        get => Window.W;
        set
        {
            Window.W = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSizePreset));
            OnPropertyChanged(nameof(SizeLabel));
        }
    }

    /// <summary>Chiều cao của cửa sổ (pixel).</summary>
    public int? H
    {
        get => Window.H;
        set
        {
            Window.H = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSizePreset));
            OnPropertyChanged(nameof(SizeLabel));
        }
    }

    /// <summary>Thứ tự lớp hiển thị (ZIndex) của cửa sổ.</summary>
    public int? ZIndex
    {
        get => Window.ZIndex;
        set
        {
            Window.ZIndex = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Hàm cung cấp danh sách màn hình khởi đầu khả dụng.</summary>
    public static Func<IReadOnlyList<StartScreenPreset>>? StartScreenProvider { get; set; }

    /// <summary>Danh sách các màn hình khởi đầu khả dụng.</summary>
    public static IReadOnlyList<StartScreenPreset> AvailableStartScreens => StartScreenProvider?.Invoke() ?? [];

    public void NotifyStartScreenChanged() => OnPropertyChanged(nameof(SelectedStartScreen));

    /// <summary>Màn hình bắt đầu được chọn cho vị trí cửa sổ.</summary>
    public StartScreenPreset? SelectedStartScreen
    {
        get
        {
            var screens = StartScreenProvider?.Invoke();
            if (screens != null && screens.Count > 0)
            {
                var curX = X ?? 0;
                var curY = Y ?? 0;

                var match = screens.FirstOrDefault(s => s.X == curX && s.Y == curY);
                if (match != null)
                    return match;

                return new StartScreenPreset($"Tùy chỉnh ({curX}, {curY})", curX, curY, 0, 0, 0);
            }

            return null;
        }
        set
        {
            if (value != null && value.ScreenNo > 0)
            {
                X = value.X;
                Y = value.Y;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Danh sách các mẫu kích thước cửa sổ chuẩn (Preset).</summary>
    public static IReadOnlyList<WindowSizePreset> AvailableSizePresets { get; } =
    [
        new("1x1", 1920, 1920),
        new("2x1", 3840, 1920),
        new("1x2", 1920, 3840),
        new("2x2", 3840, 3840),
        new("3x2", 5760, 3840),
        new("4x3", 7680, 5760),
    ];

    /// <summary>Nhãn mô tả kích thước cửa sổ hiển thị trên giao diện.</summary>
    public string SizeLabel
    {
        get
        {
            if (W == 1920 && (H == 1920 || H == 1080)) return "Toàn màn (1x1)";
            if (W == 3840 && (H == 3840 || H == 2160)) return "Khối lớn (2x2)";
            if (W == 1920 && (H == 3840 || H == 2160)) return "Màn dọc (1x2)";
            if (W == 3840 && (H == 1920 || H == 1080)) return "Màn ngang (2x1)";
            if (W == 5760 && (H == 3840 || H == 2160)) return "Khối 3x2 (6 Màn)";
            if (W == 7680 && (H == 5760 || H == 3240)) return "Toàn tường (4x3)";
            return $"{W} × {H}";
        }
    }

    /// <summary>Mẫu kích thước (Preset) đang chọn.</summary>
    public WindowSizePreset? SelectedSizePreset
    {
        get
        {
            var match = AvailableSizePresets.FirstOrDefault(p => p.W == W && p.H == H);
            if (match != null)
                return match;

            if (W == 1920 && (H == 1920 || H == 1080))
                return AvailableSizePresets[0];

            if (W == 3840 && (H == 1920 || H == 1080))
                return AvailableSizePresets[1];

            if (W == 1920 && (H == 3840 || H == 2160))
                return AvailableSizePresets[2];

            if (W == 3840 && (H == 3840 || H == 2160))
                return AvailableSizePresets[3];

            if (W == 5760 && (H == 3840 || H == 2160))
                return AvailableSizePresets[4];

            if (W == 7680 && (H == 5760 || H == 3240))
                return AvailableSizePresets[5];

            return null;
        }
        set
        {
            if (value != null)
            {
                W = value.W;
                H = value.H;
                OnPropertyChanged(nameof(SizeLabel));
                OnPropertyChanged();
            }
        }
    }

    private VwSourceDto? _selectedSource;
    /// <summary>Nguồn tín hiệu đang được chọn cho cửa sổ.</summary>
    public VwSourceDto? SelectedSource
    {
        get => _selectedSource;
        set
        {
            if (SetProperty(ref _selectedSource, value))
            {
                if (value != null)
                {
                    SourceId = value.ID;
                }
                if (SubCells.Count > 0 && SubCells[0].SelectedSource != value)
                {
                    SubCells[0].SelectedSource = value;
                }
            }
        }
    }

    /// <summary>Danh sách các chế độ chia cửa sổ con khả dụng (1, 4, 9, 16).</summary>
    public static IReadOnlyList<int> AvailableWindowModes { get; } = [1, 4, 9, 16];

    /// <summary>Chế độ chia cửa sổ (1: đơn, 4: 2x2, 9: 3x3, 16: 4x4).</summary>
    public int WindowMode
    {
        get => Window.WindowMode <= 0 ? 1 : Window.WindowMode;
        set
        {
            var normalized = value <= 0 ? 1 : value;
            if (Window.WindowMode != normalized)
            {
                Window.WindowMode = normalized;
                RebuildSubCells();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSubwindowMode));
            }
        }
    }

    /// <summary>Cho biết cửa sổ có đang ở chế độ chia nhỏ cửa sổ con hay không.</summary>
    public bool IsSubwindowMode => WindowMode > 1;

    /// <summary>Danh sách các ô cửa sổ con khi ở chế độ chia nhỏ.</summary>
    public ObservableCollection<SubWindowCell> SubCells { get; } = [];

    public void InitSubCells(IReadOnlyList<VwSourceDto>? sources)
    {
        var count = WindowMode <= 0 ? 1 : WindowMode;
        var existingSubs = Window.SubWindows ?? [];

        SubCells.Clear();
        for (var i = 1; i <= count; i++)
        {
            var foundDto = existingSubs.FirstOrDefault(s => s.SubIndex == i);
            var cell = new SubWindowCell(i, this);

            var sId = foundDto?.SourceId ?? (i == 1 ? Window.SourceId : null);
            if (!string.IsNullOrWhiteSpace(sId) && sources != null)
            {
                cell.SelectedSource = sources.FirstOrDefault(s => s.ID == sId || s.SignalNo.ToString() == sId);
            }
            cell.Label = foundDto?.Label ?? cell.SelectedSource?.Name;

            SubCells.Add(cell);
        }

        SyncSubWindowsToDto();
        if (SubCells.Count > 0 && SubCells[0].SelectedSource != null && SelectedSource == null)
        {
            SelectedSource = SubCells[0].SelectedSource;
        }
    }

    public void RebuildSubCells(IReadOnlyList<VwSourceDto>? sources = null)
    {
        var count = WindowMode <= 0 ? 1 : WindowMode;
        var existingCells = SubCells.ToList();

        SubCells.Clear();
        for (var i = 1; i <= count; i++)
        {
            var existing = existingCells.FirstOrDefault(c => c.Index == i);
            if (existing != null)
            {
                SubCells.Add(existing);
            }
            else
            {
                var newCell = new SubWindowCell(i, this);
                if (i == 1 && SelectedSource != null)
                {
                    newCell.SelectedSource = SelectedSource;
                }
                SubCells.Add(newCell);
            }
        }

        SyncSubWindowsToDto();
        OnPropertyChanged(nameof(SubCells));
    }

    public void SyncSubWindowsToDto()
    {
        Window.SubWindows = SubCells.Select(c => new VwSubWindowDto
        {
            SubIndex = c.Index,
            SourceId = c.SelectedSource?.ID,
            Label = c.Label ?? c.SelectedSource?.Name,
        }).ToList();
    }

    public void OnSubCellSourceChanged(SubWindowCell cell)
    {
        SyncSubWindowsToDto();
        if (cell.Index == 1 && SelectedSource != cell.SelectedSource)
        {
            _selectedSource = cell.SelectedSource;
            SourceId = cell.SelectedSource?.ID;
            OnPropertyChanged(nameof(SelectedSource));
        }
        OnPropertyChanged(nameof(SubCells));
    }

    public static implicit operator SceneWindowRow(VwWindowSceneDto dto) => new(dto);

    public static implicit operator VwWindowSceneDto(SceneWindowRow row) => row.Window;
}

/// <summary>
/// Description: Một ô cửa sổ con trong chế độ chia nhỏ (WindowMode &gt; 1).
/// Created date: 30/08/2026
/// </summary>
public sealed partial class SubWindowCell : ObservableObject
{
    public SubWindowCell(int index, SceneWindowRow parent)
    {
        Index = index;
        Parent = parent;
    }

    /// <summary>Cửa sổ cha chứa ô con này.</summary>
    public SceneWindowRow Parent { get; }

    /// <summary>Chỉ số thứ tự ô cửa sổ con (bắt đầu từ 1).</summary>
    public int Index { get; set; }

    [ObservableProperty]
    private VwSourceDto? _selectedSource;

    [ObservableProperty]
    private string? _label;

    /// <summary>Tiêu đề hiển thị của ô con (ví dụ: Ô 1).</summary>
    public string Title => $"Ô {Index}";

    partial void OnSelectedSourceChanged(VwSourceDto? value)
    {
        Parent.OnSubCellSourceChanged(this);
    }
}

/// <summary>
/// Description: Mẫu màn hình bắt đầu của cửa sổ (tên, toạ độ X, Y, số màn hình, hàng, cột)
/// Created date: 13/09/2026
/// </summary>
public sealed record StartScreenPreset(string Name, int X, int Y, int ScreenNo, int Row, int Col)
{
    public override string ToString() => Name;
}

/// <summary>
/// Description: Mẫu kích thước cửa sổ hiển thị định sẵn (tên, chiều rộng, chiều cao)
/// Created date: 13/09/2026
/// </summary>
public sealed record WindowSizePreset(string Name, int W, int H)
{
    public override string ToString() => Name;
}
