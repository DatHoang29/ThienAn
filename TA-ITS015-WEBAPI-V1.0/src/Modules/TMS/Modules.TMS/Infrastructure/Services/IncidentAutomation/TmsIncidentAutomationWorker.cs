using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.TMS.Constants;

namespace Modules.TMS.Infrastructure.Services.IncidentAutomation
{
    /// <summary>
    /// Author: SonTH
    /// Description: Worker nền kích hoạt luồng tự động xử lý sự cố. Thay thế Hangfire cho nghiệp vụ này:
    ///              trạng thái và mốc thời gian đã bền vững trong TmsIncidentAutomationRun nên chỉ cần
    ///              quét DB theo chu kỳ ngắn để "đánh thức" các run tới hạn.
    ///
    ///              Ưu điểm so với Hangfire ở nghiệp vụ này:
    ///              - Một nguồn sự thật duy nhất (không phải đồng bộ Status trong DB với job ngoài).
    ///              - Hủy lịch chỉ cần đổi Status; không cần lưu/xóa job id.
    ///              - Kiểm soát được độ chính xác đếm ngược theo PollingIntervalSeconds.
    ///              - Kiêm luôn vai trò recovery: run bị treo do restart sẽ được nhặt lại ở nhịp quét kế tiếp.
    ///
    ///              An toàn khi chạy nhiều instance API: mọi chuyển trạng thái đều qua compare-and-set
    ///              (ClaimRunStatusAsync) nên chỉ một instance xử lý được mỗi run.
    /// Created date: 24/07/2026
    /// </summary>
    public class TmsIncidentAutomationWorker : BackgroundService
    {
        /// <summary>
        /// Chu kỳ quét. Đủ nhỏ để đếm ngược ở giao diện khớp thực tế, đủ lớn để không tạo tải DB đáng kể
        /// (mỗi nhịp là 2 truy vấn có index, thường trả về 0 dòng).
        /// </summary>
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Thời gian chờ trước khi bắt đầu quét, để hệ thống hoàn tất khởi động (DI, Wolverine, SqlSugar).
        /// </summary>
        private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Giới hạn số run xử lý trong một nhịp quét, tránh dồn tải khi có nhiều run cùng tới hạn.
        /// </summary>
        private const int BatchSize = 20;

        /// <summary>
        /// Author: SonTH - 24/07/2026
        /// Ngưỡng coi một run đang ở trạng thái chuyển tiếp là BỊ TREO. Các trạng thái này bình thường chỉ
        /// tồn tại vài giây, nên 5 phút là dư an toàn để không cưỡng chế đóng oan một run đang chạy chậm.
        /// </summary>
        private static readonly TimeSpan StuckThreshold = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Author: SonTH - 24/07/2026
        /// Chu kỳ quét run treo. Tách khỏi chu kỳ đếm ngược 2 giây vì đây là việc dọn dẹp, không cần
        /// độ chính xác cao; quét mỗi 2 giây chỉ tạo tải DB vô ích.
        /// </summary>
        private static readonly TimeSpan StuckScanInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Author: SonTH - 24/07/2026
        /// Chu kỳ dọn các lần chạy đã kết thúc. Chạy trong worker thay vì Hangfire vì đây là việc dọn dẹp
        /// đơn giản, không cần độ chính xác cron, và tránh phụ thuộc thứ tự khởi tạo JobStorage của Hangfire.
        /// Nhiều instance API cùng chạy cũng vô hại: câu xoá theo điều kiện nên chỉ dư chứ không sai.
        /// </summary>
        private static readonly TimeSpan PurgeScanInterval = TimeSpan.FromHours(24);

        /// <summary>
        /// Giới hạn số dòng xoá mỗi nhịp dọn, tránh khoá bảng lâu. Lớn hơn BatchSize vì đây chỉ là câu DELETE.
        /// </summary>
        private const int PurgeBatchSize = 500;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TmsIncidentAutomationWorker> _logger;

        /// <summary>Mốc lần quét run treo gần nhất, để giãn nhịp quét độc lập với vòng lặp chính.</summary>
        private DateTime _lastStuckScanAt = DateTime.MinValue;

        /// <summary>Mốc lần dọn run đã kết thúc gần nhất.</summary>
        private DateTime _lastPurgeAt = DateTime.MinValue;
        private string? _eventCursor;
        private string? _finishRecoveryCursor;

