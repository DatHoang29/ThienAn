using System.Globalization;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Text;

namespace Modules.VDS.Core.Constants
{
    /// <summary>
    /// Author: KienLT
    /// Description: HỢP ĐỒNG nhận dạng sự cố "quá tốc độ TRUNG BÌNH trên đoạn A→B" — dùng chung giữa worker
    /// <c>Services.Vds</c> (bên GHI) và module WebAPI <c>Modules.VDS</c> (bên ĐỌC).
    ///
    /// <para><b>Vì sao nằm ở Core.</b> Cùng một công thức khoá phải được hai TIẾN TRÌNH khác nhau sinh ra
    /// giống nhau từng ký tự: worker ghi <c>TmsIncident.EventId</c> khi nâng sự cố, WebAPI dò lại đúng khoá đó
    /// để trả sự cố của một hành trình. Lệch một ký tự là bên đọc không bao giờ tìm thấy gì, mà KHÔNG có lỗi
    /// nào để lần ra. <c>Modules.VDS.Core</c> là project duy nhất cả hai bên đều tham chiếu được, và
    /// <c>Constants/</c> là một trong các nhóm đã được chốt cho Core.</para>
    ///
    /// <para><b>Đổi loại sự cố về sau chỉ sửa ở ĐÂY.</b> Từ 29/09/2026 dùng loại riêng "quá tốc độ trung
    /// bình" (<see cref="EventTypeName"/> = <c>SpeedingAvg</c>, <see cref="ConditionCode"/> =
    /// <c>speedingAvg</c>), tách khỏi luật quá tốc độ tức thời (<c>speeding</c>). Sự cố cũ giữ nguyên loại
    /// <c>speeding</c>. <see cref="EventIdPrefix"/> và <see cref="ConditionDiscriminator"/> thì KHÔNG được đổi.</para>
    ///
    /// <para><b>Hai hằng KHÔNG được đổi</b> vì đã nằm trong dữ liệu lịch sử: <see cref="EventIdPrefix"/> và
    /// <see cref="ConditionDiscriminator"/> tạo nên <c>EventId</c> của mọi sự cố đã ghi. Đổi chúng làm toàn bộ
    /// sự cố cũ mất liên kết với hành trình, và cửa chống trùng coi mọi chuyến là mới.</para>
    /// Created date: 25/09/2026
    /// </summary>
    public static class VdsJourneyEventConst
    {
        /// <summary>
        /// Tên loại sự kiện trong danh mục thiết bị, dùng để tra <c>TmsEventType</c>.
        ///
        /// <para>29/09/2026: tách khỏi <c>Speeding</c> (quá tốc độ tức thời) sang loại riêng <c>SpeedingAvg</c>.
        /// Tên này KHÔNG phải loại bản tin AID, chỉ là khoá tra mapping <c>vds_mapping_event_type</c> và cửa sổ
        /// chống trùng. Nó cũng nằm trong <c>ConditionKey</c>, nên sự cố cũ (tạo lúc còn là <c>Speeding</c>)
        /// không chặn trùng sự cố mới.</para>
        /// </summary>
        public const string EventTypeName = "SpeedingAvg";

        /// <summary>
        /// Slug điều kiện, khớp <c>TmsEventType.Code</c> đã seed (<c>speedingAvg</c> — "Vi phạm tốc độ trung
        /// bình"). Là giá trị fallback khi danh mục thiết bị chưa khai mapping cho <see cref="EventTypeName"/>.
        /// </summary>
        public const string ConditionCode = "speedingAvg";

        /// <summary>
        /// Tiền tố <c>EventId</c> phân biệt sự cố sinh từ HÀNH TRÌNH với sự cố sinh từ bản tin thiết bị.
        /// <b>KHÔNG ĐỔI</b> — đã nằm trong dữ liệu lịch sử.
        /// </summary>
        public const string EventIdPrefix = "jny:";

        /// <summary>
        /// Thành phần phân biệt trong khoá băm, tách sự cố "tốc độ trung bình" khỏi các điều kiện khác trên
        /// cùng một đoạn. <b>KHÔNG ĐỔI</b> — đã nằm trong dữ liệu lịch sử.
        /// </summary>
        public const string ConditionDiscriminator = "avgspeed";

