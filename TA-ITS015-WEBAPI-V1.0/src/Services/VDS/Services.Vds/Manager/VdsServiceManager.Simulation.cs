using Services.Vds.Shared.Testing;

namespace Services.Vds.Manager;

/// <summary>
/// Author: KienLT
/// Description: Phần giả lập của <see cref="VdsServiceManager"/> — bơm dữ liệu vào ĐÚNG hàng đợi mà
/// nguồn SSE dùng, không có đường tắt riêng, nên mọi bước parse, tra danh mục, ghi DB, chống trùng và
/// publish đều đi qua như bản tin thật.
///
/// <para><b>CẢNH BÁO: dữ liệu sinh ra được GHI THẬT</b> vào QuestDB và (nếu bật tính năng) vào các bảng
/// dùng chung trên SQL Server; checkpoint cũng tiến theo. Chỉ gọi khi chủ ý tạo dữ liệu test. Muốn dọn thì
/// xoá theo <c>create_uid = Service:ServiceUserId</c>.</para>
///
/// Hai contract khác vai, cố tình để riêng:
/// <list type="bullet">
/// <item><see cref="SimulateBatchAsync"/> — "sinh một lượt dữ liệu giả", không tham số, theo khuôn
/// <see cref="IServiceManager"/> dùng chung cho mọi service.</item>
/// <item><see cref="InjectAsync"/> — "bơm ĐÚNG bản tin này", có payload, cho harness WPF nơi người dùng
/// tự chọn camera / loại / tốc độ / biển số.</item>
/// </list>
/// Created date: 26/08/2026
/// </summary>
public sealed partial class VdsServiceManager : IVdsFrameInjector
{
    /// <summary>Số bản tin mỗi lượt <see cref="SimulateBatchAsync"/> bơm cho mỗi camera.</summary>
    private const int SimulatedFramesPerCamera = 1;

    public Task<bool> InjectAsync(string rawFrame, CancellationToken ct = default)
    {
        // Chỉ nhận khi pipeline đang chạy. Về mặt kỹ thuật hàng đợi Wolverine nhận được từ lúc host start,
        // nhưng bơm trước khi Init xong thì danh mục camera còn rỗng và mọi bản tin sẽ bị loại vì "camera
        // không có trong danh mục" — báo lỗi rõ ràng tốt hơn là im lặng bỏ.
        if (State != VdsServiceManagerState.Running)
        {
            _logger.LogWarning("[VDS] Chưa sẵn sàng (pipeline chưa chạy); bỏ qua inject.");
            return Task.FromResult(false);
        }

        return _dispatcher.DispatchAsync(rawFrame, isBackfill: false, ct);
    }

    /// <summary>
    /// Sinh một lượt bản tin lượt xe cho MỌI camera trong danh mục — tương đương "một chu kỳ thu thập".
    /// Luỹ đẳng theo nghĩa gọi nhiều lần được, mỗi lần sinh một batch mới.
    /// </summary>
    public async Task SimulateBatchAsync(CancellationToken cancellationToken = default)
    {
        if (State != VdsServiceManagerState.Running)
        {
            _logger.LogWarning("[VDS] Pipeline chưa chạy; bỏ qua lượt giả lập.");
            return;
        }

        var cameras = _cameras.All;
        if (cameras.Count == 0)
        {
            _logger.LogWarning("[VDS] Danh mục camera rỗng; không có gì để giả lập.");
            return;
        }

        var sent = 0;
        foreach (var camera in cameras)
        {
            for (var i = 0; i < SimulatedFramesPerCamera; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = VdsSampleEvents.Normal(camera.CameraIp, lane: camera.Lane ?? "1");
                if (await InjectAsync(json, cancellationToken).ConfigureAwait(false)) sent++;
            }
        }

        _logger.LogInformation(
            "[VDS] Giả lập {Sent}/{Total} bản tin cho {Cameras} camera.",
            sent, cameras.Count * SimulatedFramesPerCamera, cameras.Count);
    }
}