        public TmsIncidentAutomationWorker(IServiceScopeFactory scopeFactory, ILogger<TmsIncidentAutomationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// KienLT - 17/10/2026: chạy vòng quét trong ExecutionContext riêng (xem <c>IsolatedFlow</c>). Trước đây worker
        /// thừa hưởng chung connection SqlSugarScope với worker chụp ảnh/ảnh đăng ký VDS, nên run tự động lỗi
        /// "The connection is closed" ngay khi chuyển sự cố sang Đang xử lý (lúc worker chụp ảnh cũng chạy).
        /// </summary>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => Shared.Utility.Threading.IsolatedFlow.Run(() => RunLoopAsync(stoppingToken));

        private async Task RunLoopAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(StartupDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return; // App tắt trong lúc chờ khởi động
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    _eventCursor = await scope.ServiceProvider.GetRequiredService<TmsIncidentAutomationService>()
                        .ProcessRegisteredEventsAsync(BatchSize, _eventCursor, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { _logger.LogError(ex, "[EventLifecycle] Không quét được sự kiện tới hạn."); }
                try
                {
                    await ProcessDueRunsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break; // App đang tắt, thoát vòng lặp
                }
                catch (Exception ex)
                {
                    // Không để lỗi một nhịp quét làm chết worker; nhịp sau sẽ thử lại.
                    _logger.LogError(ex, IncidentAutomationMessageConst.Log.WorkerScanFailed);
                }

                try
                {
                    await RecoverStuckRunsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Tách try/catch riêng: lỗi khi dọn run treo không được làm gián đoạn việc đếm ngược.
                    _logger.LogError(ex, IncidentAutomationMessageConst.Log.WorkerStuckScanFailed);
                }

                try
                {
                    await PurgeFinishedRunsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, IncidentAutomationMessageConst.Log.WorkerPurgeFailed);
                }

                try
                {
                    await Task.Delay(PollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Một nhịp quét: tìm các run tới hạn rồi xử lý tuần tự từng run.
        ///              Mỗi run dùng scope DI riêng để service scoped (repository, cache, message bus)
        ///              có vòng đời đúng và lỗi của một run không ảnh hưởng run khác.
        /// Created date: 24/07/2026
        /// </summary>
        private async Task ProcessDueRunsAsync(CancellationToken stoppingToken)
        {
            List<string> executeRunIds;
            List<string> finishRunIds;

            using (var scope = _scopeFactory.CreateScope())
            {
                var automationService = scope.ServiceProvider.GetRequiredService<TmsIncidentAutomationService>();
                _finishRecoveryCursor = await automationService.RestoreFrozenFinishSchedulesAsync(
                    BatchSize, _finishRecoveryCursor, stoppingToken);
                (executeRunIds, finishRunIds) = await automationService.GetDueRunIdsAsync(BatchSize);
            }

            foreach (var runId in executeRunIds)
            {
                if (stoppingToken.IsCancellationRequested) return;
                await RunSafelyAsync(runId, isFinish: false);
            }

            foreach (var runId in finishRunIds)
            {
                if (stoppingToken.IsCancellationRequested) return;
                await RunSafelyAsync(runId, isFinish: true);
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Nhịp quét run bị treo, giãn theo StuckScanInterval nên không chạy mỗi 2 giây.
        ///
        ///              Cần thiết vì run treo ở trạng thái chuyển tiếp sẽ khoá sự cố khỏi tự động hóa
        ///              vĩnh viễn: worker không quét các trạng thái đó, TryScheduleWithReasonAsync lại coi
        ///              là "đang chạy" nên từ chối lập lịch mới, và job dọn cũng bỏ qua vì chưa kết thúc.
        /// Created date: 24/07/2026
        /// </summary>
        private async Task RecoverStuckRunsAsync(CancellationToken stoppingToken)
        {
            if (DateTime.Now - _lastStuckScanAt < StuckScanInterval)
                return;

            _lastStuckScanAt = DateTime.Now;

            if (stoppingToken.IsCancellationRequested)
                return;

            using var scope = _scopeFactory.CreateScope();
            var automationService = scope.ServiceProvider.GetRequiredService<TmsIncidentAutomationService>();

            try
            {
                var recovered = await automationService.RecoverStuckRunsAsync(StuckThreshold, BatchSize);
                if (recovered > 0)
                    _logger.LogWarning(IncidentAutomationMessageConst.Log.WorkerRecoveredFormat, recovered);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // KienLT - 16/10/2026: tách khỏi bước sửa bàn giao bên dưới để lỗi bước này không bỏ luôn bước kia.
                _logger.LogError(ex, IncidentAutomationMessageConst.Log.WorkerStuckScanFailed);
            }

            // KienLT - 16/10/2026: làm nốt bước trả công việc về Chờ của các run Failed bàn giao dở (lỗi DB, tiến trình
            // chết ngay sau khi claim Failed) - không có bước này sự cố lại nằm đó mà không tổ chức nào thấy.
            var repaired = await automationService.RepairPendingHandOversAsync(BatchSize);
            if (repaired > 0)
                _logger.LogWarning(IncidentAutomationMessageConst.Log.WorkerHandOverRepairedFormat, repaired);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Nhịp dọn các lần chạy đã kết thúc, giãn theo PurgeScanInterval (mỗi 24 giờ).
        ///              Số ngày giữ lại đọc từ SysOpConfig trong chính service, xem PurgeFinishedRunsAsync.
        /// Created date: 24/07/2026
        /// </summary>
        private async Task PurgeFinishedRunsAsync(CancellationToken stoppingToken)
        {
            if (DateTime.Now - _lastPurgeAt < PurgeScanInterval)
                return;

            _lastPurgeAt = DateTime.Now;

            if (stoppingToken.IsCancellationRequested)
                return;

            using var scope = _scopeFactory.CreateScope();
            var automationService = scope.ServiceProvider.GetRequiredService<TmsIncidentAutomationService>();

            await automationService.PurgeFinishedRunsAsync(PurgeBatchSize);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Xử lý một run trong scope riêng, cô lập lỗi để các run còn lại vẫn được xử lý.
        /// Created date: 24/07/2026
        /// </summary>
        private async Task RunSafelyAsync(string runId, bool isFinish)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var automationService = scope.ServiceProvider.GetRequiredService<TmsIncidentAutomationService>();

                if (isFinish)
                    await automationService.FinishAsync(runId);
                else
                    await automationService.ExecuteAsync(runId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    IncidentAutomationMessageConst.Log.WorkerRunFailedFormat,
                    runId,
                    isFinish);
            }
        }
    }
}
