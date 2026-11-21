using Newtonsoft.Json;

namespace Module.ShareData.Core.Dto.Subscription
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lịch gửi của đăng ký
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataScheduleDto
    {
        /// <summary>Kiểu lịch: continuous | daily.</summary>
        public string? Kind { get; set; }

        /// <summary>Chu kỳ gửi (giây) — tối thiểu 5.</summary>
        public int? IntervalSeconds { get; set; }

        /// <summary>
        /// Tương thích ngược
        /// </summary>
        [JsonProperty("updateDelaySec")]
        public int? UpdateDelaySecLegacy
        {
            set => IntervalSeconds ??= value;
        }

        /// <summary>Giờ bắt đầu, định dạng HH:mm.</summary>
        public string? StartTime { get; set; }

        /// <summary>Giờ kết thúc, định dạng HH:mm (chỉ dùng khi Kind = continuous).</summary>
        public string? EndTime { get; set; }

        /// <summary>Ngày trong tuần: MON | TUE | WED | THU | FRI | SAT | SUN (chỉ dùng khi Kind = daily).</summary>
        public List<string>? DaysOfWeek { get; set; }

        /// <summary>Ngày bắt đầu áp dụng, định dạng yyyy-MM-dd.</summary>
        public string? StartDate { get; set; }

        /// <summary>Ngày kết thúc áp dụng, định dạng yyyy-MM-dd.</summary>
        public string? EndDate { get; set; }

        /// <summary>Thời lượng mỗi ngày (phút), 1..1440.</summary>
        public int? DurationMinutes { get; set; }
    }
}
