using CommunityToolkit.Mvvm.ComponentModel;

namespace ITS.FA.Wpf.ViewModels;

/// <summary>Một dòng đầu báo, nút ấn hoặc cửa trên form. Author: ChuongLP. Created date: 02/11/2026.</summary>
public sealed partial class FaDeviceItem : ObservableObject
{
    private readonly Action<FaDeviceItem> _onTriggerChanged;

    /// <param name="isTriggered">Tín hiệu đang bật từ trước (khi nạp lại danh sách); gán thẳng, không gọi callback.</param>
    public FaDeviceItem(string id, int address, string kind, string? name, string position, Action<FaDeviceItem> onTriggerChanged, bool isTriggered = false)
    {
        Id = id;
        Address = address;
        Kind = kind;
        Name = name;
        Position = position;
        _onTriggerChanged = onTriggerChanged;
        _isTriggered = isTriggered;
    }

    public string Id { get; }
    public int Address { get; }
    public string Kind { get; }
    public string? Name { get; }
    public string Position { get; }

    /// <summary>Tín hiệu trên tủ giả do người test bật.</summary>
    [ObservableProperty]
    private bool _isTriggered;

    /// <summary>Trạng thái đọc lại từ DB, để đối chiếu với tín hiệu vừa bật.</summary>
    [ObservableProperty]
    private string _dbState = "-";

    partial void OnIsTriggeredChanged(bool value) => _onTriggerChanged(this);
}
