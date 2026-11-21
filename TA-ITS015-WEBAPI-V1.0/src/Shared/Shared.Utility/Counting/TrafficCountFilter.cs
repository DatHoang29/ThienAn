namespace Shared.Utility.Counting
{
    /// <summary>
    /// Author: KienLT
    /// Description: Quyết định NHỮNG LOẠI BẢN TIN nào được tính vào số đếm. Dùng chung để VDS và WebAPI
    /// không thể lệch tiêu chí — lệch tiêu chí là lệch số, mà lệch số thì rất khó truy.
    ///
    /// <para>Từ 12/09/2026 việc lọc được đẩy vào câu lệnh <c>SAMPLE BY</c> (<see cref="CountQueryBuilder"/>
    /// dựng mệnh đề <c>eventType IN (...)</c>), không còn xét từng bản tin trong RAM. Vì vậy lớp này chỉ còn
    /// giữ phần khai báo loại; các hàm xét-một-bản-tin của bộ đếm cũ đã bỏ.</para>
    ///
    /// Mặc định: chỉ lượt xe bình thường.
    ///
    /// <para><b>ĐÃ BỎ tên cũ <c>Regular</c> (22/09/2026).</b> Đặc tả API VDS đổi tên loại lượt xe từ
    /// <c>Regular</c> (v1.0) sang <c>Normal</c> (v1.2+); trước đây bộ lọc nhận CẢ HAI để không hụt bản ghi
    /// lịch sử. Người dùng xác nhận dữ liệu không còn dòng <c>Regular</c>, nên bỏ tên cũ để phía đọc chỉ có
    /// MỘT tiêu chí.</para>
    /// <para>An toàn được vì phía NHẬP vẫn quy đổi: <c>Services.Vds/Shared/Domain/VdsEventTypes.Canonical</c>
    /// đổi <c>Regular</c> → <c>Normal</c> ngay tại cửa vào, nên bản ghi mới không bao giờ mang tên cũ. Nếu
    /// sau này bỏ luôn quy đổi đó thì payload v1.0 sẽ lọt xuống DB dưới tên <c>Regular</c> và không còn nhánh
    /// nào đếm nó.</para>
    /// Created date: 04/08/2026
    /// </summary>
    public sealed class TrafficCountFilter
    {
        /// <summary>Lượt xe đi qua bình thường theo đặc tả API VDS v1.3 mục 2.2 ID 1.</summary>
        public const string NormalEventType = "Normal";

        /// <summary>Loại bản tin mặc định được đếm khi <see cref="CountedEventTypes"/> rỗng.</summary>
        public static readonly IReadOnlyList<string> DefaultEventTypes = new[] { NormalEventType };

        /// <summary>
        /// Các cặp tên cũ ↔ tên mới của đặc tả VDS; khai một tên thì bộ lọc nhận cả cặp.
        ///
        /// <para>Cặp <c>Regular</c> ↔ <c>Normal</c> đã bỏ 22/09/2026. Hai cặp còn lại là tên của LOẠI SỰ CỐ
        /// (không phải lượt xe bình thường) nên giữ nguyên.</para>
        /// </summary>
        private static readonly Dictionary<string, string> RenamedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Stopped Suddenly"] = "Stopped Vehicle",
            ["Stopped Vehicle"] = "Stopped Suddenly",
            ["Abandoned Object"] = "Fallen Object",
            ["Fallen Object"] = "Abandoned Object"
        };

        /// <summary>
        /// Loại bản tin được đếm. Rỗng ⇒ <see cref="DefaultEventTypes"/>.
        /// Bản tin sự kiện (Speeding/Congestion/...) sinh THÊM trên cùng lượt xe nên đếm cả là đếm trùng.
        /// </summary>
        public List<string> CountedEventTypes { get; set; } = new();

        /// <summary>true ⇒ bỏ qua <see cref="CountedEventTypes"/>, đếm mọi loại bản tin.</summary>
        public bool CountAllEventTypes { get; set; }

        /// <summary>
        /// Danh sách loại bản tin ĐANG có hiệu lực (rỗng khi đếm mọi loại), đã bổ sung tên cũ/tên mới của
        /// các loại sự cố từng đổi tên. Nhờ vậy cấu hình khai tên cũ vẫn khớp bản ghi mang tên mới và ngược
        /// lại — một bản ghi chỉ mang MỘT giá trị nên không có nguy cơ đếm trùng.
        /// </summary>
        public List<string> EffectiveEventTypes => CountAllEventTypes
            ? new List<string>()
            : WithRenamedAliases(CountedEventTypes.Count > 0 ? CountedEventTypes : DefaultEventTypes);

        private static List<string> WithRenamedAliases(IEnumerable<string> types)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string? name)
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                var t = name.Trim();
                if (seen.Add(t)) result.Add(t);
            }

            foreach (var type in types)
            {
                Add(type);
                if (!string.IsNullOrWhiteSpace(type) && RenamedTypes.TryGetValue(type.Trim(), out var other))
                    Add(other);
            }
            return result;
        }
    }
}
