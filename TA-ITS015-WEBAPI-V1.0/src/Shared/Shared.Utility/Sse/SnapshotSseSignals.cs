
using Shared.Infrastructure.Sse;

namespace Shared.Utility.Sse;

/// <summary>
/// Author: KienLT
/// Description: Ba frame điều khiển mà vòng dùng chung phát ra ngoài snapshot dữ liệu.
///
/// <para>Khai thành đối tượng dùng lại được vì hub phát chúng cho NHIỀU kết nối; dựng mới mỗi lần là rác
/// không cần thiết. Chúng bất biến sau khi tạo (<c>SseFrame</c> có setter private).</para>
///
/// <para><b>Vì sao dùng khóa localization thô ở đây:</b> hub là singleton, không có request scope nên không
/// lấy được <c>IStringLocalizer</c> đúng ngôn ngữ của từng kết nối. Việc dịch để lớp gọi làm — xem
/// <c>SnapshotSse.Poll</c>, nơi còn đang trong request và đã chụp localizer sẵn.</para>
/// Created date: 22/09/2026
/// </summary>
internal static class SnapshotSseSignals
{
    /// <summary>Nhịp báo còn sống khi dữ liệu không đổi; client không cần xử lý gì.</summary>
    internal static readonly object Heartbeat = new HeartbeatMarker();

    /// <summary>Nguồn dữ liệu đang lỗi; hub sẽ tự thử lại theo backoff.</summary>
    internal static readonly object Unavailable = new UnavailableMarker();

    /// <summary>Không tái lập được danh tính nên vòng dừng — không được trả dữ liệu vượt phạm vi tenant.</summary>
    internal static readonly object Unauthorized = new UnauthorizedMarker();

    internal sealed record HeartbeatMarker;
    internal sealed record UnavailableMarker;
    internal sealed record UnauthorizedMarker;

    /// <summary>
    /// Đổi một giá trị từ hub thành frame để ghi ra HTTP.
    ///
    /// <para>Ba marker trên được đổi thành frame có tên sự kiện riêng. <b>Payload đã LÀ <c>SseFrame</c> thì đi
    /// qua nguyên vẹn</b> — đó là frame do handler tự dựng (xem <c>SSEDemoController.Custom</c>), mang
    /// <c>EventName</c>, <c>Code</c>, <c>IsLast</c>, <c>Wrapped</c> của riêng nó. Bọc thêm một lớp
    /// <c>SseFrame.Ok</c> sẽ đẩy frame cũ xuống làm <c>Data</c> và xóa sạch bốn thuộc tính đó — đúng lỗi đã
    /// tái hiện 22/09/2026: <c>event=done, code=204, isLast=true</c> biến thành <c>event='', code=200,
    /// isLast=false</c>.</para>
    /// <para>Mọi thứ còn lại là snapshot dữ liệu và được bọc bình thường.</para>
    /// </summary>
    internal static SseFrame ToFrame(object payload, string unavailableMessage, string unauthorizedMessage)
        => payload switch
        {
            HeartbeatMarker => SseFrame.Ok(new { alive = true, heartbeat = true }).WithEvent("heartbeat"),
            UnavailableMarker => SseFrame.Fail(unavailableMessage, 503).WithEvent("unavailable"),
            UnauthorizedMarker => SseFrame.Fail(unauthorizedMessage, 401).WithEvent("unauthorized").Last(),
            SseFrame handlerFrame => handlerFrame,
            _ => SseFrame.Ok(payload),
        };



}
