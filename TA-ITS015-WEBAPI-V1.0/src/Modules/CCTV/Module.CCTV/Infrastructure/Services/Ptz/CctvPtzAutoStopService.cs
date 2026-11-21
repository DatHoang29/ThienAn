using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.CCTV.Infrastructure.Services.Vendor;

namespace Module.CCTV.Infrastructure.Services.Ptz
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Theo dõi phiên điều khiển PTZ theo từng camera (singleton):
    /// - Mỗi lệnh PTZ mới gọi <see cref="Begin"/> → tăng "thế hệ" lệnh + hủy hẹn tự dừng cũ.
    /// - Nhích 1 phát: chỉ gửi Stop nếu vẫn là lệnh mới nhất (<see cref="IsLatest"/>),
    ///   tránh Stop của lệnh nhích chạy sau lệnh đè giữ làm camera dừng sớm.
    /// - Đè giữ: <see cref="ScheduleAutoStop"/> hẹn tự Stop sau ContinuousMoveAutoStopMs,
    ///   phòng khi FE mất mạng/đóng tab không gửi được lệnh Stop. FE giữ lâu thì gửi lại lệnh giữ định kỳ.
    /// Hẹn giờ nằm trong bộ nhớ instance nhận lệnh (đủ dùng vì Stop gửi thẳng NxVMS).
    /// Created date: 21/10/2026
    /// </summary>
    public sealed class CctvPtzAutoStopService : ISingleton
    {
        /// <summary>Thời gian tự dừng mặc định khi cấu hình không có/không hợp lệ.</summary>
        public const int DefaultAutoStopMs = 5000;

        private const int MinAutoStopMs = 1000;
        private const int MaxAutoStopMs = 10000;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CctvPtzAutoStopService> _logger;
        private readonly ConcurrentDictionary<string, Session> _sessions = new(StringComparer.OrdinalIgnoreCase);

        private sealed class Session
        {
            public long Generation;
            public CancellationTokenSource? AutoStop;
        }

        public CctvPtzAutoStopService(IServiceScopeFactory scopeFactory, ILogger<CctvPtzAutoStopService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Đánh dấu bắt đầu một lệnh PTZ mới trên camera: hủy hẹn tự dừng đang chờ.
        /// </summary>
        /// <returns>Thế hệ của lệnh, dùng cho <see cref="IsLatest"/> / <see cref="ScheduleAutoStop"/>.</returns>
        public long Begin(string deviceId)
        {
            var session = _sessions.GetOrAdd(deviceId, _ => new Session());
            lock (session)
            {
                CancelAutoStop(session);
                return ++session.Generation;
            }
        }

        /// <summary>true nếu chưa có lệnh PTZ nào mới hơn <paramref name="generation"/> trên camera.</summary>
        public bool IsLatest(string deviceId, long generation)
        {
            if (!_sessions.TryGetValue(deviceId, out var session))
                return false;

            lock (session)
                return session.Generation == generation;
        }

        /// <summary>
        /// Hẹn tự gửi Stop sau <paramref name="delayMs"/> nếu trong thời gian đó không có lệnh PTZ mới.
        /// </summary>
        public void ScheduleAutoStop(string deviceId, long generation, string? vendor, int delayMs)
        {
            var session = _sessions.GetOrAdd(deviceId, _ => new Session());
            CancellationTokenSource cts;
            lock (session)
            {
                // Đã có lệnh mới hơn (vd FE đã gửi Stop) → không hẹn nữa.
                if (session.Generation != generation)
                    return;

                CancelAutoStop(session);
                cts = session.AutoStop = new CancellationTokenSource();
            }

            _ = RunAutoStopAsync(deviceId, generation, vendor, delayMs, cts.Token);
        }

        /// <summary>Chuẩn hóa thời gian tự dừng từ cấu hình: ≤ 0 → mặc định, còn lại kẹp trong [1s, 10s].</summary>
        public static int ResolveAutoStopMs(int configuredMs) =>
            configuredMs > 0 ? Math.Clamp(configuredMs, MinAutoStopMs, MaxAutoStopMs) : DefaultAutoStopMs;

        private async Task RunAutoStopAsync(string deviceId, long generation, string? vendor, int delayMs, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!IsLatest(deviceId, generation))
                return;

            try
            {
                // Request gốc đã kết thúc → tạo scope mới để resolve service vendor (scoped).
                using var scope = _scopeFactory.CreateScope();
                var vendorService = scope.ServiceProvider.GetRequiredService<VendorService>();
                var service = await vendorService.GetAsync(vendor);
                await service.StopPtzAsync(deviceId, CancellationToken.None);

                _logger.LogInformation("CCTV PTZ tự dừng sau {DelayMs}ms không nhận lệnh mới: DeviceId={DeviceId}", delayMs, deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CCTV PTZ tự dừng thất bại: DeviceId={DeviceId}", deviceId);
            }
        }

        private static void CancelAutoStop(Session session)
        {
            if (session.AutoStop == null)
                return;

            session.AutoStop.Cancel();
            session.AutoStop.Dispose();
            session.AutoStop = null;
        }
    }
}