        /// <summary>
        /// Tiền tố định danh phụ của khoá chống trùng theo điều kiện: hai xe khác biển vượt tốc trung bình
        /// trên cùng đoạn là hai việc khác nhau, nên biển số phải nằm trong khoá.
        /// </summary>
        public const string DedupSubKeyPrefix = "avg";

        /// <summary>
        /// Định danh phụ cho khoá chống trùng theo điều kiện: <c>avg|BIỂNSỐ</c>.
        ///
        /// <para>Biển số để trống vẫn tạo khoá có tiền tố, không trả chuỗi rỗng: chuỗi rỗng sẽ gộp mọi xe
        /// không đọc được biển trên cùng đoạn thành MỘT việc và chặn hết các bản tin sau.</para>
        /// </summary>
        public static string DedupSubKey(string? licensePlate)
            => DedupSubKeyPrefix + "|" + (licensePlate?.Trim() ?? string.Empty);

        /// <summary>
        /// <c>TmsIncident.EventId</c> TẤT ĐỊNH của một hành trình: replay cùng chuyến không sinh sự cố thứ hai
        /// (bảng có UNIQUE trên <c>TenantId, EventId</c>).
        ///
        /// <para><b>Đây là bản DUY NHẤT của công thức.</b> Trước 25/09/2026 nó nằm ở HAI nơi — worker tự ghép
        /// <c>"jny:" + ConditionHash.Create(...)</c> còn WebAPI có <c>VdsJourneyKey.ForAvgSpeed</c> — và chính
        /// XML doc bên WebAPI đã cảnh báo "đừng đổi riêng lẻ". Gộp về đây để không còn chỗ cho hai bên lệch
        /// nhau.</para>
        ///
        /// <para>Dùng HASH thay vì ghép chuỗi vì cột <c>EventId</c> giới hạn 64 ký tự, còn segmentId là GUID
        /// cộng mốc thời gian ISO thì đã vượt. Hash gồm cả <paramref name="fromTimeUtc"/> nên hai chuyến khác
        /// nhau của cùng biển trên cùng đoạn KHÔNG bị gộp làm một.</para>
        ///
        /// <para>Chuẩn hóa theo SQL datetime trước khi hash; LookupEventIds vẫn nhận khóa lịch sử
        /// từ timestamp millisecond chưa làm tròn, cả Utc và Unspecified.</para>
        /// </summary>
        public static string EventId(string? segmentId, string? licensePlate, DateTime fromTimeUtc)
        {
            var instant = fromTimeUtc.Kind == DateTimeKind.Local ? fromTimeUtc.ToUniversalTime() : fromTimeUtc;
            // SQL datetime rounds to 1/300 second and drops Kind. Both producers must hash that representation.
            return LegacyEventId(segmentId, licensePlate,
                DateTime.SpecifyKind(new SqlDateTime(instant).Value, DateTimeKind.Unspecified));
        }

        /// <summary>Exact keys only; legacy millisecond timestamps are constrained to the same SQL datetime bucket.</summary>
        public static string[] LookupEventIds(string? segmentId, string? licensePlate, DateTime fromTime)
        {
            var instant = fromTime.Kind == DateTimeKind.Local ? fromTime.ToUniversalTime() : fromTime;
            var rounded = new SqlDateTime(instant);
            var keys = new HashSet<string>(StringComparer.Ordinal) { EventId(segmentId, licensePlate, instant) };
            keys.Add(LegacyEventId(segmentId, licensePlate, instant));
            for (var offset = -2; offset <= 2; offset++)
            {
                var candidate = rounded.Value.AddMilliseconds(offset);
                if (new SqlDateTime(candidate).Value != rounded.Value) continue;
                keys.Add(LegacyEventId(segmentId, licensePlate, DateTime.SpecifyKind(candidate, DateTimeKind.Unspecified)));
                keys.Add(LegacyEventId(segmentId, licensePlate, DateTime.SpecifyKind(candidate, DateTimeKind.Utc)));
            }
            return keys.ToArray();
        }

        private static string LegacyEventId(string? segmentId, string? licensePlate, DateTime instant)
        {
            var raw = $"{segmentId}|{ConditionDiscriminator}|{licensePlate}|"
                + instant.ToString("O", CultureInfo.InvariantCulture);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return EventIdPrefix + Convert.ToHexString(hash).ToLowerInvariant()[..32];
        }
    }
}
