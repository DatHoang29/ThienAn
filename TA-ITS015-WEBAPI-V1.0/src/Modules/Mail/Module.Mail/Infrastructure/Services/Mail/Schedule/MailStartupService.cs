using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Shared.Utility.Resilience;

using Modules.Mail.Infrastructure.Services.Mail.Report;

namespace Modules.Mail.Infrastructure.Services.Mail.Schedule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Việc của phân hệ Mail lúc khởi động API. Lỗi chỉ ghi log — không chặn API khởi động.
    ///                1. Kiểm tra danh mục báo cáo trong code: mã nào khai ở RptReportCode.Catalog mà
    ///                   ReportExporter.BuildInput không dựng được (quên nhánh switch) thì ghi log lỗi ngay.
    ///                2. Đồng bộ Hangfire (khi Hangfire:Enable = true): tạo / cập nhật job "rpt-email-{ID}" cho mọi lịch
    ///                   đang bật, gỡ job thừa, đăng ký job dọn nhật ký "mail-log-cleanup".
    /// Created date: 02/10/2026
    /// Update 06/10/2026: chuyển sang BackgroundService; bước 2 (chạm DB) chờ DbReadyGate.
    /// </summary>
    public sealed class RptEmailStartupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RptEmailStartupService> _logger;
        private readonly DbReadyGate _dbReadyGate;

        public RptEmailStartupService(IServiceScopeFactory scopeFactory, ILogger<RptEmailStartupService> logger, DbReadyGate dbReadyGate)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _dbReadyGate = dbReadyGate;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var missing = ReportExporter.FindUnbuildable();
                if (missing.Count > 0)
                    _logger.LogError(
                        "Báo cáo gửi email khai trong RptReportCode.Catalog nhưng chưa có nhánh kết xuất ({Count}): {Codes}. " +
                        "Kiểm tra ReportExporter.BuildInput và {{Phân hệ}}Reports.Build.",
                        missing.Count, string.Join(", ", missing));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không kiểm tra được danh mục báo cáo gửi email");
            }

            try
            {
                await _dbReadyGate.WaitAsync(stoppingToken);

                // RptEmailJobScheduler là scoped (repository) -> phải tạo scope để resolve
                using var scope = _scopeFactory.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<RptEmailJobScheduler>();
                scheduler.RegisterLogCleanupJob();
                await scheduler.SyncAllAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không đồng bộ được job Hangfire gửi báo cáo lúc khởi động");
            }
        }
    }
}
