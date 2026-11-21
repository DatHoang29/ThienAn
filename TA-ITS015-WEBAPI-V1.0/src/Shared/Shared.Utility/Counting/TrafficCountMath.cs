namespace Shared.Utility.Counting
{
    /// <summary>
    /// Author: KienLT
    /// Description: TOÀN BỘ phép tính của tầng đếm xe, dạng HÀM THUẦN. Đây là chỗ duy nhất định nghĩa
    /// "một giờ gần nhất", "24h gần nhất", cách gộp ô và cách tính tốc độ trung bình — VDS và WebAPI gọi
    /// cùng một hàm nên hai bên không thể ra số khác nhau.
    ///
    /// Không I/O, không trạng thái, không phụ thuộc DI ⇒ test trực tiếp.
    /// Created date: 04/08/2026
    /// </summary>
    public static class TrafficCountMath
    {
        /// <summary>Hạt mặc định của bảng đếm: 5 phút.</summary>
        public const int DefaultPeriodMinutes = 5;

        /// <summary>Làm tròn xuống lưới chu kỳ (chu kỳ chia đều trong giờ nên không lệch qua giờ khác).</summary>
        public static DateTime FloorPeriod(DateTime t, int periodMinutes = DefaultPeriodMinutes)
        {
            var period = NormalizePeriod(periodMinutes);
            return new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute / period * period, 0, t.Kind);
        }

        public static DateTime FloorHour(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, 0, 0, t.Kind);

        /// <summary>
        /// Chu kỳ phải là bội số của 5 và tối thiểu 5 — hạt nhỏ nhất mà <c>SAMPLE BY</c> được gọi với.
        /// </summary>
        public static int NormalizePeriod(int periodMinutes)
        {
            if (periodMinutes <= DefaultPeriodMinutes) return DefaultPeriodMinutes;
            var remainder = periodMinutes % DefaultPeriodMinutes;
            return remainder == 0 ? periodMinutes : periodMinutes + (DefaultPeriodMinutes - remainder);
        }

        /// <summary>
        /// Cửa sổ TRƯỢT dài <paramref name="length"/> tính đến <paramref name="now"/>, mốc đầu làm tròn
        /// xuống lưới chu kỳ. Bước trượt = chu kỳ (5 phút) nên khoảng thực tế dài 60–65 phút với cửa sổ
        /// 1 giờ; bên gọi trả kèm from/to ra ngoài để bên nhận biết chính xác khoảng đã tính.
        ///
        /// Vì sao không trượt theo giây: số liệu nhỏ nhất mà tầng đếm giữ là ô 5 phút; muốn mịn hơn thì
        /// phải giữ từng lượt xe trong RAM, mà nhịp phát/nhịp gọi API đều là phút nên không đáng.
        /// </summary>
        public static (DateTime From, DateTime To) SlidingWindow(
            DateTime now, TimeSpan length, int periodMinutes = DefaultPeriodMinutes)
            => (FloorPeriod(now - length, periodMinutes), now);

        /// <summary>
        /// Gộp các ô có <c>From</c> trong <c>[from, to)</c> thành một khối. Ô ngoài khoảng bị bỏ, nên gọi
        /// được nhiều lần trên cùng một tập ô cho nhiều cửa sổ khác nhau.
        /// </summary>
        public static TrafficCountBlock Sum(IEnumerable<TrafficCountSlice> slices, DateTime from, DateTime to)
        {
            var shortCount = 0; var medium = 0; var longCount = 0; var other = 0;
            double speedSum = 0; var speedWeight = 0;
            double? min = null; double? max = null;
            var any = false;

            foreach (var s in slices)
            {
                if (s.From < from || s.From >= to) continue;
                any = true;
                shortCount += s.Short; medium += s.Medium; longCount += s.Long; other += s.Other;

                if (s.AvgSpeed is double avg && s.SpeedSampleCount > 0)
                {
                    speedSum += avg * s.SpeedSampleCount;
                    speedWeight += s.SpeedSampleCount;
                }
                if (s.MinSpeed is double mn) min = min is null || mn < min ? mn : min;
                if (s.MaxSpeed is double mx) max = max is null || mx > max ? mx : max;
            }

            if (!any) return TrafficCountBlock.Empty;

            return new TrafficCountBlock(
                shortCount, medium, longCount, other, shortCount + medium + longCount + other,
                Round(speedWeight == 0 ? null : speedSum / speedWeight), Round(min), Round(max));
        }

        /// <summary>
        /// Chuỗi theo GIỜ trong <c>[fromHour, toHour]</c> (bao gồm giờ cuối), BÙ 0 cho giờ không có xe —
        /// bên nhận vẽ chart không phải tự lấp lỗ, và không phân biệt được "không có dữ liệu" với "không
        /// có xe" là chuyện của nghiệp vụ, không phải của FE.
        /// </summary>
        public static List<TrafficCountHour> ByHour(
            IEnumerable<TrafficCountSlice> slices, DateTime fromHour, DateTime toHour)
        {
            var list = slices as IReadOnlyCollection<TrafficCountSlice> ?? slices.ToList();
            var result = new List<TrafficCountHour>();

            for (var hour = FloorHour(fromHour); hour <= FloorHour(toHour); hour = hour.AddHours(1))
                result.Add(new TrafficCountHour(hour, Sum(list, hour, hour.AddHours(1))));

            return result;
        }

        /// <summary>Trung bình có trọng số; trọng số 0 hoặc giá trị null bị bỏ. Rỗng ⇒ null.</summary>
        public static double? WeightedAverage(IEnumerable<(double? Value, int Weight)> samples)
        {
            double sum = 0; var weight = 0;
            foreach (var (value, w) in samples)
            {
                if (value is not double v || w <= 0) continue;
                sum += v * w; weight += w;
            }
            return weight == 0 ? null : Round(sum / weight);
        }

        /// <summary>Tốc độ hiển thị 1 số lẻ; giữ null khi không có mẫu.</summary>
        public static double? Round(double? value) => value is double v ? Math.Round(v, 1) : null;
    }

    /// <summary>Một điểm của chuỗi theo giờ: mốc ĐẦU giờ + số liệu của giờ đó.</summary>
    public readonly record struct TrafficCountHour(DateTime Hour, TrafficCountBlock Block);
}
