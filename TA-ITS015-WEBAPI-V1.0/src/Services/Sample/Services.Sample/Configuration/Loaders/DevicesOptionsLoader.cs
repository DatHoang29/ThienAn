using Microsoft.Extensions.Configuration;
using Services.Sample.Configuration.Options;
using Services.Shared.Configuration;

namespace Services.Sample.Configuration.Loaders;

/// <summary>
/// Author: TAC Team
/// Description: Đọc cấu hình thu thập thiết bị (section "Service:Devices") sang typed DevicesOptions từ file.
/// Created date: 17/07/2026
/// </summary>
public sealed class DevicesOptionsLoader(IConfiguration configuration)
    : OptionsLoaderBase(configuration, "Service:Devices"), IConfigLoader<DevicesOptions>
{
    public DevicesOptions DefaultValue => BindSection<DevicesOptions>();

    public Task<DevicesOptions> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(BindSection<DevicesOptions>());
    }
}
