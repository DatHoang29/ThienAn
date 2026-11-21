using Services.Shared.Events;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.ViewModels;

/// <summary>
/// Author: Đạt
/// Description: Một dòng trên bảng log. Bọc Activity (record dùng chung, không sửa được) và
/// đính kèm VwSetupSceneStep khi dòng đó là một bước đấu nối — nhờ vậy hộp thoại "Xem chi tiết"
/// đọc được RequestXml/ResponseXml mà bảng chính không phải nhồi XML thô vào ô Chi tiết.
/// Created date: 25/08/2026
/// </summary>
public sealed class EventTriggerLogEntry(Activity activity, VwSetupSceneStep? step = null)
{
    /// <summary>Dữ liệu hoạt động chi tiết của dòng log.</summary>
    public Activity Activity { get; } = activity;

    /// <summary>Bước thiết lập tương ứng kèm XML request/response nếu có.</summary>
    public VwSetupSceneStep? Step { get; } = step;

    /// <summary>Thời điểm ghi log theo giờ địa phương.</summary>
    public DateTime TimestampLocal => Activity.TimestampLocal;

    /// <summary>Giai đoạn hoặc loại thao tác của sự kiện.</summary>
    public string Stage => Activity.Stage;

    /// <summary>Chi tiết nội dung sự kiện log.</summary>
    public string Detail => Activity.Detail;

    /// <summary>Mức độ nghiêm trọng của sự kiện (Info, Warning, Error...).</summary>
    public ActivityLevel Level => Activity.Level;

    /// <summary>Có payload để mở hộp thoại chi tiết hay không.</summary>
    public bool HasDetails => Step is not null;
}
