using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ITS.Sync.Wpf.ViewModels;

/// <summary>
/// Một dòng trên lưới = một bảng đồng bộ (một strategy).
/// Hiển thị trạng thái và số bản ghi thêm/sửa/xóa/không đổi của lần chạy gần nhất.
/// </summary>
public sealed class StrategyItemViewModel : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    /// <summary>Có đưa bảng này vào chu kỳ đồng bộ hay không (checkbox).</summary>
    public bool IsEnabled { get => _isEnabled; set => Set(ref _isEnabled, value); }

    private string _tableName = string.Empty;
    /// <summary>Tên bảng đồng bộ, ví dụ "TmsEquipment".</summary>
    public string TableName { get => _tableName; set => Set(ref _tableName, value); }

    private string _status = "Chưa chạy";
    /// <summary>Trạng thái lần chạy gần nhất: Chưa chạy / Đang chạy / Thành công / Lỗi / Bỏ qua.</summary>
    public string Status { get => _status; set => Set(ref _status, value); }

    private int _inserted;
    public int Inserted { get => _inserted; set => Set(ref _inserted, value); }

    private int _updated;
    public int Updated { get => _updated; set => Set(ref _updated, value); }

    private int _deleted;
    public int Deleted { get => _deleted; set => Set(ref _deleted, value); }

    private int _unchanged;
    public int Unchanged { get => _unchanged; set => Set(ref _unchanged, value); }

    private string _lastRunAt = "-";
    /// <summary>Thời điểm chạy gần nhất.</summary>
    public string LastRunAt { get => _lastRunAt; set => Set(ref _lastRunAt, value); }

    /// <summary>Log chi tiết RIÊNG của bảng này (chi tiết thêm/sửa/xóa của lần chạy gần nhất).</summary>
    public ObservableCollection<string> Logs { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
