namespace TA_ShareData_WorkerService.Infrastructure.Services
{
    /// <summary>
    /// Interface dịch vụ kết xuất dữ liệu cho ShareData Worker
    /// Author: Đạt
    /// Created date: 31/07/2026
    /// </summary>
    public interface IShareDataExportService
    {
        Task ProcessBatchSubscriptionsAsync(CancellationToken stoppingToken = default);
    }
}
