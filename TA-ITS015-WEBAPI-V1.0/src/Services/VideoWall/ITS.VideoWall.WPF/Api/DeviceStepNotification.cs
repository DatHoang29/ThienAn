using MediatR;
using Services.Shared.Events;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.Api;

/// <summary>
/// Author: Đạt
/// Description: Dòng log của một bước đấu nối thiết bị, mang kèm VwSetupSceneStep để hộp thoại
/// "Xem chi tiết" đọc được RequestXml/ResponseXml.
///
/// Vì sao không dùng ActivityNotification: record Activity chỉ có Stage/Detail/Level nên không
/// chỗ nào chở được XML thô. Thay vì sửa ActivityPublisher dùng chung, phát thêm một notification
/// riêng — MainViewModel xử lý cả hai và đổ vào cùng một danh sách log.
/// Created date: 25/08/2026
/// </summary>
public sealed record DeviceStepNotification(Activity Activity, VwSetupSceneStep Step) : INotification;
