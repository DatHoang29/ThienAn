using System.Text.Json;

namespace ShareDataWorker.Infrastructure.Services.DataOutbound.Scheduling
{
    /// <summary>
    /// Description: Tính toán thời điểm chạy tiếp theo cho ShareDataSubscription.
    /// Hỗ trợ 3 chế độ: Event (poll 5s), Daily (theo giờ/thứ), Continuous (interval).
    /// Parse ScheduleJson bằng JsonDocument (STJ) — không dùng Newtonsoft để tránh
    /// import Newtonsoft vào ShareDataWorker, vẫn xử lý đúng key "updateDelaySec" kế thừa.
    /// Created date: 17/09/2026
    /// </summary>
    public static class DataOutboundScheduler
    {
        private static readonly Dictionary<string, DayOfWeek> DayCodeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MON"] = DayOfWeek.Monday,
            ["TUE"] = DayOfWeek.Tuesday,
            ["WED"] = DayOfWeek.Wednesday,
            ["THU"] = DayOfWeek.Thursday,
            ["FRI"] = DayOfWeek.Friday,
            ["SAT"] = DayOfWeek.Saturday,
            ["SUN"] = DayOfWeek.Sunday
        };

        private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

        /// <summary>
        /// Description: Tính thời điểm kế tiếp cần chạy cho một đăng ký.
        /// - ScheduleJson Kind = "daily": tính lần chạy tiếp theo theo giờ/thứ/biên ngày.
        /// - Mặc định (continuous): now + IntervalSeconds, kẹp trong khung giờ [StartTime, EndTime] nếu có cấu hình.
        /// - Mode = Event: tạm thời tính theo continuous/daily trong khi cơ chế realtime chưa triển khai.
        /// Created date: 17/09/2026
        /// Modified date: 21/09/2026
        /// </summary>
        public static DateTime ComputeNextTimeRun(
            ShareDataSubscription sub,
            DateTime now,
            int defaultIntervalSeconds)
        {
            var intervalSec = sub.IntervalSeconds is > 0 ? sub.IntervalSeconds.Value : defaultIntervalSeconds;
            var candidate = now.AddSeconds(intervalSec);

            if (string.IsNullOrWhiteSpace(sub.ScheduleJson))
                return candidate;

            try
            {
                using var doc = JsonDocument.Parse(sub.ScheduleJson);
                var root = doc.RootElement;

                if (TryGetProperty(root, "kind", out var kindEl)
                    && string.Equals(kindEl.GetString(), "daily", StringComparison.OrdinalIgnoreCase))
                {
                    var result = ComputeNextDailyRun(root, now);
                    if (result.HasValue)
                        return result.Value;

                    return candidate;
                }

                return EnsureValidTime(root, candidate, now);
            }
            catch
            {
                // JSON hỏng: fallback về continuous (now + intervalSec)
                return candidate;
            }
        }

        /// <summary>
        /// Description: Đăng ký có đang nằm trong khung giờ [StartTime, EndTime] được phép gửi không.
        ///              Hỗ trợ cả khung giờ qua đêm (StartTime > EndTime, ví dụ 22:00-05:00).
        ///              Dùng cho luồng gửi theo sự kiện — luồng đó không đi qua ComputeNextTimeRun.
        /// Created date: 28/09/2026
        /// Modified date: 07/10/2026
        /// </summary>
        public static bool IsValidTime(ShareDataSubscription sub, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(sub.ScheduleJson))
                return true;

            try
            {
                using var doc = JsonDocument.Parse(sub.ScheduleJson);
                var root = doc.RootElement;

                if (!TryParseTimeOfDay(root, "startTime", out var startOfDay)
                    || !TryParseTimeOfDay(root, "endTime", out var endOfDay))
                    return true;

                var offset = GetDbToLocalOffset(now);
                var localNow = now.Add(offset);
                return IsTimeInRange(localNow.TimeOfDay, startOfDay, endOfDay);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Description: Đảm bảo thời điểm chạy tiếp theo (candidate) nằm trong khung giờ [StartTime, EndTime]
        ///              cho chế độ continuous — hỗ trợ cả khung giờ qua đêm (StartTime > EndTime).
        ///              Giờ StartTime/EndTime trong cấu hình là giờ địa phương Việt Nam, cần quy đổi qua lại
        ///              với giờ DB clock. Không khai hoặc sai định dạng: giữ nguyên candidate.
        /// Created date: 18/09/2026
        /// Modified date: 07/10/2026
        /// </summary>
        private static DateTime EnsureValidTime(JsonElement scheduleRoot, DateTime candidate, DateTime now)
        {
            if (!TryParseTimeOfDay(scheduleRoot, "startTime", out var startOfDay)
                || !TryParseTimeOfDay(scheduleRoot, "endTime", out var endOfDay))
                return candidate;

            var offset = GetDbToLocalOffset(now);
            var localNow = now.Add(offset);
            var localCandidate = candidate.Add(offset);
            var startTodayLocal = localNow.Date + startOfDay;

            if (startOfDay <= endOfDay)
            {
                var endTodayLocal = localNow.Date + endOfDay;

                if (localCandidate < startTodayLocal)
                    return startTodayLocal.Subtract(offset);

                if (localCandidate > endTodayLocal)
                    return startTodayLocal.AddDays(1).Subtract(offset);

                return candidate;
            }

            // Khung giờ qua đêm: đã ở trong khung (tối nay hoặc sáng mai) thì giữ nguyên,
            // đang ở vùng "gap" ban ngày thì kẹp tới StartTime tối nay.
            return IsTimeInRange(localCandidate.TimeOfDay, startOfDay, endOfDay)
                ? candidate
                : startTodayLocal.Subtract(offset);
        }

        /// <summary>
        /// Description: Đọc và phân tích giá trị thời gian dạng HH:mm từ thuộc tính JSON.
        /// Created date: 18/09/2026
        /// </summary>
        private static bool TryParseTimeOfDay(JsonElement element, string propertyName, out TimeSpan timeOfDay)
        {
            timeOfDay = default;
            if (!TryGetProperty(element, propertyName, out var el))
                return false;

            var str = el.GetString();
            if (string.IsNullOrWhiteSpace(str))
                return false;

            return TimeSpan.TryParse(str, out timeOfDay);
        }

        /// <summary>
        /// Description: Tìm thuộc tính trong JsonElement theo tên không phân biệt hoa thường.
        /// Created date: 18/09/2026
        /// </summary>
        private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(propertyName, out value))
                    return true;

