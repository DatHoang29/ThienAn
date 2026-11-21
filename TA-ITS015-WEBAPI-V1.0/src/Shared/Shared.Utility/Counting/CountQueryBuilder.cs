using System.Globalization;
using System.Text;
using SqlSugar;

namespace Shared.Utility.Counting
{
    /// <summary>
    /// Author: KienLT (chuyển sang TMS.Core dùng chung 03/09/2026)
    /// Description: Dựng câu lệnh gộp số đếm theo <c>SAMPLE BY</c> cho QuestDB. Hàm THUẦN, không chạm DB.
    ///
    /// <para><b>Dùng chung worker + WebAPI.</b> Nhận <see cref="ICountQueryFilter"/> (hợp đồng tối thiểu)
    /// thay vì một options cụ thể, nên cả <c>Services.Vds.CountingOptions</c> và DTO input của
    /// <c>Modules.VDS</c> đều gọi được. MỘT công thức cho cả hai bên ⇒ không thể ra số lệch.</para>
    ///
    /// <para><b>Vì sao SQL thô.</b> Đã chứng minh bằng test (<c>SampleBySqlGenerationTests</c>):
    /// <c>ISugarQueryable.SampleBy(...)</c> của phiên bản SqlSugar đang dùng KHÔNG sinh
    /// <c>SAMPLE BY</c> cho QuestDB (gộp cả khoảng vào một ô — sai mà không có lỗi), và
    /// <c>SqlFunc.AggregateCount(x.Speed)</c> dịch thành <c>COUNT(*)</c> nên không đếm được số mẫu tốc độ.</para>
    ///
    /// <para>An toàn tham số: mốc thời gian và loại bản tin đi bằng THAM SỐ. Riêng <c>SAMPLE BY {n}m</c> là
    /// literal (QuestDB không nhận bind ở vị trí đó) — giá trị là <c>int</c> đã chuẩn hoá, không phải chuỗi
    /// từ ngoài. Mọi truy vấn LUÔN có điều kiện <c>detectTime</c>: thiếu là quét cả bảng.</para>
    /// </summary>
    public static class CountQueryBuilder
    {
        public const string FromParameter = "@from";
        public const string ToParameter = "@to";
        public const string EventTypeParameterPrefix = "@eventType";

        public sealed record CountQuery(string Sql, List<SugarParameter> Parameters);

        /// <summary>Câu lệnh đếm cho cửa sổ <c>[fromUtc, toUtc)</c> với hạt <paramref name="periodMinutes"/> phút.</summary>
        public static CountQuery Build(
            ICountQueryFilter filter, int periodMinutes, DateTime fromUtc, DateTime toUtc)
        {
            ArgumentNullException.ThrowIfNull(filter);
            if (periodMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(periodMinutes), "Hạt thời gian phải lớn hơn 0.");

            var parameters = new List<SugarParameter>
            {
                new(FromParameter, fromUtc),
                new(ToParameter, toUtc),
            };

            var where = new StringBuilder(
                $"WHERE isDelete IS NULL AND detectTime >= {FromParameter} AND detectTime < {ToParameter}");

            var eventTypes = filter.EffectiveEventTypes;
            if (!filter.CountAllEventTypes && eventTypes.Count > 0)
            {
                var names = new List<string>(eventTypes.Count);
                for (var i = 0; i < eventTypes.Count; i++)
                {
                    var name = EventTypeParameterPrefix + i.ToString(CultureInfo.InvariantCulture);
                    names.Add(name);
                    parameters.Add(new SugarParameter(name, eventTypes[i]));
                }

                where.Append("\n  AND eventType IN (").Append(string.Join(", ", names)).Append(')');
            }

            if (filter.RequirePlate)
                where.Append("\n  AND licensePlate IS NOT NULL AND licensePlate != ''");

            foreach (var (column, name, value) in new[] {
                ("zoneId", "@zoneId", filter.ZoneId),
                ("direction", "@direction", filter.Direction),
                ("equipmentId", "@equipmentId", filter.EquipmentId) })
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                where.Append("\n  AND ").Append(column).Append(" = ").Append(name);
                parameters.Add(new SugarParameter(name, value.Trim()));
            }

            // count(speed) đếm giá trị KHÔNG NULL nên đúng là số mẫu tốc độ; count() đếm cả dòng thiếu tốc độ
            // và sẽ làm trọng số trung bình sai.
            var sql =
                $"""
                SELECT detectTime   AS BucketTime,
                       equipmentId  AS EquipmentId,
                       direction     AS Direction,
                       zoneId       AS ZoneId,
                       vehicleSize  AS VehicleSize,
                       count()       AS VehicleCount,
                       avg(speed)    AS AvgSpeed,
                       min(speed)    AS MinSpeed,
                       max(speed)    AS MaxSpeed,
                       count(speed)  AS SpeedSamples
                FROM VdsTrafficData
                {where}
                SAMPLE BY {periodMinutes.ToString(CultureInfo.InvariantCulture)}m ALIGN TO CALENDAR
                """;

            return new CountQuery(sql, parameters);
        }
    }
}
