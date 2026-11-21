using Services.Vds.Shared.Parsing;
using Services.Vds.Configuration;

namespace Services.Vds.Pipeline.Mapping;

/// <summary>
/// Author: KienLT
/// Description: Nơi DUY NHẤT quyết định cỡ xe và quy đổi đơn vị chiều dài.
///
/// Gom hai việc này vào một class là có chủ đích: phân cỡ so theo MÉT còn cột lưu là MILIMÉT, tách ra hai
/// nơi là mở đường cho lỗi lệch đơn vị hoặc nhân hệ số hai lần (xe 5 m thành 5 km, mọi xe thành
/// <c>long</c>). Bộ mã trả về giữ đúng <c>short</c>/<c>medium</c>/<c>long</c> của hệ cũ để báo cáo TMS
/// hiện có vẫn đếm được.
///
/// Đọc cấu hình qua <see cref="IVdsConfigManager"/> ở MỖI lần gọi, không chốt trong constructor: ngưỡng
/// và đơn vị đổi được lúc đang chạy.
/// Created date: 26/08/2026
/// </summary>
public sealed class VehicleSizeResolver
{
    /// <summary>Xe ngắn.</summary>
    public const string Short = "short";

    /// <summary>Xe trung.</summary>
    public const string Medium = "medium";

    /// <summary>Xe dài.</summary>
    public const string Long = "long";

    private static readonly string[] RandomSizes = { Short, Medium, Long };

    private readonly IVdsConfigManager _config;

    public VehicleSizeResolver(IVdsConfigManager config) => _config = config;

    /// <summary>
    /// Chiều dài CHUẨN theo milimét để ghi cột độ dài.
    ///
    /// Ưu tiên <paramref name="lengthMm"/> (<c>vehicle_length_mm</c> ở mức gốc) — đã là milimét nên đi
    /// thẳng, KHÔNG qua hệ số quy đổi. Thiếu thì lùi về <paramref name="legacyLength"/> của payload cũ và
    /// quy đổi theo đơn vị khai trong cấu hình.
    ///
    /// Không có nguồn nào, không đọc được số, không dương, hoặc vượt tầm <c>int</c> ⇒ null. Trả NULL chứ
    /// không trả 0: 0 mm là dữ liệu sai, NULL là "thiết bị không cấp".
    /// </summary>
    public int? LengthMillimeter(int? lengthMm, string? legacyLength)
    {
        if (lengthMm is int mm && mm > 0) return mm;

        if (Positive(legacyLength) is not double value) return null;

        var millimeter = Math.Round(
            value * _config.Current.VehicleSize.DimensionToMillimeter, MidpointRounding.AwayFromZero);
        return millimeter is > 0 and <= int.MaxValue ? (int)millimeter : null;
    }

    /// <summary>
    /// Cỡ xe theo chế độ đang cấu hình.
    /// </summary>
    /// <param name="vendorEventId">Seed cho chế độ <c>random</c> — cùng bản tin cho cùng kết quả khi replay.</param>
    /// <param name="lengthMm">Chiều dài đã chuẩn hoá về milimét (kết quả của <see cref="LengthMillimeter"/>).</param>
    public string? Resolve(long vendorEventId, int? lengthMm)
    {
        var mode = _config.Current.VehicleSize.VehicleSizeMode?.Trim().ToLowerInvariant();
        switch (mode)
        {
            case "length":
                return ResolveByMillimeter(lengthMm);

            case "random":
            {
                // Băm nhẹ để phân bố đều, vẫn tất định theo id sự kiện.
                var hash = unchecked((ulong)vendorEventId * 2654435761UL);
                return RandomSizes[(int)(hash % (ulong)RandomSizes.Length)];
            }

            default:
                // Chế độ lạ ⇒ không xác định được cỡ. Không đoán, vì đoán sai làm lệch số liệu theo cỡ xe.
                return null;
        }
    }

    /// <summary>
    /// Phân cỡ theo chiều dài milimét với đúng hai ngưỡng đang cấu hình. Không có chiều dài ⇒ null
    /// (bản ghi được tính vào nhóm khác khi tổng hợp).
    /// </summary>
    public string? ResolveByMillimeter(int? lengthMm)
    {
        if (lengthMm is not int length || length <= 0) return null;

        var size = _config.Current.VehicleSize;
        var meter = length / 1000d;

        if (meter <= size.VehicleLengthShortMax) return Short;
        if (meter < size.VehicleLengthMediumMax) return Medium;
        return Long;
    }

    /// <summary>Số dương đọc được từ giá trị thiết bị cấp; <c>"-"</c>/rỗng/không phải số/không dương ⇒ null.</summary>
    private static double? Positive(string? value) =>
        VdsParse.Double(value?.Trim()) is double parsed && parsed > 0 ? parsed : null;
}
