namespace Services.Shared.Hosting;

/// <summary>
/// Author: TAC Team
/// Description: Vòng đời của IServiceManager. Chuyển trạng thái một chiều:
/// Created -&gt; Initialized -&gt; Running -&gt; Stopped.
/// Created date: 17/07/2026
/// </summary>
public enum ServiceManagerState
{
    /// <summary>Mới tạo, chưa InitAsync.</summary>
    Created,

    /// <summary>Đã InitAsync, sẵn sàng StartAsync.</summary>
    Initialized,

    /// <summary>Đang chạy sau StartAsync.</summary>
    Running,

    /// <summary>Đã StopAsync hoặc disposed.</summary>
    Stopped,
}
