using MediatR;
using Services.Shared.Events;

namespace Services.Vds.Wpf.Logging;

/// <summary>
/// Author: KienLT
/// Description: Handler MediatR DUY NHẤT của <see cref="ActivityNotification"/> trong harness: chuyển activity
/// do thành phần khác phát (vd <c>RegularSimulator</c> qua <c>ActivityPublisher</c>) vào CÙNG feed với log của
/// pipeline (<see cref="ActivityLoggerProvider"/>), nơi <c>MainViewModel</c> đã đăng ký nhận.
///
/// <para><b>Vì sao không để <c>MainViewModel</c> tự làm handler:</b> <c>RegisterServicesFromAssembly</c> của
/// MediatR 12 LUÔN đăng ký thêm mọi <c>INotificationHandler&lt;&gt;</c> tìm thấy dưới dạng TRANSIENT (kể cả khi
/// đã đăng ký singleton bằng tay). Mỗi lần simulator phát activity (10 lần/phút) DI dựng thêm một
/// <c>MainViewModel</c>; bản đó đăng ký sự kiện nạp danh mục nên không bao giờ được thu hồi ⇒ RAM tăng
/// ~1 GB/ngày, mỗi lượt nạp danh mục hàng nghìn bản cùng truy vấn DB ⇒ cạn pool SQL (đo trên SRV-ITS 28/10/2026).</para>
///
/// <para>Lớp này cũng bị MediatR dựng transient mỗi lần phát, nhưng chỉ giữ tham chiếu tới provider singleton
/// nên không tích tụ gì, và không chạm tới <c>MainViewModel</c> trong DI.</para>
/// Created date: 29/10/2026
/// </summary>
public sealed class ActivityNotificationForwarder(ActivityLoggerProvider feed)
    : INotificationHandler<ActivityNotification>
{
    /// <inheritdoc />
    public Task Handle(ActivityNotification notification, CancellationToken cancellationToken)
    {
        feed.Publish(notification.Activity);
        return Task.CompletedTask;
    }
}
