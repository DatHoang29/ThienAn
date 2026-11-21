using System.Globalization;

namespace Shared.Utility.Counting
{
    /// <summary>
    /// Một chuyến A→B ĐÃ HOÀN TẤT, đủ dữ kiện để tính tốc độ.
    ///
    /// <para>Là bản gọn của <c>TmsTrafficJourney</c> / <c>JourneyCandidate</c>. Khai riêng vì
    /// <c>Shared.Utility</c> KHÔNG tham chiếu project module nào (chỉ Shared.DTO + Shared.Reference), nên
    /// không thể nhận thẳng entity; bên gọi map vào đây. Đổi lại cả worker và WebAPI dùng chung một hàm.</para>
    /// </summary>
    /// <param name="SegmentId">Đoạn A→B (<c>TmsTrafficSegment.ID</c>) — chỉ dùng khi gom theo đoạn.</param>
    /// <param name="Direction">Hướng tuyến, khoá chính của số tổng quan; null/rỗng gom thành một nhóm riêng.</param>
    /// <param name="ToTime">Thời điểm xe TỚI camera cuối — mốc dùng để xếp chuyến vào cửa sổ.</param>
    /// <param name="TravelSeconds">Thời gian đi hết đoạn (giây).</param>
    /// <param name="DistanceMeter">Chiều dài đoạn lúc tính (mét) — copy sẵn trên từng chuyến.</param>
    /// <param name="IsValid">
    /// <c>TravelSeconds</c> nằm trong [Min, Max] của đoạn. Xem ghi chú về thiên lệch ở
    /// <see cref="SegmentSpeedMath"/> trước khi quyết định có loại chuyến không hợp lệ hay không.
    /// </param>
    public readonly record struct SegmentJourneySample(
        string? SegmentId,
        string? Direction,
        DateTime? ToTime,
        int? TravelSeconds,
        int? DistanceMeter,
        bool IsValid);

    /// <summary>Nguồn của con số đang hiển thị.</summary>
    public enum SegmentSpeedSource
    {
        /// <summary>Không có số nào để hiển thị: cửa sổ rỗng và cũng không có giá trị trước đó.</summary>
        None = 0,

        /// <summary>Đo được trong cửa sổ hiện tại.</summary>
        Window = 1,

        /// <summary>Cửa sổ không đủ xe nên giữ lại giá trị đo được gần nhất trước đó.</summary>
        Carried = 2,
    }

    /// <summary>
    /// Tốc độ tính được trong một cửa sổ thời gian.
    /// </summary>
    /// <param name="Direction">Hướng tuyến — khoá của số tổng quan.</param>
    /// <param name="SegmentId">
    /// Đoạn A→B khi gom theo đoạn; <c>null</c> khi đây là số của CẢ HƯỚNG (gom nhiều đoạn hoặc gom trực tiếp
    /// theo hướng).
    /// </param>
    /// <param name="SpaceMeanSpeedKmh">
    /// <b>Số dùng để hiển thị.</b> Tổng quãng đường chia tổng thời gian — xem ghi chú 1 của
    /// <see cref="SegmentSpeedMath"/>. <c>null</c> khi chưa đủ <c>minSamples</c> chuyến.
    /// </param>
    /// <param name="TimeMeanSpeedKmh">
    /// Trung bình CỘNG tốc độ từng xe. Để đối chiếu/chẩn đoán, KHÔNG dùng hiển thị. Luôn ≥
    /// <paramref name="SpaceMeanSpeedKmh"/>; chênh nhau nhiều nghĩa là tốc độ các xe phân tán mạnh.
    /// </param>
    /// <param name="SampleCount">
    /// Số chuyến đã dùng. Trả về kể cả khi dưới ngưỡng để bên gọi tự quyết định có lùi về nguồn khác không.
    /// </param>
    /// <param name="TotalDistanceMeter">Tổng quãng đường đã cộng (mét).</param>
    /// <param name="TotalTravelSeconds">Tổng thời gian đã cộng (giây).</param>
    /// <param name="FirstArrivalAt">Chuyến sớm nhất trong cửa sổ (theo <c>ToTime</c>).</param>
    /// <param name="LastArrivalAt">Chuyến muộn nhất trong cửa sổ — dùng để biết số liệu cũ bao lâu.</param>
    /// <param name="OutOfBoundsCount">
    /// Số chuyến bị loại vì <c>IsValid = false</c>. Bên gọi nên theo dõi số này: nó tăng vọt đúng lúc đường
    /// bị kẹt, xem ghi chú 2 của <see cref="SegmentSpeedMath"/>.
    /// </param>
    public readonly record struct SegmentSpeedResult(
        string? Direction,
        string? SegmentId,
        double? SpaceMeanSpeedKmh,
        double? TimeMeanSpeedKmh,
        int SampleCount,
        long TotalDistanceMeter,
        long TotalTravelSeconds,
        DateTime? FirstArrivalAt,
        DateTime? LastArrivalAt,
        int OutOfBoundsCount)
    {
        /// <summary>Đã đủ mẫu để có số tốc độ.</summary>
        public bool HasSpeed => SpaceMeanSpeedKmh is not null;

        /// <summary>
        /// Chuỗi ghi cột tốc độ kiểu chuỗi: một chữ số thập phân, không đơn vị, theo
        /// <c>InvariantCulture</c>.
        ///
        /// <para>Giữ đúng định dạng của <c>ZoneStatusSnapshot.AverageSpeedText</c> để thay được vào chỗ cũ mà
        /// bên đọc không phải sửa: máy chủ đặt locale Việt Nam sẽ ghi dấu phẩy thập phân và bên đọc số sẽ hiểu
        /// sai.</para>
        /// </summary>
        public string? SpaceMeanSpeedText =>
            SpaceMeanSpeedKmh is double v ? v.ToString("F1", CultureInfo.InvariantCulture) : null;
    }

