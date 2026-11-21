namespace Services.Sample.Configuration.Options;

/// <summary>
/// Author: TAC Team
/// Description: Cấu hình runtime riêng của Sample service (section "Service" trong appsettings).
/// NATS config nằm riêng ở DataTransporter.json (section "Nats"), không thuộc Service section.
/// Created date: 16/07/2026
/// </summary>
public sealed class ServiceConfig
{
    public string Name { get; set; } = "Sample";

    public string DataConnectionId { get; set; } = "Default";

    public int ConfigReloadSeconds { get; set; } = 60;

    public QueueOptions Queue { get; set; } = new();

    public ApiOptions Api { get; set; } = new();

    public DevicesOptions Devices { get; set; } = new();

    /// <summary>Subject NATS nhận input (kết nối + streams lo bởi TransportManager qua DataTransporter.json).</summary>
    public string NatsInputSubject { get; set; } = string.Empty;

    /// <summary>Subject NATS publish output sau khi xử lý.</summary>
    public string NatsOutputSubject { get; set; } = string.Empty;
}
