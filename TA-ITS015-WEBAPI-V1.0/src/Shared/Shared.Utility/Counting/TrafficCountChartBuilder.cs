using System.Globalization;

using Shared.Utility.Helpers;
using Shared.Utility.Utilities;

namespace Shared.Utility.Counting;

/// <summary>
/// Author: KienLT
/// Description: Dựng khối <c>chart</c> ĂN LIỀN cho ECharts từ chuỗi đếm theo giờ.
///
/// <b>Vì sao trả đúng kiểu của WebAPI</b>: kết quả là chính
/// <see cref="EChartHelper.EChartResponse"/> mà endpoint
/// <c>POST api/tms/TmsTrafficData/TrafficDataToDayChart</c> đang trả — cùng bộ khoá
/// <c>series/xAxis/yAxis/legend/title</c> và cùng cách viết chữ thường. FE vẽ bản tin NATS bằng ĐÚNG
/// đoạn code đã dùng cho chart TMS, không phải viết nhánh riêng.
///
/// <b>Vì sao dựng ở service chứ không để FE tự gộp</b>: payload đếm có chế độ <c>delta</c> (chỉ gồm vài
/// camera vừa đổi). FE muốn tự gộp thì phải giữ trạng thái đủ 24 giờ × 4 nhóm cỡ xe cho TỪNG camera ở
/// client rồi tự lấp giờ trống — đúng phần việc <see cref="TrafficCountMath.ByHour"/> đã làm sẵn.
///
/// <b>Khác chart TMS ở hai điểm, cả hai là cố ý</b>:
/// <list type="number">
/// <item>Nhãn trục X là <c>HH:mm</c> theo GIỜ SERVER, không phải số giờ trần (<c>"0"</c>, <c>"1"</c>…).
/// Số đếm chạy theo mốc UTC nên nhãn UTC sẽ lệch 7 tiếng so với bảng điều khiển; đổi về giờ server là
/// nhãn duy nhất người xem đọc đúng. Dữ liệu thô vẫn nằm ở <c>flowByHour[].hour</c> dạng UTC có hậu tố
/// <c>Z</c> nên bên nhận vẫn tự quy đổi được nếu muốn.</item>
/// <item><c>yAxis</c> được khai (<c>type = "value"</c>). Chart TMS để null nên FE phải tự bù cấu hình
/// trục.</item>
/// </list>
///
/// Hàm THUẦN: không DB, không DI, không đọc cấu hình ⇒ test trực tiếp.
/// Created date: 27/08/2026
/// </summary>
public static class TrafficCountChartBuilder
{
    /// <summary>
    /// Khoá tiêu đề. Dùng LẠI đúng khoá của chart TMS thay vì đặt khoá mới: FE đã có bản dịch cho khoá
    /// này, đặt khoá mới thì tiêu đề hiện nguyên văn <c>"lz.label…"</c> tới khi ai đó thêm bản dịch.
    /// </summary>
    public const string TitleKey = "lz.label.tmsTrafficData.trafficVolumeChartTitle";

    /// <summary>Định dạng nhãn trục X (giờ server).</summary>
    public const string HourFormat = "HH:mm";

    /// <summary>Nhóm xe không suy được cỡ. KHÔNG có trong danh mục <c>vehicle_type</c>.</summary>
    public const string OtherSizeCode = "other";

    /// <summary>
    /// Tên hiển thị cho nhóm <see cref="OtherSizeCode"/>. Danh mục <c>vehicle_type</c> chỉ có ba mã
    /// <c>short/medium/long</c> nên không tra được tên; để nguyên mã thì chú giải chart hiện chữ
    /// <c>"other"</c>. Thêm một dòng <c>other</c> vào danh mục là hết cần hằng này.
    /// </summary>
    public const string OtherSizeFallbackName = "Không xác định cỡ";

    /// <summary>
    /// Thứ tự series CỐ ĐỊNH theo cỡ xe tăng dần, không lấy theo <c>OrderNo</c> của danh mục: cả ba dòng
    /// <c>short/medium/long</c> trong DB đều có <c>OrderNo = 100</c> nên sắp theo cột đó cho thứ tự không
    /// xác định, và ECharts gán màu THEO CHỈ SỐ series ⇒ mỗi bản tin một màu khác nhau.
    /// </summary>
    private static readonly (string Code, Func<TrafficCountBlock, int> Value)[] SizeSeries =
    {
        (TrafficVehicleSizes.Short, b => b.Short),
        (TrafficVehicleSizes.Medium, b => b.Medium),
        (TrafficVehicleSizes.Long, b => b.Long),
        (OtherSizeCode, b => b.Other),
    };

