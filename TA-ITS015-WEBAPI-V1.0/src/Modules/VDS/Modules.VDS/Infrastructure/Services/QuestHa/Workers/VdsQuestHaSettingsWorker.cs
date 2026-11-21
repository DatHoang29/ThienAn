using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.QuestHa;

namespace Modules.VDS.Infrastructure.Services.QuestHa.Workers;

/// <summary>
/// Author: KienLT
/// Description: Lúc WebAPI khởi động, áp tham số router QuestDB HA từ <c>VdsConfig</c> của tenant sở hữu (<see cref="VdsQuestHaSettingsService.OwnerTenantId"/>).
/// DB chưa đọc được ⇒ giữ appsettings và thử lại mỗi phút tới khi đọc được. HA tắt ⇒ thoát ngay.
/// Created date: 31/10/2026
/// </summary>
public sealed class VdsQuestHaSettingsWorker(IServiceProvider services, ILogger<VdsQuestHaSettingsWorker> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (services.GetService<QuestHaManager>() is null) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<VdsQuestHaSettingsService>()
                    .ApplyRouterAsync(stoppingToken).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                // Chỉ tên kiểu lỗi: thông điệp lỗi SQL có thể chứa chuỗi kết nối.
                logger.LogWarning("[QuestHa] Chưa đọc được tham số router QuestDB HA từ VdsConfig ({ErrorType}); dùng appsettings, thử lại sau {Delay}.",
                    ex.GetType().Name, RetryDelay);
            }
            try { await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }
        }
    }
}
