namespace Module.ShareData.Core.Dto.Subscription
{
    /// <summary>
    /// Description: Lịch gửi của đăng ký (ánh xạ với ShareDataSubscription.ScheduleJson).
    ///              - continuous: gửi theo chu kỳ UpdateDelaySec trong khung [StartTime..EndTime].
    ///              - daily: theo DaysOfWeek, [StartDate..EndDate], StartTime, DurationMinutes.
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataScheduleDto
    {
        /// <summary>Kiểu lịch: continuous | daily.</summary>
        public string? Kind { get; set; }

        /// <summary>Chu kỳ gửi (giây) — tối thiểu 5.</summary>
        public int? UpdateDelaySec { get; set; }

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
