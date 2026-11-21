using System.Globalization;
using Shared.Utility.Counting;
using Services.Shared.Persistence;
using Services.Vds.Shared.Persistence;
using Services.Vds.Configuration;
using Shared.QuestHa;

namespace Services.Vds.Features.Counting;

/// <summary>
/// Author: KienLT
/// Description: Số ĐẾM XE tính trực tiếp từ bảng dữ liệu thô trên QuestDB bằng <c>SAMPLE BY</c>.
///
/// <b>Vì sao không còn bộ đếm trong RAM và bảng đếm riêng</b>: cộng dồn trong RAM thì restart là mất, nên
/// phải có thêm bảng chốt và một job hoà giải cache với DB — ba thứ phải luôn khớp nhau. QuestDB gộp theo
/// cửa sổ thời gian ngay trong truy vấn nên chỉ còn MỘT nguồn số: bảng thô. Restart không làm nhảy số vì
/// không có gì được giữ trong bộ nhớ.
///
/// <b>Vì sao SQL thô thay vì LINQ</b>: đã kiểm bằng cách sinh SQL offline — <c>ISugarQueryable.SampleBy</c>
/// của phiên bản SqlSugar đang dùng KHÔNG sinh ra mệnh đề <c>SAMPLE BY</c> cho QuestDB (câu lệnh vẫn chạy
/// nhưng gộp cả khoảng thành một ô, tức số liệu sai mà không có lỗi nào), và
/// <c>SqlFunc.AggregateCount(x.Speed)</c> dịch thành <c>COUNT(*)</c> nên không đếm được số mẫu tốc độ
/// thực. Hai điểm đó buộc phải viết SQL tay ở ĐÚNG một chỗ này.
///
/// An toàn tham số: mốc thời gian và danh sách loại bản tin đi bằng THAM SỐ. Riêng bước
/// <c>SAMPLE BY {n}m</c> phải là literal vì QuestDB không nhận bind variable ở vị trí đó — giá trị này là
/// <c>int</c> đã chuẩn hoá, không phải chuỗi từ ngoài.
///
/// Mọi truy vấn LUÔN có điều kiện trên <c>detectTime</c>: QuestDB phân vùng theo ngày, thiếu filter thời
/// gian là quét cả bảng và server tự huỷ query.
/// Created date: 26/08/2026
/// </summary>
public sealed class QuestCountReader : QuestRepositoryBase
{
    private readonly IVdsConfigManager _config;
    private readonly ILogger<QuestCountReader> _logger;

    public QuestCountReader(
        DatabaseManager databaseManager,
        IVdsConfigManager config,
        QuestAvailabilityGate gate,
        ILogger<QuestCountReader> logger, QuestHaManager? questHa = null) // KienLT - 31/10/2026: đọc HA qua QuestHaManager
        : base(databaseManager, config, gate, questHa)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Các ô đếm theo TỪNG CAMERA trong <c>[fromUtc, toUtc)</c>, hạt bằng <c>Counting:PeriodMinutes</c>.
    ///
    /// Trả về <see cref="TrafficCountSlice"/> dùng chung với WebAPI để phép gộp (cửa sổ trượt, chuỗi theo
    /// giờ, trung bình có trọng số) chạy đúng một công thức ở cả hai bên.
    /// </summary>
    public async Task<List<TrafficCountSlice>> ReadEquipmentSlicesAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var counting = _config.Current.Counting;
        var period = counting.EffectivePeriodMinutes;

        // Chốt mốc đầu vào lưới chu kỳ. ALIGN TO CALENDAR đã cắt ô theo lịch, nhưng mốc đầu lệch lưới thì
        // ô đầu tiên bị cắt cụt và số của nó thấp hơn thực tế.
        var from = TrafficCountMath.FloorPeriod(fromUtc, period);
        var to = toUtc;
        if (from >= to) return new List<TrafficCountSlice>();

        var maxHours = counting.EffectiveMaxQueryHours;
        if ((to - from).TotalHours > maxHours)
        {
            _logger.LogWarning(
                "[Count] Cửa sổ {Hours:0.#}h vượt trần {Max}h; chỉ đọc phần gần nhất.",
                (to - from).TotalHours, maxHours);
            from = TrafficCountMath.FloorPeriod(to.AddHours(-maxHours), period);
        }

        var query = CountQueryBuilder.Build(counting, period, from, to);
        var rows = await ReadAsync(db => db.Ado
            .SqlQueryAsync<CountSampleRow>(query.Sql, query.Parameters), ct);

        return CountSliceFolder.Fold(rows, period, counting, TrafficCountScopes.Equipment);
    }

    /// <summary>
    /// Các ô đếm theo TỪNG VÙNG trong <c>[fromUtc, toUtc)</c> — dùng cho bản tin lưu lượng theo zone.
    ///
    /// Cùng MỘT câu truy vấn với <see cref="ReadEquipmentSlicesAsync"/>, chỉ khác khoá gộp: <c>zoneId</c>
    /// đã có sẵn trong <c>SELECT</c> nên không cần thêm lượt đọc DB nào.
    ///
    /// <b>Cảnh báo cho bên gọi</b>: KHÔNG được cộng các ô zone lại để ra tổng tuyến. Một vùng là một đoạn
    /// Km; xe đi hết tuyến được đếm ở từng vùng nó đi qua nên tổng các vùng cao gấp mấy lần số xe thật.
    /// Tổng theo hướng phải lấy từ điểm vào (<c>Publish:CountDirectionGroups</c>).
    /// </summary>
    public async Task<List<TrafficCountSlice>> ReadZoneSlicesAsync(
        DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var counting = _config.Current.Counting;
        var period = counting.EffectivePeriodMinutes;

        var from = TrafficCountMath.FloorPeriod(fromUtc, period);
        var to = toUtc;
        if (from >= to) return new List<TrafficCountSlice>();

        var maxHours = counting.EffectiveMaxQueryHours;
        if ((to - from).TotalHours > maxHours)
        {
            _logger.LogWarning(
                "[ZoneTraffic] Cửa sổ {Hours:0.#}h vượt trần {Max}h; chỉ đọc phần gần nhất.",
                (to - from).TotalHours, maxHours);
            from = TrafficCountMath.FloorPeriod(to.AddHours(-maxHours), period);
        }

        var query = CountQueryBuilder.Build(counting, period, from, to);
        var rows = await ReadAsync(db => db.Ado
            .SqlQueryAsync<CountSampleRow>(query.Sql, query.Parameters), ct);

        return CountSliceFolder.Fold(rows, period, counting, TrafficCountScopes.Zone);
    }
}
