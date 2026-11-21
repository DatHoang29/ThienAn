namespace Services.Shared.Hosting;

/// <summary>
/// Author: TAC Team
/// Description: Facade giao tiếp giữa host (Worker/WPF) và phần xử lý bên trong.
/// Host chỉ quan tâm Init/Start/Stop + State; chi tiết khởi tạo và chạy do triển khai
/// IServiceManager (vd SampleServiceManager) quyết định theo từng dự án.
/// Created date: 17/07/2026
/// </summary>
public interface IServiceManager
{
    /// <summary>Trạng thái hiện tại của manager (xem <see cref="ServiceManagerState"/>).</summary>
    ServiceManagerState State { get; }

    /// <summary>Khởi tạo: nạp cấu hình, thiết lập hạ tầng và dịch vụ. Chưa chạy.</summary>
    /// <exception cref="InvalidOperationException">Quay lại nếu cấu hình không hợp lệ.</exception>
    Task InitAsync(CancellationToken cancellationToken = default);

    /// <summary>Bắt đầu chạy toàn bộ dịch vụ đã khởi tạo. Idempotent khi đã Running.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Dừng dịch vụ, giải phóng tài nguyên. Idempotent khi đã Stopped.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
