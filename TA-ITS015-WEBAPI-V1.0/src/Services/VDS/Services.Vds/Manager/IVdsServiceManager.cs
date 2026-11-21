namespace Services.Vds.Manager;

/// <summary>
/// Author: KienLT
/// Description: Vòng đời của service VDS (Init/Start/Stop + State) — contract RIÊNG của VDS.
///
/// Vì sao KHÔNG dùng <c>Services.Shared.Hosting.IServiceManager</c>: contract chung ở
/// <c>Services.Shared.Runtime</c> do đội khác quản lý và đang thay đổi (từng gộp/tách phần giả lập).
/// VDS chỉ cần đúng bốn thành viên lifecycle dưới đây nên tự giữ contract để không bị kéo theo mỗi lần
/// contract chung đổi, và để nhánh VDS không phải sửa file dùng chung của người khác.
/// Created date: 18/09/2026
/// </summary>
public interface IVdsServiceManager
{
    /// <summary>Trạng thái hiện tại của service (xem <see cref="VdsServiceManagerState"/>).</summary>
    VdsServiceManagerState State { get; }

    /// <summary>Khởi tạo: nạp cấu hình, thiết lập hạ tầng và dịch vụ. Chưa chạy.</summary>
    /// <exception cref="InvalidOperationException">Ném khi cấu hình không hợp lệ.</exception>
    Task InitAsync(CancellationToken cancellationToken = default);

    /// <summary>Bắt đầu chạy toàn bộ dịch vụ đã khởi tạo. Idempotent khi đã Running.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Dừng dịch vụ, giải phóng tài nguyên. Idempotent khi đã Stopped.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sinh 1 batch dữ liệu giả lập và đẩy vào pipeline (mỗi camera một bản tin) — cho WPF harness/test
    /// khi chưa có nguồn dữ liệu thật. Idempotent: gọi nhiều lần được, mỗi lần sinh batch mới.
    /// </summary>
    Task SimulateBatchAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Author: KienLT
/// Description: Trạng thái vòng đời của <see cref="IVdsServiceManager"/>. Bốn giá trị khớp
/// <c>Services.Shared.Hosting.ServiceManagerState</c> để hành vi không đổi, nhưng tách ra để VDS
/// không phụ thuộc assembly dùng chung ở phần lifecycle.
/// Created date: 18/09/2026
/// </summary>
public enum VdsServiceManagerState
{
    Created,
    Initialized,
    Running,
    Stopped
}
