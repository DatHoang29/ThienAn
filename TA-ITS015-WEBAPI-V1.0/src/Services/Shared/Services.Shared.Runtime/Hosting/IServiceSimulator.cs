namespace Services.Shared.Hosting;

/// <summary>
/// Author: TAC Team
/// Description: Contract cho chức năng giả lập (simulation) — tách b khỏi lifecycle <see cref="IServiceManager"/>.
/// Một service manager có thể implement cả 2 (qua partial class): IServiceManager cho Init/Start/Stop,
/// IServiceSimulator cho SinhDataGiả để test pipeline khi không có device/nguồn dữ liệu thật.
/// UI (WPF) inject interface này để expose nút Simulate mà không phụ thuộc concrete manager.
/// Created date: 20/07/2026
/// </summary>
public interface IServiceSimulator
{
    /// <summary>
    /// Sinh 1 batch dữ liệu giả lập và đẩy vào pipeline (queue + activity notification) — mô phỏng
    /// một chu kỳ thu thập device thật. Idempotent: gọi nhiều lần được, mỗi lần sinh batch mới.
    /// </summary>
    /// <param name="cancellationToken">Token hủy (vd. khi service shutdown giữa chừng).</param>
    Task SimulateBatchAsync(CancellationToken cancellationToken = default);
}
