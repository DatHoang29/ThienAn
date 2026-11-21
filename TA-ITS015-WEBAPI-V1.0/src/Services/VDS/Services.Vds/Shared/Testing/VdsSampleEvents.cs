using System.Globalization;
using Newtonsoft.Json;
using Services.Vds.Shared.Domain;
using Shared.Utility.Utilities;

namespace Services.Vds.Shared.Testing;

/// <summary>
/// Số xe theo CỠ của một chu kỳ thống kê.
///
/// Ba nhóm đầu là ba trường <c>small_vehicle_num</c> / <c>medium_vehicle_num</c> / <c>large_vehicle_num</c>
/// ở cấp <c>data_list[]</c>; tổng ba nhóm LUÔN bằng <c>vehicle_num</c>.
/// <see cref="Other"/> KHÔNG có trong bản tin thật — nó là nhóm xe thiếu chiều dài nên không suy được cỡ,
/// chỉ tồn tại ở tầng đếm. Đặt <see cref="Other"/> khác 0 là cố tình làm lệch bất biến tổng, để test nhánh
/// cảnh báo của <c>StatisticHandler</c>.
/// </summary>
public readonly record struct VdsStatisticSizeCounts(int Short, int Medium, int Long, int Other)
{
    /// <summary>Tổng bốn nhóm — phải bằng <c>vehicle_num</c> của chu kỳ.</summary>
    public int Total => Short + Medium + Long + Other;
}

/// <summary>
/// Author: KienLT
/// Description: Sinh JSON bản tin VDS mẫu để bơm vào pipeline khi chưa có nguồn thật (WPF harness,
/// <c>SimulateBatchAsync</c>).
///
/// <para><b>Hình dạng frame <see cref="Normal"/> đã được kiểm chứng THẬT</b>: đó là frame mà phép thử
/// end-to-end ngày 26/08/2026 dùng (injector → Wolverine → camera cache → SqlSugar/PgWire → QuestDB), và
/// bản ghi đọc lại khớp đúng mapping mong đợi: <c>type=Normal, lane=2, speed=37, length_mm=6500,
/// size=medium</c>.</para>
///
/// <para><b>CẢNH BÁO — dữ liệu sinh ra được GHI THẬT.</b> Frame đi qua đúng pipeline của bản tin thật nên
/// nó ghi vào QuestDB, tiến checkpoint, và (nếu bật) sinh sự cố / cảnh báo biển số / trạng thái vùng.
/// Muốn dọn thì xoá theo <c>create_uid = Service:ServiceUserId</c>.</para>
///
/// Chỉ có <see cref="Normal"/> và <see cref="Statistic"/> — đúng hai thứ có consumer thật
/// (<c>VdsServiceManager.SimulateBatchAsync</c> và <c>RegularSimulator</c> của WPF). Bản cũ còn
/// <c>Event(...)</c>; chưa port vì chưa ai gọi.
/// Created date: 26/08/2026
/// </summary>
public static class VdsSampleEvents
{
    /// <summary>
    /// Bộ đếm id bản tin.
    ///
    /// Chặn trong khoảng 10^9 chứ không dùng thẳng <c>Ticks</c>: <c>IngestionCheckpointStore</c> lưu
    /// <c>LastEventId</c> để biết đã xử lý tới đâu, nên bơm một id khổng lồ sẽ đẩy checkpoint lên mốc mà
    /// bản tin thật không bao giờ vượt được, và mọi bản tin thật sau đó trông như "đã xử lý rồi".
    /// </summary>
    private static long _seq = DateTime.UtcNow.Ticks % 1_000_000_000;

    private static long NextId() => Interlocked.Increment(ref _seq);

