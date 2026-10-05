using System.Text.Json;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Scheduling;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Bộ kiểm thử chuyên biệt cho DataOutboundScheduler — kiểm chứng nguyên nhân gốc rễ
    ///              lệch múi giờ giữa đồng hồ DB (UTC) và cấu hình lịch gửi địa phương (Việt Nam UTC+7).
    /// Created date: 05/10/2026
    /// </summary>
    public class DataOutboundSchedulerTests
    {
        private const string VietnamStandardScheduleJson = "{\"startTime\": \"08:00\", \"endTime\": \"17:00\"}";

        #region 1. Kiểm thử ca lỗi gốc rễ: Máy chủ DB chạy UTC trong khi người dùng cấu hình giờ Việt Nam

        /// <summary>
        /// Description: Kiểm thử case gốc rễ lỗi gói 101: DB clock chạy UTC lúc 01:26:28 (tương ứng 08:26:28 sáng giờ VN).
        ///              Với cấu hình [08:00 - 17:00], thời điểm này hoàn toàn hợp lệ trong giờ làm việc.
        ///              - Lỗi cũ: Code cũ so sánh 01:26:28 < 08:00:00 -> tưởng chưa đến giờ -> kẹp NextTimeRun về 08:00:00 UTC (15h chiều VN),
        ///                        dẫn đến gói 101 chạy 1 lần rồi ngừng gửi suốt 7 tiếng.
        ///              - Sau khi sửa: Hệ thống quy đổi múi giờ, nhận biết 08:26:28 VN nằm trong khung giờ -> NextTimeRun = now + 30s (01:26:58 UTC),
        ///                        đảm bảo gói tin được gửi liên tục mỗi 30 giây.
        /// Created date: 05/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_UtcServerClock_WithinLocalWorkingHours_SchedulesNextIntervalWithoutClamping_Test()
        {
            // Arrange: 01:26:28 UTC tương ứng 08:26:28 sáng tại Việt Nam (UTC+7)
            var nowUtc = new DateTime(2026, 10, 5, 1, 26, 28, DateTimeKind.Utc);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = VietnamStandardScheduleJson
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowUtc);

            // Assert: Lần chạy tiếp theo phải là now + 30 giây (01:26:58 UTC), KHÔNG BỊ kẹp về 08:00:00 UTC
            var expectedNextRun = new DateTime(2026, 10, 5, 1, 26, 58, DateTimeKind.Utc);
            Assert.Equal(expectedNextRun, nextRun);
        }

        /// <summary>
        /// Description: Kiểm tra khi thời điểm UTC thực tế trước giờ làm việc tại VN (ví dụ: 00:00:00 UTC = 07:00:00 sáng VN).
        ///              Hệ thống phải kẹp lần chạy đến đúng 08:00:00 sáng VN hôm nay, tức 01:00:00 UTC.
        /// Created date: 05/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_UtcServerClock_BeforeLocalStartTime_ClampsToStartTimeTodayConvertedToUtc_Test()
        {
            // Arrange: 00:00:00 UTC = 07:00:00 sáng VN (trước giờ bắt đầu 08:00)
            var nowUtc = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = VietnamStandardScheduleJson
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowUtc);

            // Assert: Phải kẹp đến 08:00:00 sáng VN = 01:00:00 UTC cùng ngày
            var expectedNextRun = new DateTime(2026, 10, 5, 1, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expectedNextRun, nextRun);
        }

        /// <summary>
        /// Description: Kiểm tra khi thời điểm UTC thực tế sau giờ làm việc tại VN (ví dụ: 16:00:00 UTC = 23:00:00 đêm VN).
        ///              Hệ thống phải kẹp lần chạy sang 08:00:00 sáng ngày hôm sau theo giờ VN, tức 01:00:00 UTC ngày hôm sau.
        /// Created date: 05/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_UtcServerClock_AfterLocalEndTime_ClampsToStartTimeNextDayConvertedToUtc_Test()
        {
            // Arrange: 16:00:00 UTC = 23:00:00 đêm VN (sau giờ kết thúc 17:00)
            var nowUtc = new DateTime(2026, 10, 5, 16, 0, 0, DateTimeKind.Utc);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = VietnamStandardScheduleJson
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowUtc);

            // Assert: Phải kẹp sang 08:00:00 sáng VN hôm sau = 01:00:00 UTC ngày hôm sau (06/10/2026)
            var expectedNextRun = new DateTime(2026, 10, 6, 1, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expectedNextRun, nextRun);
        }

        #endregion

        #region 2. Kiểm thử IsWithinTimeWindow hỗ trợ sự kiện realtime

        /// <summary>
        /// Description: Kiểm thử IsWithinTimeWindow cho luồng gửi theo sự kiện realtime,
        ///              quy đổi chính xác giờ UTC của máy chủ sang giờ VN trước khi so khớp [08:00 - 17:00].
        /// Created date: 05/10/2026
        /// </summary>
        [Theory]
        [InlineData(1, 26, 28, true)]   // 01:26:28 UTC = 08:26:28 VN -> Trong khung giờ
        [InlineData(5, 0, 0, true)]     // 05:00:00 UTC = 12:00:00 trưa VN -> Trong khung giờ
        [InlineData(9, 59, 59, true)]   // 09:59:59 UTC = 16:59:59 chiều VN -> Trong khung giờ
        [InlineData(10, 0, 1, false)]   // 10:00:01 UTC = 17:00:01 chiều VN -> Quá giờ làm việc
        [InlineData(0, 30, 0, false)]   // 00:30:00 UTC = 07:30:00 sáng VN -> Chưa đến giờ làm việc
        [InlineData(16, 0, 0, false)]   // 16:00:00 UTC = 23:00:00 đêm VN -> Ngoài khung giờ
        public void IsWithinTimeWindow_UtcServerClock_EvaluatesAgainstLocalVietnamTimeWindow_Test(
            int utcHour, int utcMinute, int utcSecond, bool expectedWithinWindow)
        {
            // Arrange
            var nowUtc = new DateTime(2026, 10, 5, utcHour, utcMinute, utcSecond, DateTimeKind.Utc);
            var sub = new ShareDataSubscription
            {
                ScheduleJson = VietnamStandardScheduleJson
            };

            // Act
            var isWithin = DataOutboundScheduler.IsWithinTimeWindow(sub, nowUtc);

            // Assert
            Assert.Equal(expectedWithinWindow, isWithin);
        }

        #endregion

        #region 3. Kiểm thử khi máy chủ chạy múi giờ Local (Việt Nam UTC+7)

        /// <summary>
        /// Description: Kiểm tra khi máy chủ hoặc DB chạy trực tiếp theo múi giờ Local (offset = 0).
        ///              Thời điểm 08:26:28 sáng Local nằm trong [08:00 - 17:00] thì NextTimeRun = now + 30s (08:26:58).
        /// Created date: 05/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_LocalServerClock_WorksCorrectlyWithoutOffset_Test()
        {
            // Arrange: 08:26:28 giờ Local
            var nowLocal = new DateTime(2026, 10, 5, 8, 26, 28, DateTimeKind.Local);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = VietnamStandardScheduleJson
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowLocal);

            // Assert: Lần chạy tiếp theo là 08:26:58 Local
            var expectedNextRun = new DateTime(2026, 10, 5, 8, 26, 58, DateTimeKind.Local);
            Assert.Equal(expectedNextRun, nextRun);
        }

        #endregion

        #region 4. Kiểm thử chế độ Daily (Lịch chạy cố định giờ) khi DB chạy UTC

        /// <summary>
        /// Description: Kiểm tra lịch chạy Daily lúc 08:00 sáng mỗi ngày. Khi DB clock là UTC lúc 00:30:00 UTC (07:30 sáng VN),
        ///              lần chạy tiếp theo trong ngày phải là 08:00:00 sáng VN quy đổi về UTC = 01:00:00 UTC cùng ngày.
        /// Created date: 05/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextDailyRun_UtcServerClock_SchedulesDailyAtLocalVietnamTimeConvertedToUtc_Test()
        {
            // Arrange: Thứ Hai 05/10/2026 lúc 00:30:00 UTC (07:30 sáng VN)
            var nowUtc = new DateTime(2026, 10, 5, 0, 30, 0, DateTimeKind.Utc);
            var sub = new ShareDataSubscription
            {
                ScheduleJson = "{\"kind\": \"daily\", \"startTime\": \"08:00\", \"daysOfWeek\": [\"MON\", \"TUE\", \"WED\", \"THU\", \"FRI\"]}"
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowUtc);

            // Assert: Lần chạy tiếp theo phải là 08:00 VN = 01:00:00 UTC cùng ngày
            var expectedNextRun = new DateTime(2026, 10, 5, 1, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expectedNextRun, nextRun);
        }

        #endregion
    }
}
