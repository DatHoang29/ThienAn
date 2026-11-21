using CommunityToolkit.Mvvm.ComponentModel;
using Services.Vds.Catalog;

namespace Services.Vds.Wpf.ViewModels;

/// <summary>
/// Author: KienLT
/// Description: Một camera trong ComboBox của tab sự kiện. Bất biến — chỉ để chọn, không có trạng thái.
/// Created date: 04/08/2026
/// </summary>
public sealed record CameraOption(string Ip, string Display, string DirectionName, CameraInfo Info);

/// <summary>
/// Author: KienLT
/// Description: Một camera có ô tích trong hai danh sách của tab mô phỏng.
///
/// Là <c>ObservableObject</c> (không phải POCO như bản cũ) nên "Chọn tất cả / Bỏ chọn" chỉ cần đổi
/// <see cref="IsChecked"/> là UI tự cập nhật — bản cũ phải gọi
/// <c>(list.ItemsSource as ICollectionView)?.Refresh()</c> từ code-behind vì POCO không phát INPC.
/// Created date: 04/08/2026
/// </summary>
public sealed partial class CameraCheckItem : ObservableObject
{
    public required string Ip { get; init; }

    public required string EquipmentId { get; init; }

    public required string Display { get; init; }

    /// <summary><c>TmsEquipment.DirectionId</c> — khoá để tra camera nhân bản số đếm của hướng.</summary>
    public string? DirectionId { get; init; }

    /// <summary>Tên hướng tuyến — dùng làm tiêu đề nhóm trên UI.</summary>
    public required string DirectionName { get; init; }

    [ObservableProperty]
    private bool _isChecked;
}

/// <summary>
/// Author: KienLT
/// Description: Một hướng tuyến + camera được chọn làm ĐÍCH NHÂN BẢN số đếm của hướng đó.
///
/// Bơm một lượt xe ở bất kỳ camera nào thuộc hướng này thì bơm thêm một lượt y hệt cho
/// <see cref="Selected"/>, nhờ đó camera đó mang số đếm của cả hướng. Không chọn ⇒ hướng đó không nhân bản.
/// Created date: 08/08/2026
/// </summary>
public sealed partial class DirectionCountOption : ObservableObject
{
    public required string DirectionId { get; init; }

    public required string DirectionName { get; init; }

    /// <summary>Các camera thuộc hướng này — nguồn của ComboBox.</summary>
    public required IReadOnlyList<CameraOption> Cameras { get; init; }

    [ObservableProperty]
    private CameraOption? _selected;
}
