using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Utility.Resilience;

namespace Module.VideoWall.Infrastructure.Services.Schedule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lúc app khởi động, dựng lại recurring job Hangfire cho toàn bộ VwSchedule
    ///              (mỗi lịch một job) và dọn job mồ côi. Chỉ đăng ký khi Hangfire bật —
    ///              cùng cách làm với CctvSyncDeviceJobRegistrar.
    /// Created date: 06/10/2026
    /// Update 06/10/2026: chuyển sang BackgroundService + chờ DbReadyGate (không chạm DB lúc StartAsync).
    /// </summary>
    internal sealed class VwScheduleJobRegistrar : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VwScheduleJobRegistrar> _logger;
        private readonly DbReadyGate _dbReadyGate;

        public VwScheduleJobRegistrar(IServiceScopeFactory scopeFactory, ILogger<VwScheduleJobRegistrar> logger, DbReadyGate dbReadyGate)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _dbReadyGate = dbReadyGate;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _dbReadyGate.WaitAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<VwScheduleJobService>();
                await service.SyncAllAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                // Không để lỗi đồng bộ lịch làm chết cả app
                _logger.LogError(ex, "Không đồng bộ được job Hangfire của lịch phát kịch bản VideoWall");
            }
        }
    }
}
