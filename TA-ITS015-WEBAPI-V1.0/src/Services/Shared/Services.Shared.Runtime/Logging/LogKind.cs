namespace Services.Shared.Logging;

/// <summary>
/// Author: TAC Team
/// Description: Bộ loại log (logger category name) dùng chung. Mỗi loại ứng với một file log riêng do
/// <see cref="KindFileLogProcessor"/> route theo category name. Thư mục + tên file cấu hình qua
/// <see cref="LoggingOptions"/> (Configuration/Logging.json section "Logging", field Kinds &gt; PathTemplate)
/// — không hardcode trong code. Lấy ý tưởng từ BaseConst.LoggerName.MainLog bên DeviceMonitor.
/// Thêm loại mới: thêm const ở dưới + thêm vào <see cref="All"/> + (tuỳ chọn) override PathTemplate trong
/// Logging.json cho kind đó.
/// Created date: 17/07/2026
/// </summary>
public static class LogKind
{
    /// <summary>Log chung: lifecycle service, thông tin vận hành.</summary>
    public const string Main = "Main";

    /// <summary>Lỗi / exception — route riêng ra error-*.log.</summary>
    public const string Error = "Error";

    /// <summary>Cấu hình (load/reload config, config loader).</summary>
    public const string Config = "Config";

    /// <summary>Data flow / persistence (worker, repository, notification handler).</summary>
    public const string Data = "Data";

    /// <summary>Thiết bị (codec, plugin loader, acquisition runner).</summary>
    public const string Device = "Device";

    /// <summary>Messaging (NATS source, transport).</summary>
    public const string Nats = "Nats";

    /// <summary>
    /// Toàn bộ kind mặc định — KindFileLogProcessor mở 1 file cho mỗi kind ở đây.
    /// Thêm kind mới: thêm const ở trên + thêm vào danh sách này.
    /// </summary>
    public static IReadOnlyCollection<string> All { get; } = new[] { Main, Error, Config, Data, Device, Nats };
}
