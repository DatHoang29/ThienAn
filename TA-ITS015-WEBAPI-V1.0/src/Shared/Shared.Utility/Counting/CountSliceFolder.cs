namespace Shared.Utility.Counting
{
    /// <summary>
    /// Author: KienLT
    /// Description: Một ô kết quả thô của <c>SAMPLE BY</c> (trước khi gộp bốn nhóm cỡ xe) — khớp cột SELECT
    /// của <see cref="CountQueryBuilder"/>. Dùng chung cho worker và WebAPI để cùng một hình dạng dữ liệu.
    /// Created date: 03/09/2026
    /// </summary>
    public sealed class CountSampleRow
    {
        public DateTime BucketTime { get; set; }
        public string? EquipmentId { get; set; }
        public string? Direction { get; set; }
        public string? ZoneId { get; set; }
        public string? VehicleSize { get; set; }
        public int VehicleCount { get; set; }
        public double? AvgSpeed { get; set; }
        public double? MinSpeed { get; set; }
        public double? MaxSpeed { get; set; }
        public int SpeedSamples { get; set; }
    }

    /// <summary>
    /// Author: KienLT (chuyển sang TMS.Core dùng chung 03/09/2026)
    /// Description: Gộp các ô <c>SAMPLE BY</c> (<see cref="CountSampleRow"/>) của cùng một chu kỳ về MỘT ô
    /// bốn nhóm cỡ xe (<see cref="TrafficCountSlice"/>). Hàm THUẦN — không I/O, test được không cần DB.
    ///
    /// <para>Đây là chỗ DUY NHẤT gộp CountRow → slice: worker (<c>QuestCountReader</c>) và WebAPI
    /// (<c>VdsTrafficCountService</c>) cùng gọi để không ra số lệch.</para>
    /// </summary>
    public static class CountSliceFolder
    {
        /// <summary>Bộ lọc camera cho tầng gộp (rỗng = mọi camera).</summary>
        public interface IEquipmentScope
        {
            bool IsCountingEquipment(string? equipmentId);
        }

        /// <summary>
        /// Gộp rows về slices theo <paramref name="scopeType"/>
        /// (<see cref="TrafficCountScopes.Equipment"/> hoặc <see cref="TrafficCountScopes.Zone"/>).
        /// Bộ lọc camera áp cho CẢ HAI: camera bị loại khỏi phạm vi đếm thì cũng không góp số vào vùng.
        /// </summary>
        public static List<TrafficCountSlice> Fold(
            IEnumerable<CountSampleRow> rows, int periodMinutes, IEquipmentScope scope, string scopeType)
        {
            var byZone = scopeType == TrafficCountScopes.Zone;

            var grouped = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.EquipmentId))
                .Where(r => scope.IsCountingEquipment(r.EquipmentId))
                // Gộp theo vùng thì dòng CHƯA GÁN VÙNG phải bỏ: không có vùng để quy về.
                .Where(r => !byZone || !string.IsNullOrWhiteSpace(r.ZoneId))
                .GroupBy(r => (
                    r.BucketTime,
                    ScopeId: (byZone ? r.ZoneId! : r.EquipmentId!).Trim()));

            var slices = new List<TrafficCountSlice>();

            foreach (var group in grouped)
            {
                var shortCount = 0; var medium = 0; var longCount = 0; var other = 0;
                double speedSum = 0; var speedSamples = 0;
                var sizeSums = new double[4];
                var sizeSamples = new int[4];
                double? min = null; double? max = null;
                string? direction = null; string? zoneId = null;

                foreach (var row in group)
                {
                    switch (row.VehicleSize?.Trim().ToLowerInvariant())
                    {
                        case TrafficVehicleSizes.Short: shortCount += row.VehicleCount; break;
                        case TrafficVehicleSizes.Medium: medium += row.VehicleCount; break;
                        case TrafficVehicleSizes.Long: longCount += row.VehicleCount; break;
                        default: other += row.VehicleCount; break;
                    }

                    if (row.AvgSpeed is double avg && row.SpeedSamples > 0)
                    {
                        speedSum += avg * row.SpeedSamples;
                        speedSamples += row.SpeedSamples;
                        var index = row.VehicleSize?.Trim().ToLowerInvariant() switch { "short" => 0, "medium" => 1, "long" => 2, _ => 3 };
                        sizeSums[index] += avg * row.SpeedSamples;
                        sizeSamples[index] += row.SpeedSamples;
                    }

                    if (row.MinSpeed is double rowMin) min = min is null || rowMin < min ? rowMin : min;
                    if (row.MaxSpeed is double rowMax) max = max is null || rowMax > max ? rowMax : max;

                    direction ??= NullIfBlank(row.Direction);
                    zoneId ??= NullIfBlank(row.ZoneId);
                }

                // Ô rỗng hoàn toàn: SAMPLE BY có thể trả ô không xe khi bù khoảng trống.
                if (shortCount + medium + longCount + other == 0 && speedSamples == 0) continue;

                slices.Add(new TrafficCountSlice(
                    // Phạm vi VÙNG không mang hướng (một vùng có thể chứa camera cả hai hướng).
                    new TrafficCountScope(scopeType, group.Key.ScopeId, byZone ? null : direction),
                    group.Key.BucketTime,
                    group.Key.BucketTime.AddMinutes(periodMinutes),
                    shortCount, medium, longCount, other,
                    TrafficCountMath.Round(speedSamples == 0 ? null : speedSum / speedSamples),
                    TrafficCountMath.Round(min),
                    TrafficCountMath.Round(max),
                    speedSamples,
                    byZone ? group.Key.ScopeId : zoneId) {
                        ShortSpeed = new(sizeSums[0], sizeSamples[0]), MediumSpeed = new(sizeSums[1], sizeSamples[1]),
                        LongSpeed = new(sizeSums[2], sizeSamples[2]), OtherSpeed = new(sizeSums[3], sizeSamples[3])
                    });
            }

            return slices;
        }

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
