using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Module.CCTV.Infrastructure.Services.Device;
using Module.Integration.NxVms.Extensions;
using Shared.Utility.Resilience;

namespace Module.CCTV.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCctvCore(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddCctvInfrastructure(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddNxVmsIntegration();
            services.AddAppLocalization();

            // Job Hangfire đồng bộ thiết bị CCTV — chỉ đăng ký khi Hangfire bật.
            // Đăng ký hosted service để RecurringJob.AddOrUpdate chạy lúc app khởi động
            // (JobStorage đã sẵn sàng), thay vì gọi trực tiếp ở pha ConfigureServices.
            if (configuration["Hangfire:Enable"]?.ToLower() == "true")
            {
                services.AddHostedService<CctvSyncDeviceJobRegistrar>();
            }

            return services;
        }

        public static IServiceCollection AddCctvValidation(this IServiceCollection services)
        {
            services.AddControllers().AddCctvValidation();
            return services;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Đăng ký job Hangfire đồng bộ thiết bị CCTV theo chu kỳ (cron lấy từ SysOpConfig,
        /// Created date: 2026-09-03
        /// </summary>
        /// Update 06/10/2026: chuyển sang BackgroundService + chờ DbReadyGate (không chạm DB lúc StartAsync).
        private sealed class CctvSyncDeviceJobRegistrar : BackgroundService
        {
            private const string JobId = "sync-cctv-devices";
            private const string DefaultCron = "*/60 * * * *"; // fallback nếu DB chưa cấu hình cron

            private readonly IServiceScopeFactory _scopeFactory;
            private readonly DbReadyGate _dbReadyGate;

            public CctvSyncDeviceJobRegistrar(IServiceScopeFactory scopeFactory, DbReadyGate dbReadyGate)
            {
                _scopeFactory = scopeFactory;
                _dbReadyGate = dbReadyGate;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                try { await _dbReadyGate.WaitAsync(stoppingToken); }
                catch (OperationCanceledException) { return; }

                // DeviceService là scoped -> phải tạo scope để resolve.
                using var scope = _scopeFactory.CreateScope();
                var deviceService = scope.ServiceProvider.GetRequiredService<DeviceService>();

                string cron;
                try { cron = await deviceService.GetSyncDeviceAsync(); }
                catch { cron = DefaultCron; }

                // Update 21/10/2026: job gọi CctvDeviceSyncService (có DisableConcurrentExecution + khóa chống chạy chồng với nút "Đồng bộ").
                RecurringJob.AddOrUpdate<CctvDeviceSyncService>(
                    recurringJobId: JobId,
                    methodCall: service => service.RunScheduledAsync(),
                    cronExpression: cron,
                    options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
            }
        }
    }
}
