using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Storage;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Description: ViewModel quản lý danh sách và cấu hình nguồn tín hiệu (Sources) cục bộ cho thiết bị
/// Created date: 13/09/2026
/// </summary>
public sealed partial class SourceManagementViewModel : ObservableObject
{
    private readonly string _deviceKey;
    private readonly List<VwSourceDto> _probeSources;
    private readonly string? _baseDirectory;

    public SourceManagementViewModel(string deviceKey, IEnumerable<VwSourceDto>? probeSources, string? baseDirectory = null)
    {
        _deviceKey = string.IsNullOrWhiteSpace(deviceKey) ? "default" : deviceKey;
        _probeSources = probeSources?.Where(s => s.SourceType == "local_signal" || s.SignalNo.HasValue).ToList() ?? [];
        _baseDirectory = baseDirectory;
        RefreshList();
    }

    /// <summary>Danh sách nguồn tín hiệu (VwSourceDto).</summary>
    public ObservableCollection<VwSourceDto> Sources { get; } = [];

    /// <summary>Nguồn tín hiệu đang được chọn để xem hoặc chỉnh sửa.</summary>
    [ObservableProperty]
    private VwSourceDto? _selectedSource;

    /// <summary>Mã định danh ID của nguồn tín hiệu trong form.</summary>
    [ObservableProperty]
    private string _formId = string.Empty;

    /// <summary>Mã code của nguồn tín hiệu trong form.</summary>
    [ObservableProperty]
    private string _formCode = string.Empty;

    /// <summary>Tên hiển thị của nguồn tín hiệu trong form.</summary>
    [ObservableProperty]
    private string _formName = string.Empty;

    /// <summary>Loại tín hiệu truyền dẫn (RTSP, ONVIF, HIK_SDK...).</summary>
    [ObservableProperty]
    private string _formSignalType = "RTSP";

    /// <summary>Địa chỉ URL kết nối của nguồn phát stream.</summary>
    [ObservableProperty]
    private string _formUrl = string.Empty;

    /// <summary>Tỷ lệ khung hình hiển thị (16:9, 4:3...).</summary>
    [ObservableProperty]
    private string _formAspectRatio = "16:9";

    /// <summary>Ghi chú bổ sung cho nguồn tín hiệu.</summary>
    [ObservableProperty]
    private string _formRemark = string.Empty;

    /// <summary>Thông báo lỗi khi thao tác trên nguồn tín hiệu.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Thông báo trạng thái thực thi thành công.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Danh sách các loại tín hiệu luồng stream được hỗ trợ.</summary>
    public static IReadOnlyList<string> AvailableSignalTypes { get; } =
        ["RTSP", "ONVIF", "HIK_SDK", "HTTP_STREAM"];

    /// <summary>Danh sách các tỷ lệ khung hình màn hình được hỗ trợ.</summary>
    public static IReadOnlyList<string> AvailableAspectRatios { get; } =
        ["16:9", "4:3"];

    partial void OnSelectedSourceChanged(VwSourceDto? value)
    {
        if (value == null)
            return;

        if (value.SourceType == "ip_stream" || !string.IsNullOrWhiteSpace(value.Url))
        {
            FormId = value.ID ?? string.Empty;
            FormCode = value.Code ?? string.Empty;
            FormName = value.Name ?? string.Empty;
            FormSignalType = string.IsNullOrWhiteSpace(value.SignalType) ? "RTSP" : value.SignalType;
            FormUrl = value.Url ?? string.Empty;
            FormAspectRatio = string.IsNullOrWhiteSpace(value.AspectRatio) ? "16:9" : value.AspectRatio;
            FormRemark = value.Remark ?? string.Empty;
            ErrorMessage = null;
        }
    }

    [RelayCommand]
    public void ResetForm()
    {
        FormId = string.Empty;
        FormCode = string.Empty;
        FormName = string.Empty;
        FormSignalType = "RTSP";
        FormUrl = string.Empty;
        FormAspectRatio = "16:9";
        FormRemark = string.Empty;
        ErrorMessage = null;
        SelectedSource = null;
    }

    [RelayCommand]
    public void SaveSource()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(FormCode))
        {
            ErrorMessage = "Vui lòng nhập Mã nguồn (Code).";
            return;
        }

        if (string.IsNullOrWhiteSpace(FormName))
        {
            ErrorMessage = "Vui lòng nhập Tên nguồn (Name).";
            return;
        }

        if (string.IsNullOrWhiteSpace(FormUrl))
        {
            ErrorMessage = "Vui lòng nhập địa chỉ URL luồng IP Stream.";
            return;
        }

        if (FormUrl.Trim().Length > 256)
        {
            ErrorMessage = "Địa chỉ URL không được vượt quá 256 ký tự (chuẩn hệ thống).";
            return;
        }

        var id = string.IsNullOrWhiteSpace(FormId) ? Guid.NewGuid().ToString("N") : FormId;
        var dto = new VwSourceDto
        {
            ID = id,
            Code = FormCode.Trim(),
            Name = FormName.Trim(),
            SourceType = "ip_stream",
            SignalType = FormSignalType,
            Url = FormUrl.Trim(),
            AspectRatio = FormAspectRatio,
            Remark = string.IsNullOrWhiteSpace(FormRemark) ? null : FormRemark.Trim(),
            Status = VwStatus.Enable,
            OrderNo = 50,
        };

        VwLocalSourceStore.SaveSource(_deviceKey, dto, _baseDirectory);
        StatusMessage = $"Đã lưu nguồn '{dto.Name}' thành công.";
        RefreshList();
        ResetForm();
    }

    [RelayCommand]
    public void DeleteSource(VwSourceDto? source)
    {
        var target = source ?? SelectedSource;
        if (target == null || string.IsNullOrWhiteSpace(target.ID))
            return;

        if (target.SourceType == "local_signal" && target.SignalNo.HasValue)
        {
            ErrorMessage = "Không thể xoá kênh tín hiệu HDMI vật lý từ phần cứng thiết bị.";
            return;
        }

        VwLocalSourceStore.DeleteSource(_deviceKey, target.ID, _baseDirectory);
        StatusMessage = $"Đã xoá nguồn '{target.Name ?? target.Code}'.";
        RefreshList();
        if (SelectedSource?.ID == target.ID)
            ResetForm();
    }

    public void RefreshList()
    {
        var custom = VwLocalSourceStore.Load(_deviceKey, _baseDirectory);
        var merged = VwLocalSourceStore.Merge(_probeSources, custom);
        Sources.Clear();
        foreach (var s in merged)
        {
            Sources.Add(s);
        }
    }
}