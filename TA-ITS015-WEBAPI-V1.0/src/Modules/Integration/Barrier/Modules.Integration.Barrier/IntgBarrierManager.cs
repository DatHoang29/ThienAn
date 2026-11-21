using Modules.Integration.Barrier.Base.Enums;
using Modules.Integration.Barrier.Base.Interfaces;
using Modules.Integration.Barrier.Base.Models;
using Modules.Integration.Barrier.Siemens;

namespace Modules.Integration.Barrier;

/// <summary>
/// Điều phối tích hợp Barrier: chọn driver theo <see cref="BarrierConfig.Vendor"/>.
/// Tương đương IntgVmsManager của VMS — điểm vào DUY NHẤT mà ITS.Barrier.Core / WPF / BE gọi.
///
/// Thread-safe: driver stateless, có thể dùng 1 instance manager cho nhiều lệnh song song.
/// Thêm hãng: tạo project Modules.Integration.Barrier.{Hãng}, thêm ProjectReference, đăng ký vào _drivers.
/// Author: ChuongLP
/// Date: 05-10-2026
/// </summary>
public class IntgBarrierManager
{
    private readonly IReadOnlyDictionary<BarrierVendor, IBarrierIntegration> _drivers;

    public IntgBarrierManager() : this(new IBarrierIntegration[]
    {
                new SiemensS71200BarrierDriver()
    })
    {
    }

    /// <summary>
    /// Cho phép truyền driver giả lập khi test.
    /// Author: ChuongLP
    /// Date: 05-10-2026
    /// </summary>
    public IntgBarrierManager(IEnumerable<IBarrierIntegration> drivers)
    {
        _drivers = drivers.ToDictionary(d => d.Vendor);
    }

    public IEnumerable<BarrierVendor> SupportedVendors => _drivers.Keys;

    /// <summary>
    /// Gửi lệnh barrier (Open/Close) tới thiết bị barrier theo config.
    /// Author: ChuongLP
    /// Date: 05-10-2026
    /// </summary>
    public Task<BarrierCommandResult> SendCommandAsync(BarrierConfig config, BarrierCommandType command, CancellationToken ct = default)
        => TryGetDriver(config, out var driver, out var error)
            ? driver.SendCommandAsync(config, command, ct)
            : Task.FromResult(new BarrierCommandResult { Success = false, Message = error });

    /// <summary>
    /// Gửi lệnh mở barrier tới thiết bị barrier theo config.
    /// <returns></returns>
    /// Author: ChuongLP
    /// Date: 05-10-2026
    /// <returns></returns>
    public Task<BarrierCommandResult> OpenAsync(BarrierConfig config, CancellationToken ct = default)
        => SendCommandAsync(config, BarrierCommandType.Open, ct);

    /// <summary>
    /// Gửi lệnh đóng barrier tới thiết bị barrier theo config.
    /// Author: ChuongLP
    /// Date: 05-10-2026
    /// </summary>
    public Task<BarrierCommandResult> CloseAsync(BarrierConfig config, CancellationToken ct = default)
        => SendCommandAsync(config, BarrierCommandType.Close, ct);

    /// <summary>
    /// Lấy trạng thái barrier từ thiết bị barrier theo config.
    /// Author: ChuongLP
    /// Date: 05-10-2026
    /// </summary>
    public Task<BarrierStatusResult> GetStatusAsync(BarrierConfig config, CancellationToken ct = default)
        => TryGetDriver(config, out var driver, out var error)
            ? driver.GetStatusAsync(config, ct)
            : Task.FromResult(new BarrierStatusResult { IsConnected = false, RawDetail = error });

    /// <summary>
    /// Kiểm tra kết nối tới thiết bị barrier theo config.
    /// Author: ChuongLP
    /// Date: 05-10-2026
    /// </summary>
    public Task<bool> CheckConnectionAsync(BarrierConfig config, CancellationToken ct = default)
        => TryGetDriver(config, out var driver, out _)
            ? driver.CheckConnectionAsync(config, ct)
            : Task.FromResult(false);

    /// <summary>
    /// Lấy driver theo config, trả về false nếu không tìm thấy driver.
    /// Author: ChuongLP
    /// Date: 05-10-2026
    /// </summary>
    private bool TryGetDriver(BarrierConfig config, out IBarrierIntegration driver, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(config.IpAddress))
        {
            driver = null!;
            error = "Thiếu địa chỉ IP thiết bị barrier";
            return false;
        }

        if (_drivers.TryGetValue(config.Vendor, out driver!))
            return true;

        error = $"Chưa hỗ trợ hãng barrier: {config.Vendor}";
        return false;
    }
}