                foreach (var prop in element.EnumerateObject())
                {
                    if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        value = prop.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Description: Tính thời điểm chạy tiếp theo cho lịch daily.
        /// Trả về null nếu không tìm được (JSON thiếu StartTime, DaysOfWeek rỗng, ngoài biên ngày).
        /// Created date: 17/09/2026
        /// </summary>
        private static DateTime? ComputeNextDailyRun(JsonElement scheduleRoot, DateTime now)
        {
            // Đọc StartTime (HH:mm)
            if (!TryParseTimeOfDay(scheduleRoot, "startTime", out var startOfDay))
                return null;

            // Đọc DaysOfWeek
            var allowedDays = new HashSet<DayOfWeek>();
            if (TryGetProperty(scheduleRoot, "daysOfWeek", out var daysEl)
                && daysEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in daysEl.EnumerateArray())
                {
                    var dayStr = d.GetString();
                    if (dayStr != null && DayCodeMap.TryGetValue(dayStr, out var dow))
                        allowedDays.Add(dow);
                }
            }

            // Đọc biên ngày (StartDate / EndDate)
            DateOnly? startDate = null;
            DateOnly? endDate = null;

            if (TryGetProperty(scheduleRoot, "startDate", out var sdEl))
            {
                var sdStr = sdEl.GetString();
                if (!string.IsNullOrWhiteSpace(sdStr) && DateOnly.TryParse(sdStr, out var sd))
                    startDate = sd;
            }

            if (TryGetProperty(scheduleRoot, "endDate", out var edEl))
            {
                var edStr = edEl.GetString();
                if (!string.IsNullOrWhiteSpace(edStr) && DateOnly.TryParse(edStr, out var ed))
                    endDate = ed;
            }

            var offset = GetDbToLocalOffset(now);
            var localNow = now.Add(offset);

            // Tìm lần kích hoạt tiếp theo trong 7 ngày tới
            for (var dayOffset = 0; dayOffset <= 7; dayOffset++)
            {
                var candidateLocal = localNow.Date.AddDays(dayOffset) + startOfDay;

                // Bỏ qua nếu thời điểm đó đã qua hôm nay theo giờ địa phương
                if (candidateLocal <= localNow)
                    continue;

                var candidateDate = DateOnly.FromDateTime(candidateLocal);

                if (startDate.HasValue && candidateDate < startDate.Value)
                    continue;

                if (endDate.HasValue && candidateDate > endDate.Value)
                    return null; // Hết biên — không còn lần chạy nào hợp lệ

                if (allowedDays.Count > 0 && !allowedDays.Contains(candidateLocal.DayOfWeek))
                    continue;

                return candidateLocal.Subtract(offset);
            }

            return null;
        }

        /// <summary>
        /// Description: Xác định độ lệch giữa giờ DB (now) và giờ địa phương Việt Nam.
        ///              Nếu máy chủ DB chạy UTC (lệch với UtcNow < 120 phút) thì DB clock là UTC ->
        ///              chênh lệch là offset của Việt Nam (+7h). Ngược lại (DB đã là giờ local) -> offset = 0.
        /// Created date: 05/10/2026
        /// </summary>
        private static TimeSpan GetDbToLocalOffset(DateTime dbNow)
        {
            if (dbNow.Kind == DateTimeKind.Utc || (dbNow.Kind != DateTimeKind.Local && Math.Abs((dbNow - DateTime.UtcNow).TotalMinutes) < 120))
                return VietnamTimeZone.BaseUtcOffset;

            return TimeSpan.Zero;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lấy múi giờ Việt Nam theo hệ điều hành: tên Windows "SE Asia Standard Time", rồi tên Linux "Asia/Ho_Chi_Minh"; không có cả hai thì dùng múi giờ của máy.
        /// Created date: 05/10/2026
        /// </summary>
        private static TimeZoneInfo ResolveVietnamTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                }
                catch
                {
                    return TimeZoneInfo.Local;
                }
            }
        }

        /// <summary>
        /// Description: Giờ-trong-ngày có nằm trong khung [start, end] không — hỗ trợ cả khung giờ
        ///              qua đêm khi start > end (ví dụ 22:00 → 05:00).
        /// Created date: 07/10/2026
        /// </summary>
        private static bool IsTimeInRange(TimeSpan timeOfDay, TimeSpan start, TimeSpan end)
        {
            return start <= end
                ? timeOfDay >= start && timeOfDay <= end
                : timeOfDay >= start || timeOfDay <= end;
        }
    }
}

