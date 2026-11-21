namespace Shared.Utility.Utilities
{
    /// <summary>
    /// Time Server Utility
    /// </summary>
    public class TimeServerUtil
    {
        public static DateTime GetTimeServer()
        {
            return DateTime.Now;
        }

        /// <summary>
        /// Convert Datetime UTC +0 to local system timezone
        /// </summary>
        /// <param name="utcTime"></param>
        /// <returns></returns>
        public static DateTime? GetTimeServer(DateTime? utcTime)
        {
            if (utcTime == null)
            {
                return null;
            }

            //return datetime utc to correct local timezone
            var time = TimeZoneInfo.ConvertTimeFromUtc(utcTime.Value, TimeZoneInfo.Local);
            return time;
        }

        /// <summary>
        /// Chuẩn hoá thời điểm dùng để đọc/ghi kho thời gian UTC.
        /// Local được đổi sang UTC; Unspecified được hiểu là UTC để không tự dịch giờ
        /// của payload đã theo hợp đồng UTC nhưng thiếu hậu tố Z.
        /// </summary>
        public static DateTime ToUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        /// <summary>Phiên bản nullable của <see cref="ToUtc(DateTime)"/>.</summary>
        public static DateTime? ToUtc(DateTime? value)
        {
            return value.HasValue ? ToUtc(value.Value) : null;
        }

        /// <summary>
        /// Quy một mốc GIỜ SERVER về UTC thật.
        ///
        /// <para><b>Khác <see cref="ToUtc(DateTime)"/> ở chỗ nào:</b> <c>ToUtc</c> coi
        /// <see cref="DateTimeKind.Unspecified"/> LÀ UTC nên chỉ dán Kind, KHÔNG dịch giá trị — đúng cho kho
        /// UTC (QuestDB, hoặc payload theo hợp đồng UTC nhưng thiếu hậu tố Z). Hàm này coi
        /// <c>Unspecified</c> là GIỜ SERVER và dịch về UTC — đúng cho cột SQL Server được ghi bằng
        /// <c>DateTime.Now</c>, vì SqlSugar trả các cột đó về dưới dạng <c>Unspecified</c>.</para>
        ///
        /// <para>Dán Z vào giá trị giờ server mà không dịch là công bố sai thời điểm đúng 7 tiếng ở múi giờ
        /// Việt Nam. Chọn sai hàm giữa hai hàm này là nguyên nhân của lớp lỗi đó.</para>
        ///
        /// <para><b>Chỉ dùng cho cột đã xác minh là giờ server</b> (rà 22/09/2026, cập nhật 25/09/2026):
        /// <c>TmsIncident.StartDate</c>, <c>CreatedDate</c>, <c>ValidatedDate</c>, <c>ApprovedDate</c>,
        /// <c>FinishedDate</c>, <c>TmsZoneStatus.UpdateTime</c>, cùng các cột audit
        /// <c>CreateTime</c>/<c>UpdateTime</c> của <c>EntityTenant</c>.</para>
        ///
        /// <para><b>KHÔNG dùng cho các cột worker ghi UTC</b>: <c>TmsTrafficJourney.FromTime/ToTime</c>,
        /// <c>TmsTrafficSummary.*</c>, <c>TmsVehicleFilterHistory.DetectTime</c> và mọi cột của QuestDB — quy
        /// đổi lần nữa là sai ngược lại. Với các cột đó dùng <see cref="ToUtc(DateTime)"/>.</para>
        ///
        /// <para><c>Utc</c> được giữ nguyên để không trừ hai lần nếu người gọi đã convert trước.
        /// Múi giờ lấy từ <see cref="TimeZoneInfo.Local"/> của process, giống <see cref="GetTimeServer()"/>;
        /// solution không có khóa cấu hình múi giờ nào.</para>
        /// </summary>
        public static DateTime ToServerUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeZoneInfo.Local),
            };
        }

        /// <summary>Phiên bản nullable của <see cref="ToServerUtc(DateTime)"/>; <c>null</c> giữ nguyên <c>null</c>.</summary>
        public static DateTime? ToServerUtc(DateTime? value)
        {
            return value.HasValue ? ToServerUtc(value.Value) : null;
        }

        /// <summary>
        /// Quy một mốc UTC về GIỜ SERVER — phép nghịch của <see cref="ToServerUtc(DateTime)"/>.
        ///
        /// <para><b>Vì sao cần bản này khi đã có <see cref="GetTimeServer(DateTime?)"/>:</b> hàm kia nhận
        /// <c>DateTime?</c> và gọi thẳng <see cref="TimeZoneInfo.ConvertTimeFromUtc(DateTime, TimeZoneInfo)"/>,
        /// mà hàm BCL đó NÉM <see cref="ArgumentException"/> khi <c>Kind = Local</c>. Vì vậy các caller có giá
        /// trị non-nullable hoặc không chắc Kind đã phải tự viết
        /// <c>ConvertTimeFromUtc(SpecifyKind(x, Utc), Local)</c> — cùng một biểu thức bị lặp ở 8 chỗ trong
        /// module TMS.Report và VDS trước 25/09/2026.</para>
        ///
        /// <para><c>Unspecified</c> được hiểu là UTC (cùng quy ước với <see cref="ToUtc(DateTime)"/>, đúng cho
        /// giá trị đọc từ QuestDB hoặc cột UTC của SQL Server). <c>Local</c> giữ nguyên vì đã là giờ server,
        /// quy đổi lần nữa là cộng thêm một lần lệch.</para>
        ///
        /// <para>Kết quả mang <see cref="DateTimeKind.Unspecified"/> — giống giá trị <c>DateTime.Now</c> sau
        /// khi đi qua SQL Server, nên so sánh với các cột giờ server không bị lệch Kind.</para>
        /// </summary>
        public static DateTime ToServerTime(DateTime value)
        {
            if (value.Kind == DateTimeKind.Local) return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
            var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local), DateTimeKind.Unspecified);
        }

        /// <summary>Phiên bản nullable của <see cref="ToServerTime(DateTime)"/>; <c>null</c> giữ nguyên <c>null</c>.</summary>
        public static DateTime? ToServerTime(DateTime? value)
        {
            return value.HasValue ? ToServerTime(value.Value) : null;
        }

        /// <summary>
        /// Trả khoảng UTC [đầu ngày local, đầu ngày local kế tiếp) chứa thời điểm truyền vào.
        /// Dùng khi dữ liệu lưu UTC nhưng báo cáo/chỉ số được tổ chức theo ngày vận hành local.
        /// </summary>
        public static (DateTime FromUtc, DateTime ToUtc) GetLocalDayUtcRange(
            DateTime instant, TimeZoneInfo? timeZone = null)
        {
            timeZone ??= TimeZoneInfo.Local;
            var localDay = TimeZoneInfo.ConvertTimeFromUtc(ToUtc(instant), timeZone).Date;
            var from = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDay, DateTimeKind.Unspecified), timeZone);
            var to = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDay.AddDays(1), DateTimeKind.Unspecified), timeZone);
            return (from, to);
        }
    }
}
