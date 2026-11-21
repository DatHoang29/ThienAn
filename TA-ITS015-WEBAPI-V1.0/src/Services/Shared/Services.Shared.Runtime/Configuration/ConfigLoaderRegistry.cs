using Microsoft.Extensions.DependencyInjection;

namespace Services.Shared.Configuration;

/// <summary>
/// Author: TAC Team
/// Description: Resolve IConfigLoader&lt;TConfig&gt; từ DI. Mỗi loader tự lo phần đọc bảng và mapping
/// của nó; registry chỉ điều phối + bắt lỗi (loader lỗi → trả DefaultValue, không làm sập reload).
/// Created date: 17/07/2026
/// </summary>
internal sealed class ConfigLoaderRegistry(IServiceProvider serviceProvider) : IConfigLoaderRegistry
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<TConfig> LoadAsync<TConfig>(CancellationToken cancellationToken)
        where TConfig : class, new()
    {
        var loader = _serviceProvider.GetService<IConfigLoader<TConfig>>();
        if (loader is null)
            return new TConfig();

        try
        {
            return await loader.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Loader lỗi (DB mất kết nối, bảng thiếu...) → dùng default, giữ reload tiếp tục cho các section khác.
            return loader.DefaultValue;
        }
    }
}
