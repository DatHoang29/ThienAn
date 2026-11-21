namespace Services.Vds.Manager;

/// <summary>
/// Author: KienLT
/// Description: Nguồn bản tin VDS, do <see cref="VdsServiceManager"/> điều khiển.
///
/// <para><b>Vì sao cần trừu tượng này thay vì để nguồn tự là <c>BackgroundService</c>:</b> nguồn phải
/// đóng TRƯỚC khi host drain hàng đợi Wolverine, nếu không thì lúc dừng vẫn còn bản tin mới chảy vào một
/// hàng đợi đang đóng. <c>BackgroundService</c> dừng theo host nên không đảm bảo được thứ tự đó — thứ tự
/// dừng chỉ đúng khi có một chỗ điều phối, và chỗ đó là manager.</para>
///
/// Nguồn cũng KHÔNG được tự chạy khi host start: danh mục camera phải nạp xong trước bản tin đầu tiên,
/// nếu không mọi frame đầu tiên bị bỏ vì "camera không có trong cache".
/// Created date: 26/08/2026
/// </summary>
public interface IVdsEventSource
{
    /// <summary>
    /// Chạy vòng đọc nguồn tới khi <paramref name="stoppingToken"/> bị huỷ.
    ///
    /// Hợp đồng: KHÔNG ném khi bị huỷ theo yêu cầu, và tự thử lại khi mất kết nối — manager chỉ gọi một
    /// lần rồi để nguồn tự lo phần bền bỉ.
    /// </summary>
    Task RunAsync(CancellationToken stoppingToken);
}