    /// <summary>
    /// Giá trị tốc độ đo được gần nhất, do BÊN GỌI giữ và truyền lại.
    ///
    /// <para><see cref="SegmentSpeedMath"/> là hàm thuần nên không tự nhớ gì. Bên gọi lấy giá trị này từ nơi
    /// nó vốn đã lưu (cột trong DB, hoặc RAM) rồi truyền vào để hàm quyết định giữ hay thay.</para>
    /// </summary>
    /// <param name="SpeedKmh">Tốc độ đã đo được.</param>
    /// <param name="MeasuredAt">
    /// Thời điểm của lần đo đó, nên dùng <see cref="SegmentSpeedResult.LastArrivalAt"/> (xe cuối cùng tới
    /// camera cuối) chứ KHÔNG phải lúc ghi DB: số liệu già bao lâu tính từ lúc có xe thật.
    /// </param>
    public readonly record struct SegmentSpeedCarry(double SpeedKmh, DateTime MeasuredAt);

    /// <summary>
    /// Con số cuối cùng để hiển thị, kèm nguồn và tuổi của nó.
    /// </summary>
    /// <param name="Direction">Hướng tuyến.</param>
    /// <param name="SegmentId">Đoạn, hoặc <c>null</c> khi là số của cả hướng.</param>
    /// <param name="SpeedKmh">Số để hiển thị; <c>null</c> khi <see cref="Source"/> là <c>None</c>.</param>
    /// <param name="Source">Đo trong cửa sổ, giữ từ trước, hay không có gì.</param>
    /// <param name="MeasuredAt">Thời điểm xe cuối cùng của lần đo đang được dùng.</param>
    /// <param name="Age">Số liệu già bao lâu so với <c>now</c>; <c>null</c> khi không có số.</param>
    /// <param name="IsStale">
    /// <c>true</c> khi số đang hiển thị KHÔNG phải của cửa sổ hiện tại. FE nên hiện kèm mốc thời gian thay vì
    /// để người trực hiểu đây là số hiện thời.
    /// </param>
    /// <param name="Window">
    /// Số liệu cửa sổ hiện tại, trả về KỂ CẢ khi rỗng — để bên gọi thấy có bao nhiêu chuyến và bao nhiêu
    /// chuyến bị loại vì ngoài biên, chứ không chỉ thấy con số cuối.
    /// </param>
    public readonly record struct SegmentSpeedDisplay(
        string? Direction,
        string? SegmentId,
        double? SpeedKmh,
        SegmentSpeedSource Source,
        DateTime? MeasuredAt,
        TimeSpan? Age,
        bool IsStale,
        SegmentSpeedResult Window)
    {
        /// <summary>Có số để hiển thị.</summary>
        public bool HasSpeed => SpeedKmh is not null;

        /// <summary>Chuỗi ghi cột, cùng định dạng <see cref="SegmentSpeedResult.SpaceMeanSpeedText"/>.</summary>
        public string? SpeedText =>
            SpeedKmh is double v ? v.ToString("F1", CultureInfo.InvariantCulture) : null;

        /// <summary>Giá trị để truyền lại cho lượt sau; <c>null</c> khi chưa có số nào để giữ.</summary>
        public SegmentSpeedCarry? ToCarry() =>
            SpeedKmh is double v && MeasuredAt is DateTime at ? new SegmentSpeedCarry(v, at) : null;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Tốc độ trung bình suy từ THỜI GIAN ĐI HẾT ĐOẠN của từng lượt xe, gom theo HƯỚNG tuyến.
    /// Hàm THUẦN: không I/O, không trạng thái, không DI ⇒ kiểm được trực tiếp.
    ///
    /// <para><b>Dùng cho số TỔNG QUAN của cả tuyến theo hướng.</b> Không phải tốc độ tại một camera VDS, cũng
    /// không phải tốc độ của một vùng: khoá duy nhất là <c>Direction</c> (hướng 1 / hướng 2). Đây là hàm
    /// chuẩn bị sẵn để thay cho tốc độ TỨC THỜI hiện dùng — <c>ZoneStatusTracker</c> đang lấy trung bình cộng
    /// các mẫu tốc độ điểm mà camera đo được tại một vị trí (<c>Samples.Average(s =&gt; s.Speed)</c>). Một xe
    /// có thể vọt qua camera ở 60 km/h rồi dừng 3 phút giữa đoạn, nên mẫu điểm không mô tả được tuyến.
    /// CHƯA ĐƯỢC NỐI VÀO luồng chạy.</para>
    ///
    /// <para><b>0. Nhịp tính: MỖI LƯỢT XE tính lại.</b> Hàm thuần nên bên gọi cứ gọi lại mỗi khi có một chuyến
    /// mới hoàn tất (xe vừa tới camera cuối): con số trả về là chuyến đó CỘNG DỒN với các chuyến trong 30 phút
    /// trước nó. Không phải 30 phút mới phát một lần, cũng không phải phát riêng tốc độ của chuyến vừa xong.
    /// Lưu ý nếu nối vào đường ghi bảng hiện tại: <c>ZoneStatusTracker</c> còn chặn nhịp ghi bằng
    /// <c>MinWriteIntervalSeconds</c> — muốn phát theo từng lượt xe thì phải xem lại chốt đó, nó không thuộc
    /// hàm này.</para>
    ///
    /// <para><b>1. Vì sao là tổng-quãng-đường / tổng-thời-gian, KHÔNG phải trung bình cộng tốc độ từng xe.</b>
    /// Đây là <i>space-mean speed</i>, đại lượng đúng để mô tả một tuyến đường; trung bình cộng
    /// (<i>time-mean speed</i>) luôn cho số CAO HƠN và cao lệch nhất đúng lúc cần chính xác nhất. Ví dụ hai
    /// xe qua đoạn 1000 m: xe thứ nhất 80 km/h (45 s), xe thứ hai 10 km/h (360 s).
    /// Trung bình cộng = 45,0 km/h — đọc ra "đi được". Tổng/tổng = 2000 m / 405 s = 17,8 km/h — đúng thực tế
    /// là đang tắc. Vì mọi xe đi cùng một chiều dài, công thức này đúng bằng trung bình ĐIỀU HOÀ của tốc độ
    /// từng xe. Cả hai số đều được trả về, nhưng chỉ
    /// <see cref="SegmentSpeedResult.SpaceMeanSpeedKmh"/> dùng để hiển thị.</para>
    ///
    /// <para><b>2. Bẫy: loại chuyến <c>IsValid = false</c> làm số ĐẸP LÊN đúng lúc đường tắc.</b>
    /// <c>IsValid = false</c> nghĩa là <c>TravelSeconds</c> ra ngoài [Min, Max] của đoạn, mà nguyên nhân
    /// thường gặp nhất của việc VƯỢT <c>Max</c> chính là kẹt xe hoặc xe dừng lâu. Loại hết nhóm đó đi thì
    /// còn lại toàn xe chạy thoáng, và tuyến đang tắc vẫn báo tốc độ cao. Mặc định
    /// <c>includeOutOfBounds = false</c> để đồng bộ với chỗ khác trong repo (tầng nâng sự cố không dùng
    /// chuyến không hợp lệ, vì nguyên nhân còn lại là hai xe khác nhau bị đọc trùng biển). Khi nối vào phải
    /// chốt lại có ý thức: theo dõi <see cref="SegmentSpeedResult.OutOfBoundsCount"/>, và nếu cần phản ánh
    /// kẹt xe thì bật <c>includeOutOfBounds</c> hoặc nới <c>MaxTravelSeconds</c> của đoạn.</para>
    ///
    /// <para><b>3. Cửa sổ 30 phút lọc theo <c>ToTime</c>, nửa mở <c>[from, to)</c>.</b> Một chuyến chỉ tồn tại
    /// khi xe đã tới camera cuối, nên mốc xếp nó vào cửa sổ là lúc TỚI. Nửa mở để gọi nhiều cửa sổ liên tiếp
    /// trên cùng tập dữ liệu không đếm trùng biên — cùng quy ước với <see cref="TrafficCountMath.Sum"/>.</para>
    ///
    /// <para><b>4. KHÔNG dùng <c>TmsTrafficJourney.AvgSpeedKmh</c> đã lưu.</b> Cột đó đã làm tròn, cộng dồn
    /// lại là làm tròn hai lần. Tính lại từ <c>DistanceMeter</c> và <c>TravelSeconds</c> cho ra đúng
    /// space-mean, và cũng là cách duy nhất để tổng/tổng có nghĩa.</para>
    ///
    /// <para><b>5. Hai cách lấy số cho cả hướng — phải chốt.</b>
    /// <list type="number">
    /// <item><b>Một đoạn dài suốt tuyến</b>: khai một <c>TmsTrafficSegment</c> mỗi hướng, camera đầu → camera
    /// cuối, khoảng cách cả tuyến. Gọi <see cref="ByRouteAt"/> là xong. Đơn giản, nhưng chuyến chỉ thành hình
    /// khi biển số đọc được ở CẢ HAI đầu tuyến, nên số mẫu ít, và thời gian đi hết tuyến dài (1–2 giờ) khiến
    /// con số mô tả quá khứ chứ không phải hiện tại — xe vừa tới đích bắt đầu đi từ trước đó rất lâu.</item>
    /// <item><b>Nối chuỗi các đoạn liền kề</b>: khai nhiều đoạn ngắn giữa các camera cạnh nhau, gọi
    /// <see cref="BySegmentAt"/> rồi <see cref="Combine"/> để ra một số cho cả hướng. Nhiều mẫu hơn, số liệu
    /// mới hơn (đoạn ngắn thì thời gian đi ngắn), và xe chỉ đi một phần tuyến vẫn góp mẫu. Đổi lại phải khai
    /// nhiều đoạn.</item>
    /// </list>
    /// Cách 2 là cách thường dùng cho màn hình tổng quan. Cả hai đều ra <see cref="SegmentSpeedResult"/> có
    /// <c>SegmentId = null</c>, nên tầng hiển thị không phân biệt.</para>
    ///
    /// <para><b>6. Giữ giá trị cũ khi cửa sổ rỗng — và vì sao phải trả kèm tuổi.</b> Hướng vắng xe 30 phút thì
    /// không có gì để tính, nên <see cref="Display"/> giữ lại giá trị đo gần nhất do bên gọi truyền vào
    /// (<see cref="SegmentSpeedCarry"/>). Rủi ro là lúc 3 giờ sáng đường không có xe, màn hình vẫn hiện tốc độ
    /// đo từ 6 tiếng trước như thể đang thực. Vì vậy kết quả LUÔN mang
    /// <see cref="SegmentSpeedDisplay.IsStale"/> và <see cref="SegmentSpeedDisplay.Age"/>: FE phải hiện kèm
    /// mốc thời gian. Muốn chặn cứng thì đặt <c>maxCarrySeconds</c> — mặc định 0 là KHÔNG giới hạn, giữ đúng
    /// yêu cầu "hết xe thì lấy số trước đó".</para>
    ///
    /// <para><b>7. Cấu hình camera đầu/cuối của mỗi hướng: dùng <c>TmsTrafficSegment</c>, đừng thêm enum hay
    /// hằng mới.</b> Bảng đó đã có đúng bốn thứ cần: <c>FromEquipmentId</c>, <c>ToEquipmentId</c>,
    /// <c>Direction</c>, <c>DistanceMeter</c>. Thêm một bộ mã cứng trong code sẽ tạo nguồn sự thật thứ hai,
    /// và <c>JourneyMatcher</c> vốn đã ghép chuyến theo đúng bảng này — khai chỗ khác thì chuyến không bao giờ
    /// sinh ra.</para>
    /// Created date: 25/09/2026
    /// </summary>
    public static class SegmentSpeedMath
    {
        /// <summary>
        /// Số chuyến tối thiểu để tin con số. Một chuyến duy nhất là một cái xe, không phải tình trạng tuyến:
        /// một xe dừng ăn trưa giữa đường sẽ kéo tốc độ xuống gần 0.
        /// </summary>
        public const int DefaultMinSamples = 3;

        /// <summary>Độ dài cửa sổ tính trung bình, tính tại camera cuối: 30 phút gần nhất.</summary>
        public const int DefaultWindowMinutes = 30;

        /// <summary>
        /// Trần tuổi của giá trị được giữ lại, giây. <c>0</c> = KHÔNG giới hạn — giữ mãi tới khi có xe mới,
        /// đúng yêu cầu nghiệp vụ; bù lại phải đọc <see cref="SegmentSpeedDisplay.IsStale"/> khi hiển thị.
        /// </summary>
        public const int DefaultMaxCarrySeconds = 0;

        /// <summary>Mét/giây sang km/h.</summary>
        private const double MsToKmh = 3.6;

        /// <summary>
        /// Cửa sổ <c>[now - windowMinutes, now)</c>.
        ///
        /// <para>KHÔNG làm tròn xuống lưới chu kỳ như <see cref="TrafficCountMath.SlidingWindow"/>: hàm kia
        /// gộp các ô đếm 5 phút nên phải snap theo lưới ô, còn ở đây từng chuyến là một sự kiện có mốc thời
        /// gian riêng, snap chỉ làm cửa sổ dài thêm 0–5 phút mà không được gì.</para>
        /// </summary>
        public static (DateTime From, DateTime To) Window(DateTime now, int windowMinutes = DefaultWindowMinutes)
            => (now.AddMinutes(-Math.Max(1, windowMinutes)), now);

        // ==================== Gom theo HƯỚNG (số tổng quan cả tuyến) ====================

        /// <summary>
        /// Tốc độ của MỖI HƯỚNG trong cửa sổ, bỏ qua chuyến thuộc đoạn nào. Kết quả có
        /// <c>SegmentId = null</c>.
        /// </summary>
        public static List<SegmentSpeedResult> ByRoute(
            IEnumerable<SegmentJourneySample> journeys,
            DateTime from,
            DateTime to,
            int minSamples = DefaultMinSamples,
            bool includeOutOfBounds = false)
            => Accumulate(journeys, from, to, minSamples, includeOutOfBounds, groupBySegment: false);

        /// <summary>Như <see cref="ByRoute"/> nhưng tự lấy cửa sổ 30 phút gần nhất tính đến <paramref name="now"/>.</summary>
        public static List<SegmentSpeedResult> ByRouteAt(
            IEnumerable<SegmentJourneySample> journeys,
            DateTime now,
            int windowMinutes = DefaultWindowMinutes,
            int minSamples = DefaultMinSamples,
            bool includeOutOfBounds = false)
        {
            var (from, to) = Window(now, windowMinutes);
            return ByRoute(journeys, from, to, minSamples, includeOutOfBounds);
        }

        /// <summary>
        /// Tốc độ của MỘT hướng. <c>null</c> khi hướng đó không có chuyến nào trong cửa sổ.
        ///
        /// <para><paramref name="direction"/> để null/rỗng nghĩa là lấy nhóm chuyến KHÔNG khai hướng, không
        /// phải "mọi hướng" — gộp hai chiều làm một là che mất chiều đang tắc.</para>
        /// </summary>
        public static SegmentSpeedResult? ForRoute(
            IEnumerable<SegmentJourneySample> journeys,
            string? direction,
            DateTime from,
            DateTime to,
            int minSamples = DefaultMinSamples,
            bool includeOutOfBounds = false)
        {
            var wanted = direction?.Trim() ?? string.Empty;
            foreach (var row in ByRoute(journeys, from, to, minSamples, includeOutOfBounds))
                if (string.Equals(row.Direction ?? string.Empty, wanted, StringComparison.OrdinalIgnoreCase))
                    return row;

            return null;
        }

        /// <summary>
        /// Đường dùng đủ một lượt cho MỘT hướng: cửa sổ 30 phút gần nhất, không đủ xe thì giữ giá trị cũ.
        /// Gọi lại hàm này mỗi khi có một chuyến mới hoàn tất.
        ///
        /// <para>Hướng không có chuyến nào vẫn trả kết quả (không trả null) để bên gọi luôn có
        /// <see cref="SegmentSpeedDisplay.Window"/> mà đọc số mẫu — phân biệt được "vắng xe" với "sai mã hướng".</para>
        /// </summary>
        public static SegmentSpeedDisplay ResolveRoute(
            IEnumerable<SegmentJourneySample> journeys,
            string? direction,
            DateTime now,
            SegmentSpeedCarry? previous = null,
            int windowMinutes = DefaultWindowMinutes,
            int minSamples = DefaultMinSamples,
            bool includeOutOfBounds = false,
            int maxCarrySeconds = DefaultMaxCarrySeconds)
        {
            var (from, to) = Window(now, windowMinutes);
            var window = ForRoute(journeys, direction, from, to, minSamples, includeOutOfBounds)
                         ?? EmptyRoute(direction);
            return Display(window, now, previous, maxCarrySeconds);
        }

        /// <summary>Số liệu rỗng của một hướng — cửa sổ không có chuyến nào.</summary>
        public static SegmentSpeedResult EmptyRoute(string? direction)
        {
            var dir = direction?.Trim();
            return new SegmentSpeedResult(
                string.IsNullOrEmpty(dir) ? null : dir, null, null, null, 0, 0, 0, null, null, 0);
        }

        // ==================== Gom theo ĐOẠN (để nối chuỗi cả tuyến) ====================

        /// <summary>
        /// Tốc độ của mỗi cặp (đoạn, hướng) trong cửa sổ — dùng cho cách 2 ở ghi chú 5: tính từng đoạn ngắn
        /// rồi <see cref="Combine"/> thành số của cả hướng.
        /// </summary>
        public static List<SegmentSpeedResult> BySegment(
            IEnumerable<SegmentJourneySample> journeys,
            DateTime from,
            DateTime to,
            int minSamples = DefaultMinSamples,
            bool includeOutOfBounds = false)
            => Accumulate(journeys, from, to, minSamples, includeOutOfBounds, groupBySegment: true);

        /// <summary>Như <see cref="BySegment"/> nhưng tự lấy cửa sổ 30 phút gần nhất.</summary>
        public static List<SegmentSpeedResult> BySegmentAt(
            IEnumerable<SegmentJourneySample> journeys,
            DateTime now,
            int windowMinutes = DefaultWindowMinutes,
            int minSamples = DefaultMinSamples,
            bool includeOutOfBounds = false)
        {
            var (from, to) = Window(now, windowMinutes);
            return BySegment(journeys, from, to, minSamples, includeOutOfBounds);
        }

        /// <summary>
        /// Gộp nhiều đoạn thành MỘT số cho cả hướng. Kết quả có <c>SegmentId = null</c>.
        ///
        /// <para>Cộng tiếp tổng quãng đường và tổng thời gian rồi chia, KHÔNG lấy trung bình cộng các
        /// <see cref="SegmentSpeedResult.SpaceMeanSpeedKmh"/>: trung bình của các trung bình sẽ cho đoạn ngắn
        /// vài chuyến cùng trọng số với đoạn dài hàng trăm chuyến, và lại rơi vào đúng thiên lệch mà ghi chú
        /// 1 nói tới.</para>
        ///
        /// <para>Nhận cả phần tử chưa đủ <c>minSamples</c>: mỗi đoạn lẻ có thể thiếu mẫu nhưng cộng lại thì
        /// đủ. Ngưỡng được áp MỘT LẦN trên tổng, qua <paramref name="minSamples"/>.</para>
        /// </summary>
        public static SegmentSpeedResult? Combine(
            IEnumerable<SegmentSpeedResult> parts,
            string? direction = null,
            int minSamples = DefaultMinSamples)
        {
            ArgumentNullException.ThrowIfNull(parts);

            var acc = new Accumulator(direction, null);
            var any = false;

            foreach (var p in parts)
            {
                any = true;
                acc.OutOfBounds += p.OutOfBoundsCount;
                if (p.SampleCount <= 0) continue;

                acc.Count += p.SampleCount;
                acc.Meters += p.TotalDistanceMeter;
                acc.Seconds += p.TotalTravelSeconds;
                acc.TimeMeanSum += (p.TimeMeanSpeedKmh ?? 0) * p.SampleCount;
                acc.Touch(p.FirstArrivalAt);
                acc.Touch(p.LastArrivalAt);
            }

            return any ? acc.ToResult(minSamples) : null;
        }

        // ==================== Chốt số để hiển thị ====================

        /// <summary>
        /// Chốt con số để hiển thị từ số liệu cửa sổ và giá trị cũ: đủ mẫu thì lấy cửa sổ, không thì GIỮ giá
        /// trị cũ, không có cả hai thì <c>None</c>.
        ///
        /// <para>Tách riêng khỏi <see cref="ResolveRoute"/> để dùng được sau <see cref="Combine"/>.</para>
        /// </summary>
        /// <param name="window">Số liệu cửa sổ, lấy từ <see cref="ByRouteAt"/> hoặc <see cref="Combine"/>.</param>
        /// <param name="now">Giờ hiện tại, để tính tuổi số liệu.</param>
        /// <param name="previous">Giá trị đo gần nhất do bên gọi giữ; <c>null</c> khi chưa từng có.</param>
        /// <param name="maxCarrySeconds">Trần tuổi của giá trị giữ lại; <c>0</c> = không giới hạn.</param>
        public static SegmentSpeedDisplay Display(
            SegmentSpeedResult window,
            DateTime now,
            SegmentSpeedCarry? previous = null,
            int maxCarrySeconds = DefaultMaxCarrySeconds)
        {
            if (window.SpaceMeanSpeedKmh is double fresh)
            {
                var at = window.LastArrivalAt;
                return new SegmentSpeedDisplay(
                    window.Direction, window.SegmentId, fresh, SegmentSpeedSource.Window,
                    at, at is DateTime t ? now - t : null, IsStale: false, window);
            }

            if (previous is SegmentSpeedCarry carry)
            {
                var age = now - carry.MeasuredAt;

                // Tuổi âm (giá trị cũ mang mốc ở tương lai, thường do lệch giờ giữa hai tiến trình) vẫn được
                // dùng nhưng KHÔNG bị trần cắt: cắt theo tuổi âm sẽ xoá sạch số liệu mỗi lần lệch giờ.
                var expired = maxCarrySeconds > 0 && age.TotalSeconds > maxCarrySeconds;
                if (!expired)
                    return new SegmentSpeedDisplay(
                        window.Direction, window.SegmentId, carry.SpeedKmh, SegmentSpeedSource.Carried,
                        carry.MeasuredAt, age, IsStale: true, window);
            }

            return new SegmentSpeedDisplay(
                window.Direction, window.SegmentId, null, SegmentSpeedSource.None,
                null, null, IsStale: false, window);
        }

        // ==================== Bên trong ====================

        /// <summary>Gom chuyến vào nhóm rồi cộng dồn; <paramref name="groupBySegment"/> chọn khoá nhóm.</summary>
        private static List<SegmentSpeedResult> Accumulate(
            IEnumerable<SegmentJourneySample> journeys,
            DateTime from,
            DateTime to,
            int minSamples,
            bool includeOutOfBounds,
            bool groupBySegment)
        {
            ArgumentNullException.ThrowIfNull(journeys);
            var result = new List<SegmentSpeedResult>();
            if (to <= from) return result;

            var groups = new Dictionary<(string Direction, string Segment), Accumulator>();

            foreach (var j in journeys)
            {
                if (j.ToTime is not DateTime arrival) continue;
                if (arrival < from || arrival >= to) continue;

                // Gom theo đoạn thì chuyến thiếu mã đoạn là dữ liệu hỏng; gom theo hướng thì mã đoạn không
                // liên quan, chuyến vẫn dùng được.
                var segment = j.SegmentId?.Trim() ?? string.Empty;
                if (groupBySegment && segment.Length == 0) continue;

                var direction = j.Direction?.Trim() ?? string.Empty;
                var key = (direction.ToUpperInvariant(), groupBySegment ? segment.ToUpperInvariant() : string.Empty);

                if (!groups.TryGetValue(key, out var acc))
                {
                    acc = new Accumulator(
                        direction.Length == 0 ? null : direction,
                        groupBySegment ? segment : null);
                    groups[key] = acc;
                }

                // Chuyến ngoài biên được ĐẾM RIÊNG kể cả khi bị loại: bên gọi cần thấy nó tồn tại để biết
                // con số còn lại đã bị lọc mất bao nhiêu bằng chứng kẹt xe.
                if (!j.IsValid)
                {
                    acc.OutOfBounds++;
                    if (!includeOutOfBounds) continue;
                }

                // Thiếu hoặc phi lý ⇒ bỏ hẳn, không đếm vào OutOfBounds: đây là dữ liệu hỏng, khác với
                // "thời gian đi bất thường".
                if (j.TravelSeconds is not int seconds || seconds <= 0) continue;
                if (j.DistanceMeter is not int meters || meters <= 0) continue;

                acc.Add(meters, seconds, arrival);
            }

            foreach (var acc in groups.Values) result.Add(acc.ToResult(minSamples));
            return result;
        }

        /// <summary>Bộ cộng dồn của một nhóm; tách riêng để hai đường (gom chuyến, gộp đoạn) dùng chung.</summary>
        private sealed class Accumulator(string? direction, string? segmentId)
        {
            public int Count;
            public long Meters;
            public long Seconds;

            /// <summary>Tổng tốc độ từng xe, để tính trung bình CỘNG phục vụ đối chiếu.</summary>
            public double TimeMeanSum;

            public int OutOfBounds;
            private DateTime? _first;
            private DateTime? _last;

            public void Add(int meters, int seconds, DateTime arrival)
            {
                Count++;
                Meters += meters;
                Seconds += seconds;
                TimeMeanSum += meters / (double)seconds * MsToKmh;
                Touch(arrival);
            }

            public void Touch(DateTime? at)
            {
                if (at is not DateTime v) return;
                if (_first is null || v < _first) _first = v;
                if (_last is null || v > _last) _last = v;
            }

            public SegmentSpeedResult ToResult(int minSamples)
            {
                var enough = Count > 0 && Count >= Math.Max(1, minSamples);
                var space = enough && Seconds > 0
                    ? TrafficCountMath.Round(Meters / (double)Seconds * MsToKmh)
                    : null;
                var time = enough ? TrafficCountMath.Round(TimeMeanSum / Count) : null;

                return new SegmentSpeedResult(
                    Direction: direction,
                    SegmentId: segmentId,
                    SpaceMeanSpeedKmh: space,
                    TimeMeanSpeedKmh: time,
                    SampleCount: Count,
                    TotalDistanceMeter: Meters,
                    TotalTravelSeconds: Seconds,
                    FirstArrivalAt: _first,
                    LastArrivalAt: _last,
                    OutOfBoundsCount: OutOfBounds);
            }
        }
    }
}
