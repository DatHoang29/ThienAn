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
        /// Description: Candidate (now + interval) rơi vào vùng gap ban ngày của khung giờ qua đêm 22:00-05:00
        ///              (12:00 trưa) thì phải kẹp tới StartTime (22:00) tối cùng ngày, KHÔNG phải now + interval.
        /// Created date: 07/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_Overnight_CandidateInDaytimeGap_ClampsToStartTimeTonight_Test()
        {
            // Arrange: 12:00 trưa, giữa vùng gap (05:00 - 22:00)
            var nowLocal = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Local);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = OvernightScheduleJson
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowLocal);

            // Assert: kẹp tới 22:00 tối cùng ngày
            var expectedNextRun = new DateTime(2026, 10, 7, 22, 0, 0, DateTimeKind.Local);
            Assert.Equal(expectedNextRun, nextRun);
        }

        /// <summary>
        /// Description: Candidate đã nằm trong đoạn tối của khung giờ qua đêm (23:00, trong 22:00-05:00)
        ///              thì KHÔNG bị kẹp, next run vẫn là now + interval như continuous bình thường.
        /// Created date: 07/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_Overnight_CandidateInEveningSegment_SchedulesNextIntervalWithoutClamping_Test()
        {
            // Arrange: 23:00 đêm, trong đoạn tối của khung 22:00-05:00
            var nowLocal = new DateTime(2026, 10, 7, 23, 0, 0, DateTimeKind.Local);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = OvernightScheduleJson
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowLocal);

            // Assert: không bị kẹp
            var expectedNextRun = new DateTime(2026, 10, 7, 23, 0, 30, DateTimeKind.Local);
            Assert.Equal(expectedNextRun, nextRun);
        }

        /// <summary>
        /// Description: Candidate đã nằm trong đoạn sáng hôm sau của khung giờ qua đêm (04:00, trong 22:00-05:00)
        ///              thì KHÔNG bị kẹp, next run vẫn là now + interval như continuous bình thường.
        /// Created date: 07/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_Overnight_CandidateInMorningSegment_SchedulesNextIntervalWithoutClamping_Test()
        {
            // Arrange: 04:00 sáng, trong đoạn sáng hôm sau của khung 22:00-05:00
            var nowLocal = new DateTime(2026, 10, 7, 4, 0, 0, DateTimeKind.Local);
            var sub = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = OvernightScheduleJson
            };

            // Act
            var nextRun = DataOutboundScheduler.ComputeNextTimeRun(sub, nowLocal);

            // Assert: không bị kẹp
            var expectedNextRun = new DateTime(2026, 10, 7, 4, 0, 30, DateTimeKind.Local);
            Assert.Equal(expectedNextRun, nextRun);
        }

        #endregion

        #region 6. Kiểm thử khung giờ hẹp (cách nhau 1 phút và cách nhau 10 giây)

        private const string OneMinuteNarrowScheduleJson = "{\"startTime\": \"06:00\", \"endTime\": \"06:01\"}";
        private const string TenSecondsNarrowScheduleJson = "{\"startTime\": \"23:00:00\", \"endTime\": \"23:00:10\"}";

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
            var next0 = DataOutboundScheduler.ComputeNextTimeRun(sub, t0);
            Assert.Equal(new DateTime(2026, 10, 9, 6, 0, 30, DateTimeKind.Local), next0);

            // Act & Assert 2: Lúc 06:00:30 -> NextRun là 06:01:00 (Gửi lần 2)
            var t1 = new DateTime(2026, 10, 9, 6, 0, 30, DateTimeKind.Local);
            var next1 = DataOutboundScheduler.ComputeNextTimeRun(sub, t1);
            Assert.Equal(new DateTime(2026, 10, 9, 6, 1, 0, DateTimeKind.Local), next1);

            // Act & Assert 3: Lúc 06:01:00 -> candidate là 06:01:30 (> 06:01:00) -> Kẹp sang 06:00:00 ngày hôm sau
            var t2 = new DateTime(2026, 10, 9, 6, 1, 0, DateTimeKind.Local);
            var next2 = DataOutboundScheduler.ComputeNextTimeRun(sub, t2);
            Assert.Equal(new DateTime(2026, 10, 10, 6, 0, 0, DateTimeKind.Local), next2);
        }

        /// <summary>
        /// Description: Kiểm thử IsValidTime với khung giờ siêu hẹp 10 giây (23:00:00 -> 23:00:10):
        ///              - Trong 10 giây: gửi được (true).
        ///              - Sau giây thứ 10: không gửi được (false).
        /// Created date: 09/10/2026
        /// </summary>
        [Theory]
        [InlineData(23, 0, 0, true)]   // Bắt đầu 23:00:00 -> Được gửi
        [InlineData(23, 0, 5, true)]   // Giữa khoảng 23:00:05 -> Được gửi
        [InlineData(23, 0, 10, true)]  // Đúng biên 23:00:10 -> Được gửi
        [InlineData(23, 0, 11, false)] // 23:00:11 (quá 10s) -> Ngoài khung giờ
        [InlineData(22, 59, 59, false)]// Trước 23:00:00 -> Ngoài khung giờ
        public void IsValidTime_TenSecondsWindow_EvaluatesCorrectly_Test(
            int hour, int minute, int second, bool expectedWithin)
        {
            // Arrange
            var nowLocal = new DateTime(2026, 10, 9, hour, minute, second, DateTimeKind.Local);
            var sub = new ShareDataSubscription
            {
                ScheduleJson = TenSecondsNarrowScheduleJson
            };

            // Act
            var isWithin = DataOutboundScheduler.IsValidTime(sub, nowLocal);

            // Assert
            Assert.Equal(expectedWithin, isWithin);
        }

        /// <summary>
        /// Description: Kiểm thử ComputeNextTimeRun khi khung giờ chỉ 10 giây (23:00:00 -> 23:00:10):
        ///              - Với IntervalSeconds = 30 (chu kỳ 30s > 10s):
        ///                Tại 23:00:00: candidate = 23:00:30 vượt quá 23:00:10 -> kẹp sang 23:00:00 hôm sau.
        ///                -> Chỉ gửi được đúng 1 lần duy nhất trong ngày!
        ///              - Với IntervalSeconds = 5 (chu kỳ 5s < 10s):
        ///                Tại 23:00:00: candidate = 23:00:05 <= 23:00:10 -> NextRun = 23:00:05 (gửi lần 2).
        ///                Tại 23:00:05: candidate = 23:00:10 <= 23:00:10 -> NextRun = 23:00:10 (gửi lần 3).
        ///                Tại 23:00:10: candidate = 23:00:15 > 23:00:10 -> kẹp sang 23:00:00 hôm sau.
        ///                -> Gửi được 3 lần trong 10 giây!
        /// Created date: 09/10/2026
        /// </summary>
        [Fact]
        public void ComputeNextTimeRun_TenSecondsWindow_WithDifferentIntervals_Test()
        {
            // Case A: Chu kỳ 30s (> 10s)
            var sub30 = new ShareDataSubscription
            {
                IntervalSeconds = 30,
                ScheduleJson = TenSecondsNarrowScheduleJson
            };

            var t0 = new DateTime(2026, 10, 9, 23, 0, 0, DateTimeKind.Local);
            var next30 = DataOutboundScheduler.ComputeNextTimeRun(sub30, t0);
            // Lần tiếp theo vượt quá 23:00:10 nên kẹp sang 23:00:00 ngày hôm sau
            Assert.Equal(new DateTime(2026, 10, 10, 23, 0, 0, DateTimeKind.Local), next30);

            // Case B: Chu kỳ 5s (< 10s)
            var sub5 = new ShareDataSubscription
            {
                IntervalSeconds = 5,
                ScheduleJson = TenSecondsNarrowScheduleJson
            };

            // Bắn lần 1 lúc 23:00:00 -> lần tiếp 23:00:05
            var next5_0 = DataOutboundScheduler.ComputeNextTimeRun(sub5, t0);
            Assert.Equal(new DateTime(2026, 10, 9, 23, 0, 5, DateTimeKind.Local), next5_0);

            // Bắn lần 2 lúc 23:00:05 -> lần tiếp 23:00:10
            var t0_5 = new DateTime(2026, 10, 9, 23, 0, 5, DateTimeKind.Local);
            var next5_1 = DataOutboundScheduler.ComputeNextTimeRun(sub5, t0_5);
            Assert.Equal(new DateTime(2026, 10, 9, 23, 0, 10, DateTimeKind.Local), next5_1);

            // Bắn lần 3 lúc 23:00:10 -> candidate là 23:00:15 > 23:00:10 -> kẹp sang ngày hôm sau
            var t0_10 = new DateTime(2026, 10, 9, 23, 0, 10, DateTimeKind.Local);
            var next5_2 = DataOutboundScheduler.ComputeNextTimeRun(sub5, t0_10);
            Assert.Equal(new DateTime(2026, 10, 10, 23, 0, 0, DateTimeKind.Local), next5_2);
        }

        #endregion
    }
}
