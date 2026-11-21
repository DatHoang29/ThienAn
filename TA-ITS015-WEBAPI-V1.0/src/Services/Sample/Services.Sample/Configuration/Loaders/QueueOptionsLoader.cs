using Microsoft.Extensions.Configuration;
using Services.Sample.Configuration.Options;
using Services.Shared.Configuration;

namespace Services.Sample.Configuration.Loaders;

/// <summary>
/// Author: TAC Team
/// Description: Đọc cấu hình Queue (section "Service:Queue") sang typed QueueOptions từ file.
/// Created date: 17/07/2026
/// </summary>
public sealed class QueueOptionsLoader(IConfiguration configuration)
    : OptionsLoaderBase(configuration, "Service:Queue"), IConfigLoader<QueueOptions>
{
    public QueueOptions DefaultValue => BindSection<QueueOptions>();

    public Task<QueueOptions> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(BindSection<QueueOptions>());
    }
}
