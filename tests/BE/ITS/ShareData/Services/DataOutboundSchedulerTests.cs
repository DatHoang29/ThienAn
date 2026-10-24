using System.Text.Json;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Scheduling;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Bộ kiểm thử chuyên biệt cho DataOutboundScheduler — kiểm chứng nguyên nhân gốc rễ
    ///              lệch múi giờ giữa đồng hồ DB (UTC) và cấu hình lịch gửi địa phương (Việt Nam UTC+7).
    /// Created date: 05/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    public class DataOutboundSchedulerTests
    {
        private const string VietnamStandardScheduleJson = "{\"startTime\": \"08:00\", \"endTime\": \"17:00\"}";
        private const int DefaultIntervalSeconds = 60;

        #region 1. Kiểm thử ca lỗi gốc rễ: Máy chủ DB chạy UTC trong khi người dùng cấu hình giờ Việt Nam

        /// <summary>
        /// Description: Kiểm thử tính thời điểm chạy kế tiếp khi đồng hồ DB chạy UTC đối chiếu cấu hình giờ VN [08:00 - 17:00]:
        ///              - Trong giờ làm việc: now + interval không bị kẹp (01:26:28 UTC -> 01:26:58 UTC).
        ///              - Trước giờ làm việc: kẹp về giờ bắt đầu hôm nay quy đổi sang UTC (00:00:00 UTC -> 01:00:00 UTC).
        ///              - Sau giờ làm việc: kẹp sang giờ bắt đầu hôm sau quy đổi sang UTC (16:00:00 UTC -> 01:00:00 UTC ngày hôm sau).
        /// Created date: 05/10/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Theory]
        [InlineData(1, 26, 28, 5, 1, 26, 58)] // 01:26:28 UTC = 08:26:28 VN -> now + 30s = 01:26:58 UTC
        [InlineData(0, 0, 0, 5, 1, 0, 0)]      // 00:00:00 UTC = 07:00:00 VN -> kẹp đến 08:00 VN = 01:00:00 UTC cùng ngày
        [InlineData(16, 0, 0, 6, 1, 0, 0)]     // 16:00:00 UTC = 23:00:00 VN -> kẹp đến 08:00 VN = 01:00:00 UTC ngày hôm sau
        public void ComputeNextTimeRun_WhenUtcServerClock_EvaluatesClampingCorrectly_Test(
            int nowHour, int nowMinute, int nowSecond,
            int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            // Arrange
            var nowUtc = new DateTime(2026, 10, 5, nowHour, nowMinute, nowSecond, DateTimeKind.Utc);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = VietnamStandardScheduleJson
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowUtc, DefaultIntervalSeconds);

            // Assert
            var expectedNextRun = new DateTime(2026, 10, expectedDay, expectedHour, expectedMinute, expectedSecond, DateTimeKind.Utc);
            Assert.Equal(expectedNextRun, nextRun);
        }

        #endregion

        #region 2. Kiểm thử IsValidTime hỗ trợ sự kiện realtime

        /// <summary>
        /// Description: Kiểm thử IsValidTime cho luồng gửi theo sự kiện realtime,
        ///              quy đổi chính xác giờ UTC của máy chủ sang giờ VN trước khi so khớp [08:00 - 17:00].
        /// Created date: 05/10/2026
        /// Modified date: 07/10/2026
        /// </summary>
        [Theory]
        [InlineData(1, 26, 28, true)]   // 01:26:28 UTC = 08:26:28 VN -> Trong khung giờ
        [InlineData(5, 0, 0, true)]     // 05:00:00 UTC = 12:00:00 trưa VN -> Trong khung giờ
        [InlineData(9, 59, 59, true)]   // 09:59:59 UTC = 16:59:59 chiều VN -> Trong khung giờ
        [InlineData(10, 0, 1, false)]   // 10:00:01 UTC = 17:00:01 chiều VN -> Quá giờ làm việc
        [InlineData(0, 30, 0, false)]   // 00:30:00 UTC = 07:30:00 sáng VN -> Chưa đến giờ làm việc
        [InlineData(16, 0, 0, false)]   // 16:00:00 UTC = 23:00:00 đêm VN -> Ngoài khung giờ
        public void IsValidTime_UtcServerClock_EvaluatesAgainstLocalVietnamTime_Test(
            int utcHour, int utcMinute, int utcSecond, bool expectedWithin)
        {
            // Arrange
            var nowUtc = new DateTime(2026, 10, 5, utcHour, utcMinute, utcSecond, DateTimeKind.Utc);
            var sub = new ShareDataSubscription
            {
                ScheduleJson = VietnamStandardScheduleJson
            };

            // Act
            var isWithin = DataOutboundScheduler.IsValidTime(sub, nowUtc);

            // Assert
            Assert.Equal(expectedWithin, isWithin);
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
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowLocal, DefaultIntervalSeconds);

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
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowUtc, DefaultIntervalSeconds);

            // Assert: Lần chạy tiếp theo phải là 08:00 VN = 01:00:00 UTC cùng ngày
            var expectedNextRun = new DateTime(2026, 10, 5, 1, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expectedNextRun, nextRun);
        }

        #endregion

        #region 5. Kiểm thử khung giờ qua đêm (Start > End)

        private const string OvernightScheduleJson = "{\"startTime\": \"22:00\", \"endTime\": \"05:00\"}";

        /// <summary>
        /// Description: Kiểm thử IsValidTime với khung giờ qua đêm 22:00 → 05:00 (máy chủ chạy giờ Local,
        ///              offset = 0, để tách biệt phép thử wraparound khỏi phép quy đổi múi giờ đã test ở region 1-3).
        /// Created date: 07/10/2026
        /// </summary>
        [Theory]
        [InlineData(23, 0, 0, true)]    // Trong khung, đoạn tối
        [InlineData(4, 0, 0, true)]     // Trong khung, đoạn sáng hôm sau
        [InlineData(22, 0, 0, true)]    // Đúng biên StartTime
        [InlineData(5, 0, 0, true)]     // Đúng biên EndTime
        [InlineData(12, 0, 0, false)]   // Giữa trưa — vùng gap ban ngày
        [InlineData(21, 59, 59, false)] // Ngay trước biên StartTime — vẫn thuộc vùng gap
        public void IsValidTime_Overnight_EvaluatesWraparoundCorrectly_Test(
            int localHour, int localMinute, int localSecond, bool expectedWithin)
        {
            // Arrange
            var nowLocal = new DateTime(2026, 10, 7, localHour, localMinute, localSecond, DateTimeKind.Local);
            var sub = new ShareDataSubscription
            {
                ScheduleJson = OvernightScheduleJson
            };

            // Act
            var isWithin = DataOutboundScheduler.IsValidTime(sub, nowLocal);

            // Assert
            Assert.Equal(expectedWithin, isWithin);
        }

        /// <summary>
        /// Description: Kiểm thử ComputeNextTimeRun với khung giờ qua đêm [22:00 - 05:00] (chu kỳ 30s):
        ///              - Trong vùng gap ban ngày (12:00): kẹp tới giờ bắt đầu tối nay (22:00:00).
        ///              - Trong đoạn tối (23:00): không kẹp, next run = now + 30s (23:00:30).
        ///              - Trong đoạn sáng hôm sau (04:00): không kẹp, next run = now + 30s (04:00:30).
        /// Created date: 07/10/2026
        /// Modified date: 24/10/2026
        /// </summary>
        [Theory]
        [InlineData(12, 0, 0, 7, 22, 0, 0)]  // Giữa trưa (gap): kẹp tới 22:00 tối nay
        [InlineData(23, 0, 0, 7, 23, 0, 30)] // Đoạn tối: now + 30s
        [InlineData(4, 0, 0, 7, 4, 0, 30)]   // Đoạn sáng: now + 30s
        public void ComputeNextTimeRun_WhenOvernightSchedule_EvaluatesClampingCorrectly_Test(
            int nowHour, int nowMinute, int nowSecond,
            int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
        {
            // Arrange
            var nowLocal = new DateTime(2026, 10, 7, nowHour, nowMinute, nowSecond, DateTimeKind.Local);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = OvernightScheduleJson
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowLocal, DefaultIntervalSeconds);

            // Assert
            var expectedNextRun = new DateTime(2026, 10, expectedDay, expectedHour, expectedMinute, expectedSecond, DateTimeKind.Local);
            Assert.Equal(expectedNextRun, nextRun);
        }

        #endregion

        #region 6. Kiểm thử khung giờ hẹp (cách nhau 1 phút)

        private const string OneMinuteNarrowScheduleJson = "{\"startTime\": \"06:00\", \"endTime\": \"06:01\"}";

        /// <summary>
        /// Description: Kiểm thử IsValidTime với khung giờ hẹp 1 phút (06:00 -> 06:01):
        ///              - Trong khoảng [06:00:00, 06:01:00]: gửi được (true).
        ///              - Trước 06:00:00 hoặc sau 06:01:00: không gửi (false).
        /// Created date: 09/10/2026
        /// </summary>
        [Theory]
        [InlineData(6, 0, 0, true)]    // Đúng biên StartTime 06:00:00 -> Được gửi
        [InlineData(6, 0, 30, true)]   // Giữa khung 06:00:30 (sau 30s) -> Được gửi
        [InlineData(6, 1, 0, true)]    // Đúng biên EndTime 06:01:00 -> Được gửi
        [InlineData(5, 59, 59, false)] // Trước 06:00:00 -> Ngoài khung giờ
        [InlineData(6, 1, 1, false)]   // Sau 06:01:00 -> Ngoài khung giờ
        public void IsValidTime_OneMinuteWindow_EvaluatesCorrectly_Test(
            int hour, int minute, int second, bool expectedWithin)
        {
            // Arrange
            var nowLocal = new DateTime(2026, 10, 9, hour, minute, second, DateTimeKind.Local);
            var sub = new ShareDataSubscription
            {
                ScheduleJson = OneMinuteNarrowScheduleJson
            };

            // Act
            var isWithin = DataOutboundScheduler.IsValidTime(sub, nowLocal);

            // Assert
            Assert.Equal(expectedWithin, isWithin);
        }

        /// <summary>
        /// Description: Kiểm thử ComputeNextTimeRun với khung giờ 1 phút (06:00 -> 06:01) và chu kỳ 30 giây:
        ///              - Lần 1 (06:00:00): candidate = 06:00:30, vẫn trong khung -> NextRun = 06:00:30 (không bị kẹp).
        ///              - Lần 2 (06:00:30): candidate = 06:01:00, vẫn trong khung -> NextRun = 06:01:00 (không bị kẹp).
        ///              - Khi candidate vượt quá 06:01:00: tự động kẹp sang 06:00:00 sáng ngày hôm sau.
        /// Created date: 09/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_OneMinuteWindow_SchedulesTwiceThenClampsToNextDay_Test()
        {
            // Arrange
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = OneMinuteNarrowScheduleJson
            };

            // Act & Assert 1: Lúc 06:00:00 -> NextRun là 06:00:30 (Gửi lần 1)
            var t0 = new DateTime(2026, 10, 9, 6, 0, 0, DateTimeKind.Local);
            var next0 = DataOutboundScheduler.ComputeNextTimeRun(sub, t0, DefaultIntervalSeconds);
            Assert.Equal(new DateTime(2026, 10, 9, 6, 0, 30, DateTimeKind.Local), next0);

            // Act & Assert 2: Lúc 06:00:30 -> NextRun là 06:01:00 (Gửi lần 2)
            var t1 = new DateTime(2026, 10, 9, 6, 0, 30, DateTimeKind.Local);
            var next1 = DataOutboundScheduler.ComputeNextTimeRun(sub, t1, DefaultIntervalSeconds);
            Assert.Equal(new DateTime(2026, 10, 9, 6, 1, 0, DateTimeKind.Local), next1);

            // Act & Assert 3: Lúc 06:01:00 -> candidate là 06:01:30 (> 06:01:00) -> Kẹp sang 06:00:00 ngày hôm sau
            var t2 = new DateTime(2026, 10, 9, 6, 1, 0, DateTimeKind.Local);
            var next2 = DataOutboundScheduler.ComputeNextTimeRun(sub, t2, DefaultIntervalSeconds);
            Assert.Equal(new DateTime(2026, 10, 10, 6, 0, 0, DateTimeKind.Local), next2);
        }

        #endregion

        #region 7. Kiểm thử fallback defaultIntervalSeconds khi IntervalSeconds của đăng ký null hoặc không hợp lệ

        /// <summary>
        /// Description: Kiểm thử ComputeNextTimeRun khi đăng ký không cấu hình IntervalSeconds (null)
        ///              thì sử dụng đúng defaultIntervalSeconds được truyền từ cấu hình worker.
        /// Created date: 18/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_WhenIntervalSecondsNull_UsesProvidedDefaultInterval_Test()
        {
            // Arrange
            var now = new DateTime(2026, 10, 18, 10, 0, 0, DateTimeKind.Local);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = null
            };
            const int testDefaultInterval = 45;

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, now, testDefaultInterval);

            // Assert
            Assert.Equal(now.AddSeconds(testDefaultInterval), nextRun);
        }

        #endregion
    }
}
