using Microsoft.Extensions.Configuration;
using Services.Sample.Configuration.Options;
using Services.Shared.Configuration;

namespace Services.Sample.Configuration.Loaders;

/// <summary>
/// Author: TAC Team
/// Description: Đọc cấu hình Api input source (section "Service:Api") sang typed ApiOptions từ file.
/// Created date: 17/07/2026
/// </summary>
public sealed class ApiOptionsLoader(IConfiguration configuration)
    : OptionsLoaderBase(configuration, "Service:Api"), IConfigLoader<ApiOptions>
{
    public ApiOptions DefaultValue => BindSection<ApiOptions>();

    public Task<ApiOptions> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(BindSection<ApiOptions>());
    }
}