    private static string NowUtc() =>
        DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    /// <summary>
    /// Một lượt xe đi qua bình thường.
    /// </summary>
    /// <param name="cameraIp">Phải là camera CÓ trong danh mục, nếu không frame bị bỏ ở cửa vào.</param>
    /// <param name="lane">Làn — đi vào <c>metadata.detection_region</c>, map ra cột <c>lane</c>.</param>
    /// <param name="plate">Biển số. Truyền <c>"-"</c> để mô phỏng bản tin không đọc được biển.</param>
    /// <param name="speedKmh">Tốc độ km/h — quyết định nhánh quá tốc độ và trạng thái vùng.</param>
    /// <param name="lengthMeter">
    /// Chiều dài xe tính bằng MÉT cho tiện gọi; quy ra <c>vehicle_length_mm</c> ở MỨC GỐC theo đặc tả
    /// v1.2+. null ⇒ gửi <c>"-"</c> đúng như máy chủ làm khi module ước lượng chiều dài đang tắt, nhờ vậy
    /// đi qua đúng nhánh "vendor gửi gạch ngang" thay vì nhánh "thiếu trường".
    /// </param>
    /// <param name="legacyType">
    /// true ⇒ gửi <c>type = "Regular"</c> (tên loại v1.0) để đi qua nhánh quy đổi của
    /// <c>VdsEnvelopeNormalizer</c>. Mặc định false: gửi tên đã chuẩn hoá như máy chủ v1.3.
    /// </param>
    public static string Normal(
        string cameraIp,
        string lane = "1",
        string plate = "60A90685",
        int speedKmh = 37,
        double? lengthMeter = 6.5,
        bool legacyType = false)
    {
        var id = NextId();

        return JsonConvert.SerializeObject(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["camera_ip"] = cameraIp,
            ["type"] = legacyType ? VdsEventTypes.LegacyNormal : VdsEventTypes.Normal,
            ["occurred_utc"] = NowUtc(),
            ["vehicle_length_mm"] = lengthMeter is double m && m > 0
                ? (object)(int)Math.Round(m * 1000, MidpointRounding.AwayFromZero)
                : "-",
            ["metadata"] = new Dictionary<string, object?>
            {
                // detection_region là trường thiết bị dùng cho làn; mapper đọc trường này.
                ["detection_region"] = lane,
                ["license_plate"] = plate,
                ["speed"] = $"{speedKmh.ToString(CultureInfo.InvariantCulture)}km/h",
                ["vehicle_type"] = plate == "-" ? "Car" : "SUV",
                ["vehicle_color"] = plate == "-" ? "-" : "White",
            },
        });
    }

    /// <summary>
    /// Bản tin SỰ CỐ (Speeding / Conflagration / Stopped Vehicle …). Metadata dùng chung schema với lượt
    /// xe bình thường; chỉ khác <c>type</c> và <c>alarm_type</c>.
    /// </summary>
    /// <param name="type">
    /// Tên loại; đi qua <see cref="VdsEventTypes.Canonical"/> nên nhận cả tên v1.0 lẫn v1.3.
    /// </param>
    /// <param name="plate">
    /// Mặc định là biển hợp lệ để test được cửa chống trùng theo biển số — nhiều loại dùng biển làm định
    /// danh phụ. Truyền <c>"-"</c> để mô phỏng bản tin không đọc được biển.
    /// </param>
    /// <param name="speedKmh">Mặc định 100 để loại <c>Speeding</c> vượt ngưỡng thường dùng.</param>
    public static string Event(
        string cameraIp,
        string type,
        string lane = "1",
        string plate = "60A90685",
        int speedKmh = 100,
        double? lengthMeter = 6.5,
        bool legacyType = false)
    {
        var id = NextId();
        var canonical = VdsEventTypes.Canonical(type);

        return JsonConvert.SerializeObject(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["camera_ip"] = cameraIp,
            ["type"] = legacyType ? LegacyNameOf(canonical) : canonical,
            ["occurred_utc"] = NowUtc(),
            ["vehicle_length_mm"] = lengthMeter is double m && m > 0
                ? (object)(int)Math.Round(m * 1000, MidpointRounding.AwayFromZero)
                : "-",
            ["metadata"] = new Dictionary<string, object?>
            {
                // alarm_type mang chính tên loại khi là vi phạm — bản tin thật của thiết bị làm vậy.
                ["alarm_type"] = canonical,
                ["detection_region"] = lane,
                ["license_plate"] = plate,
                ["speed"] = $"{speedKmh.ToString(CultureInfo.InvariantCulture)}km/h",
                ["vehicle_type"] = plate == "-" ? "Car" : "SUV",
                ["vehicle_color"] = plate == "-" ? "-" : "White",
            },
        });
    }

    /// <summary>Tên v1.0 của một loại, nếu loại đó từng đổi tên; còn lại giữ nguyên.</summary>
    private static string LegacyNameOf(string canonical) => canonical switch
    {
        VdsEventTypes.Normal => VdsEventTypes.LegacyNormal,
        VdsEventTypes.StoppedVehicle => VdsEventTypes.LegacyStoppedVehicle,
        VdsEventTypes.FallenObject => VdsEventTypes.LegacyFallenObject,
        _ => canonical,
    };

    /// <summary>
    /// Bản tin thống kê chu kỳ (<c>type = Statistic</c>) — schema metadata RIÊNG, số liệu theo từng làn
    /// trong <c>data_list[]</c>.
    ///
    /// Không truyền <paramref name="vehicleNum"/> thì dùng mẫu 2 làn cố định. Truyền vào thì sinh MỘT làn
    /// theo đúng số xe và tốc độ trung bình đã quan sát, để số liệu thống kê khớp với các bản tin lượt xe
    /// vừa gửi trong cùng chu kỳ.
    /// </summary>
    /// <param name="sizeCounts">
    /// Số xe theo cỡ. Ghi vào <c>small/medium/large_vehicle_num</c> ở cấp <c>data_list[]</c> và chỉ gắn
    /// vào làn ĐẦU TIÊN — <c>StatisticHandler</c> cộng dồn các phần tử nên gắn vào nhiều làn sẽ đếm lặp.
    /// Không truyền thì không có trường nào, đúng như bản tin của máy chủ chưa bật module ước lượng
    /// chiều dài.
    /// </param>
    public static string Statistic(
        string cameraIp,
        int periodMinutes = 3,
        int? vehicleNum = null,
        double? avgSpeedKmh = null,
        VdsStatisticSizeCounts? sizeCounts = null)
    {
        var id = NextId();
        var end = BaseUtil.TimeServer.GetTimeServer();
        var start = end.AddMinutes(-periodMinutes);
        string F(DateTime d) => d.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

        var sizeAttached = false;

        Dictionary<string, object?> Lane(
            string region, string speed, string num, int flow, double density, double mean, int sample)
        {
            var lane = new Dictionary<string, object?>
            {
                ["detection_region"] = region,
                // Cố ý để RỖNG: thiết bị cấu hình theo ROI chỉ gửi detection_region, và
                // VdsStatisticLaneItem.Lane phải tự lùi về trường đó.
                ["lane_no."] = "",
                ["average_speed"] = speed,
                ["vehicle_num"] = num,
            };

            if (sizeCounts is not null && !sizeAttached)
            {
                var s = sizeCounts.Value;
                lane["small_vehicle_num"] = s.Short.ToString(CultureInfo.InvariantCulture);
                lane["medium_vehicle_num"] = s.Medium.ToString(CultureInfo.InvariantCulture);
                lane["large_vehicle_num"] = s.Long.ToString(CultureInfo.InvariantCulture);
                sizeAttached = true;
            }

            lane["space_occupy_ration"] = "7.4";
            lane["time_occupy_ration"] = "7.32";
            lane["space_headway"] = "98.5";
            lane["time_headway"] = "13.8";
            lane["ext"] = new Dictionary<string, object?>
            {
                ["flow_vph"] = flow,
                ["density_vpkm"] = density,
                ["space_mean_kmh"] = mean,
                ["speed_sample"] = sample,
            };

            return lane;
        }

        var metadata = new Dictionary<string, object?>
        {
            ["event_type"] = VdsEventTypes.Statistic,
            ["device_name"] = "Traffic Sensing Camera",
            ["start_time"] = F(start),
            ["end_time"] = F(end),
            ["period_minutes"] = periodMinutes,
            ["computed_by"] = "sonata",
            ["data_list"] = vehicleNum is int n
                ? new object[]
                {
                    // flow_vph quy đổi từ số xe trong chu kỳ về lưu lượng giờ.
                    Lane("1",
                        (avgSpeedKmh ?? 0).ToString("0.#", CultureInfo.InvariantCulture),
                        n.ToString(CultureInfo.InvariantCulture),
                        periodMinutes > 0 ? n * 60 / periodMinutes : n,
                        Math.Round(n / (double)Math.Max(periodMinutes, 1) * 1.5, 1),
                        Math.Round(avgSpeedKmh ?? 0, 1),
                        n),
                }
                : new object[]
                {
                    Lane("1", "26.5", "13", 260, 10.2, 25.6, 6),
                    Lane("2", "27.2", "8", 160, 8, 19.9, 6),
                },
        };

        return JsonConvert.SerializeObject(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["camera_ip"] = cameraIp,
            ["type"] = VdsEventTypes.Statistic,
            ["occurred_utc"] = NowUtc(),
            ["metadata"] = metadata,
        });
    }
}
