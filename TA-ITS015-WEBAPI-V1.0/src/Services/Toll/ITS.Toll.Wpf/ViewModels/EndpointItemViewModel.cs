using CommunityToolkit.Mvvm.ComponentModel;
using ITS.Toll.Core.Configuration;

namespace ITS.Toll.Wpf.ViewModels;

/// <summary>
/// Một dòng cấu hình API trên lưới (chỉnh động: Url, có FromDate/ToDate hay không, bật/tắt...),
/// kèm kết quả lần gọi gần nhất để theo dõi.
/// Author: ChuongLP
/// Created date: 24/08/2026
/// </summary>
public sealed partial class EndpointItemViewModel : ObservableObject
{
    /// <summary>Tạo dòng trống để người dùng nhập trực tiếp trên form.</summary>
    public EndpointItemViewModel() { }

    public EndpointItemViewModel(TollApiEndpoint endpoint)
    {
        Key = endpoint.Key;
        Name = endpoint.Name;
        Url = endpoint.Url;
        RequiresDateRange = endpoint.RequiresDateRange;
        DateFromParam = string.IsNullOrWhiteSpace(endpoint.DateFromParam) ? "FromDate" : endpoint.DateFromParam;
        DateToParam = string.IsNullOrWhiteSpace(endpoint.DateToParam) ? "ToDate" : endpoint.DateToParam;
        DataType = endpoint.DataType;
        Enabled = endpoint.Enabled;
    }

    [ObservableProperty] private string _key = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _url = string.Empty;
    [ObservableProperty] private bool _requiresDateRange;
    [ObservableProperty] private string _dateFromParam = "FromDate";
    [ObservableProperty] private string _dateToParam = "ToDate";
    [ObservableProperty] private string _dataType = string.Empty;
    [ObservableProperty] private bool _enabled = true;

    // ===== Kết quả lần gọi gần nhất (chỉ đọc trên lưới) =====
    [ObservableProperty] private string _lastStatus = "Chưa gọi";
    [ObservableProperty] private long _lastDurationMs;
    [ObservableProperty] private string _lastRecordCount = "-";
    [ObservableProperty] private string _lastCalledAt = "-";

    /// <summary>Chuyển ngược về model cấu hình để client dùng.</summary>
    public TollApiEndpoint ToEndpoint() => new()
    {
        Key = Key,
        Name = Name,
        Url = Url,
        RequiresDateRange = RequiresDateRange,
        DateFromParam = DateFromParam,
        DateToParam = DateToParam,
        DataType = DataType,
        Enabled = Enabled,
    };
}
