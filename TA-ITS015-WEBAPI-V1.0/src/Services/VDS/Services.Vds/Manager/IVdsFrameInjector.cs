namespace Services.Vds.Manager;

/// <summary>
/// Author: KienLT
/// Description: Cửa bơm MỘT frame JSON (đúng format VDS API v1) vào pipeline đang chạy — dành cho WPF
/// harness và test.
///
/// Vì sao tách riêng thay vì dùng <c>IServiceManager.SimulateBatchAsync</c> của khuôn: <c>SimulateBatchAsync()</c> không
/// nhận tham số nên chỉ sinh được batch mặc định, còn harness cần bơm bản tin do người dùng dựng
/// (chọn camera, loại sự kiện, tốc độ, biển số). Hai vai khác nhau nên để hai contract:
/// <list type="bullet">
/// <item><c>IServiceManager.SimulateBatchAsync</c> — "sinh một lượt dữ liệu giả", không tham số.</item>
/// <item><see cref="InjectAsync"/> — "bơm đúng bản tin này", có payload.</item>
/// </list>
/// Nhờ đó UI không phải phụ thuộc vào concrete manager.
/// Created date: 24/08/2026
/// </summary>
public interface IVdsFrameInjector
{
    /// <summary>
    /// Bơm một JSON sự kiện vào pipeline. Đi qua đúng chuỗi thành phần với bản tin SSE thật.
    /// Trả false nếu pipeline chưa chạy, payload rỗng, hoặc kênh đã đóng.
    /// </summary>
    Task<bool> InjectAsync(string rawFrame, CancellationToken ct = default);
}