    /// <summary>
    /// Dựng chart lưu lượng theo giờ, mỗi series là một cỡ xe.
    /// </summary>
    /// <param name="hours">
    /// Chuỗi theo giờ đã bù 0 (kết quả <see cref="TrafficCountMath.ByHour"/>). Mốc giờ là UTC.
    /// </param>
    /// <param name="displayName">
    /// Tra tên hiển thị của mã cỡ xe — thực tế là
    /// <c>code => ConfigDataManager.NameOf("vehicle_type", code)</c>. Null hoặc trả về chính mã ⇒ dùng mã
    /// (riêng nhóm <see cref="OtherSizeCode"/> dùng <see cref="OtherSizeFallbackName"/>).
    /// </param>
    public static EChartHelper.EChartResponse Build(
        IReadOnlyList<TrafficCountHour>? hours,
        Func<string, string?>? displayName = null)
    {
        hours ??= Array.Empty<TrafficCountHour>();

        var labels = hours.Select(h => HourLabel(h.Hour)).ToArray();
        var series = new List<EChartHelper.EChartSeries>();
        var legend = new List<string>();

        foreach (var (code, value) in SizeSeries)
        {
            // Nhóm "other" chỉ hiện khi THỰC SỰ có xe không suy được cỡ. Luôn phát thì chart nào cũng có
            // thêm một đường 0 vô nghĩa trong chú giải; bỏ hẳn thì mất dữ liệu khi thiết bị không cấp
            // chiều dài. Nó nằm CUỐI danh sách nên xuất hiện/mất đi không đổi màu ba series kia.
            if (code == OtherSizeCode && !hours.Any(h => value(h.Block) > 0)) continue;

            var name = SeriesName(code, displayName);
            legend.Add(name);

            series.Add(new EChartHelper.EChartSeries
            {
                name = name,
                type = "line",
                // data là List<string> theo đúng kiểu của EChartSeries (chart TMS cũng ToString()).
                // ECharts đọc chuỗi số bình thường.
                data = hours.Select(h => value(h.Block).ToString(CultureInfo.InvariantCulture)).ToList(),
                label = new EChartHelper.EChartSeriesLabel(),
                itemStyle = new EChartHelper.EChartItemStyle(),
            });
        }

        return new EChartHelper.EChartResponse
        {
            series = series,
            xAxis = new EChartHelper.EChartAxis
            {
                type = "category",
                data = labels,
                boundaryGap = true,
            },
            yAxis = new EChartHelper.EChartAxis
            {
                type = "value",
            },
            legend = new EChartHelper.EChartLegend
            {
                data = legend.ToArray(),
                top = "5%",
            },
            title = new EChartHelper.EChartTitle
            {
                text = TitleKey,
                subtext = string.Empty,
                top = "92%",
                left = "center",
            },
        };
    }

    /// <summary>
    /// Nhãn một mốc giờ, quy về giờ server.
    ///
    /// <c>SpecifyKind</c> là bắt buộc: <c>TimeZoneInfo.ConvertTimeFromUtc</c> NÉM khi nhận
    /// <c>DateTimeKind.Local</c>. Mốc từ pipeline luôn là UTC, nhưng một nhãn sai giờ còn đỡ hơn cả bản
    /// tin đếm chết vì một tham số lạc Kind.
    /// </summary>
    private static string HourLabel(DateTime hourUtc)
    {
        var utc = DateTime.SpecifyKind(hourUtc, DateTimeKind.Utc);
        var local = BaseUtil.TimeServer.GetTimeServer(utc) ?? utc;
        return local.ToString(HourFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tên series. <c>ConfigDataManager.NameOf</c> trả về CHÍNH MÃ khi không tra được, nên so lại với mã
    /// để biết là đã tra ra tên thật hay chỉ là giá trị lùi.
    /// </summary>
    private static string SeriesName(string code, Func<string, string?>? displayName)
    {
        var resolved = displayName?.Invoke(code);

        if (!string.IsNullOrWhiteSpace(resolved)
            && !string.Equals(resolved.Trim(), code, StringComparison.OrdinalIgnoreCase))
        {
            return resolved.Trim();
        }

        return code == OtherSizeCode ? OtherSizeFallbackName : code;
    }
}
