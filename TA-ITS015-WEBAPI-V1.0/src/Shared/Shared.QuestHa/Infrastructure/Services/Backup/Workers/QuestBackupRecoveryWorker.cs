using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Shared.QuestHa.Infrastructure.Services.Backup.Workers;

/// <summary>
/// Author: KienLT
/// Description: Phục hồi node backup từng dừng trước khi host bắt đầu replicator/lịch/IPC.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestBackupRecoveryWorker(IServiceProvider services, IOptions<QuestHaOptions> options) : IHostedService
{
    /// <summary>Chờ phục hồi hoàn tất trước khi các hosted service kế tiếp khởi chạy.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (config.WriteMode == QuestWriteModeEnum.Direct || config.Nodes.Count == 0 || !Directory.Exists(config.Backup.StateDirectory)) return;
        await services.GetRequiredService<QuestBackupService>().RecoverInterruptedAsync(cancellationToken);
    }

    /// <summary>Worker startup không giữ công việc nền.</summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
