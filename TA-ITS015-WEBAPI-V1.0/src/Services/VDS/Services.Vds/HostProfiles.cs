using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services.Shared.Logging;

namespace Services.Vds;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình HOST (logging, chạy như Windows Service) tách khỏi cấu hình DỊCH VỤ
/// (<see cref="ServiceCollectionExtensions.AddVdsServices"/>).
///
/// Tách vì hai thứ này đổi theo hai lý do khác nhau: cùng một bộ service chạy được ở nhiều vỏ host (worker
/// nền, công cụ có giao diện, test), mà mỗi vỏ cần cách ghi log và cách đăng ký vòng đời riêng.
///
/// Log đi qua <see cref="LogManager"/> (ZLogger): console + MỘT FILE cho mỗi loại log, cấu hình ở
/// <c>Configuration/Logging.json</c>. Bên gọi phải nạp file đó vào cấu hình TRƯỚC khi gọi hàm ở đây.
/// Created date: 26/08/2026
/// </summary>
public static class HostProfiles
{
    /// <summary>Tên trong Windows Service Manager, theo quy ước <c>TA-ITS015-{Service}</c>.</summary>
    public const string WindowsServiceName = "TA-ITS015-VDS-Service";

    /// <summary>
    /// Profile worker nền: logging theo loại + đăng ký để chạy được như Windows Service.
    ///
    /// Chạy bằng <c>dotnet run</c> vẫn bình thường: <c>AddWindowsService</c> tự nhận biết không nằm trong
    /// Service Control Manager và không đổi hành vi.
    /// </summary>
    public static void ConfigureWorkerService(
        IHostApplicationBuilder builder, string serviceName = WindowsServiceName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        LogManager.ConfigureLogging(builder.Logging, builder.Configuration);
        builder.Services.AddWindowsService(options => options.ServiceName = serviceName);
    }

    /// <summary>
    /// Profile công cụ có giao diện: CHỈ logging, không đăng ký Windows Service.
    ///
    /// Lưu ý <see cref="LogManager.ConfigureLogging"/> gọi <c>ClearProviders()</c> — provider riêng của giao
    /// diện phải thêm SAU khi gọi hàm này, nếu không sẽ bị xoá.
    /// </summary>
    public static void ConfigureGuiHost(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        LogManager.ConfigureLogging(builder.Logging, builder.Configuration);
    }
}
