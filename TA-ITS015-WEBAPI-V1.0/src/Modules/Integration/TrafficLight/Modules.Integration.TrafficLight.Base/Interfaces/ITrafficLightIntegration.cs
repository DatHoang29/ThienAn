using Modules.Integration.TrafficLight.Base.Enums;
using Modules.Integration.TrafficLight.Base.Models;

namespace Modules.Integration.TrafficLight.Base.Interfaces;

/// <summary>
/// Giao diện chuẩn điều khiển Đèn tín hiệu giao thông (đa hãng).
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public interface ITrafficLightIntegration
{
    TrafficLightVendor Vendor { get; }

    /// <summary>Thiết lập màu đèn (Đỏ, Xanh, Vàng, Chớp, Tắt).</summary>
    Task<TrafficLightResult> SetColorAsync(TrafficLightConfig config, TrafficLightColor color, CancellationToken ct = default);

    /// <summary>Đọc trạng thái màu đèn hiện tại.</summary>
    Task<TrafficLightStatusResult> GetStatusAsync(TrafficLightConfig config, CancellationToken ct = default);

    /// <summary>Kiểm tra kết nối tới thiết bị.</summary>
    Task<bool> CheckConnectionAsync(TrafficLightConfig config, CancellationToken ct = default);
}
