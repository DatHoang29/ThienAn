using Modules.Integration.TrafficLight.Base.Enums;
using Modules.Integration.TrafficLight.Base.Interfaces;
using Modules.Integration.TrafficLight.Base.Models;
using Modules.Integration.TrafficLight.Modbus;
using Modules.Integration.TrafficLight.Siemens;

namespace Modules.Integration.TrafficLight;

/// <summary>
/// Bộ điều phối thiết bị Đèn tín hiệu giao thông đa hãng (Multi-vendor Manager).
/// Quản lý driver Siemens S7-1200 và Modbus TCP độc lập không phụ thuộc Furion.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class IntgTrafficLightManager
{
    private readonly Dictionary<TrafficLightVendor, ITrafficLightIntegration> _drivers = new();

    public IntgTrafficLightManager()
    {
        RegisterDriver(new SiemensS71200TrafficLightDriver());
        RegisterDriver(new ModbusTcpTrafficLightDriver());
    }

    public void RegisterDriver(ITrafficLightIntegration driver)
    {
        _drivers[driver.Vendor] = driver;
    }

    public ITrafficLightIntegration GetDriver(TrafficLightVendor vendor)
    {
        if (_drivers.TryGetValue(vendor, out var driver))
        {
            return driver;
        }

        throw new NotSupportedException($"Không tìm thấy driver cho hãng đèn: {vendor}");
    }

    public async Task<TrafficLightResult> SetColorAsync(TrafficLightVendor vendor, TrafficLightConfig config, TrafficLightColor color, CancellationToken ct = default)
    {
        var driver = GetDriver(vendor);
        return await driver.SetColorAsync(config, color, ct).ConfigureAwait(false);
    }

    public async Task<TrafficLightStatusResult> GetStatusAsync(TrafficLightVendor vendor, TrafficLightConfig config, CancellationToken ct = default)
    {
        var driver = GetDriver(vendor);
        return await driver.GetStatusAsync(config, ct).ConfigureAwait(false);
    }

    public async Task<bool> CheckConnectionAsync(TrafficLightVendor vendor, TrafficLightConfig config, CancellationToken ct = default)
    {
        var driver = GetDriver(vendor);
        return await driver.CheckConnectionAsync(config, ct).ConfigureAwait(false);
    }
}
