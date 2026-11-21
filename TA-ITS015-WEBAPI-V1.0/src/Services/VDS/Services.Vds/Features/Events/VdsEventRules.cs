using Services.Vds.Shared.Domain;

namespace Services.Vds.Features.Events;

/// <summary>
/// Author: KienLT
/// Description: Toàn bộ quyết định sinh sự kiện nằm trong MỘT <c>switch</c> ở MỘT file.
///
/// Cố ý KHÔNG dùng rule engine, bảng rule hay bộ diễn giải biểu thức: thêm loại mới là thêm một
/// <c>case</c>, đọc file này là biết hết luật. Hàm THUẦN, không I/O nên kiểm chứng được trực tiếp.
///
/// <para><b>Bố cục của <see cref="Decide"/>: riêng lên trên, chung xuống dưới.</b>
/// <list type="number">
/// <item>Hai loại KHÔNG phải sự kiện (lượt xe bình thường, bản tin thống kê).</item>
/// <item>Các loại có luật RIÊNG — mỗi loại một khối, vì chúng cần dữ liệu khác nhau: quá tốc độ giữ khoá chống trùng cũ; kẹt xe định danh theo LÀN chứ không theo xe.</item>
/// <item>Nhóm sự cố GẮN VỚI MỘT XE — dùng chung một dòng, định danh phụ là biển số.</item>
/// <item>Nhóm sự việc của HIỆN TRƯỜNG — dùng chung một dòng, không có định danh phụ.</item>
/// </list>
/// Thêm loại mới thì trước tiên thử xếp vào nhóm 3 hoặc 4; chỉ tách khối riêng khi nó thật sự cần dữ liệu
/// khác. Nhờ vậy sửa cách chặn trùng của cả một nhóm là sửa đúng một chỗ.</para>
///
/// Tên loại lấy từ hằng <see cref="VdsEventTypes"/>, KHÔNG viết chuỗi trực tiếp: <c>switch</c> chuỗi của C#
/// phân biệt hoa thường nên tên lệch một ký tự sẽ rơi vào <c>default</c> — chưa ánh xạ thì lưu unknown, không tạo hồ sơ. Dùng hằng thì đổi tên là lỗi biên dịch.
///
/// <c>ConditionCode</c> là slug ỔN ĐỊNH, khớp mã loại đã seed trong danh mục và giữ liên tục với dữ liệu cũ.
/// Vì vậy <see cref="VdsEventTypes.StoppedVehicle"/> vẫn dùng slug <c>stopped_suddenly</c> và
/// <see cref="VdsEventTypes.FallenObject"/> vẫn dùng <c>abandoned_object</c>.
/// Created date: 26/08/2026
/// </summary>
public static class VdsEventRules
{
    public static EventDecision Decide(VdsEventContext ctx)
    {
        var row = ctx.Row;
        var type = VdsEventTypes.Canonical(row.AidEventType ?? row.EventType);

        switch (type)
        {
            // ===== (1) Không phải sự kiện =====

            case VdsEventTypes.Normal:
                // Lượt xe bình thường: là dữ liệu lưu lượng, không phải sự kiện.
                return EventDecision.Skip("normal_traffic");

            case VdsEventTypes.Statistic:
                // Bản tin thống kê đi nhánh riêng, không bao giờ tới đây.
                return EventDecision.Skip("statistic_frame");

            // ===== (2) Luật riêng =====

            case VdsEventTypes.Speeding:
                // API v1.8: AID evaluates the camera/lane threshold; Slow Moving is a separate type.
                // Preserve the over-limit condition and subkey for dedup across upgrades.
                return EventDecision.Event("speeding_over_limit").PromoteToIncident()
                    with { SubKey = SpeedSubKey("over", row.LicensePlate) };

            case VdsEventTypes.Congestion:
                // Kẹt xe là tình trạng của LÀN, không của một xe: định danh phụ theo làn thì mọi bản tin kẹt
                // xe trên cùng làn được gộp thành một sự việc, còn hai làn nghẽn là hai việc khác nhau.
                return EventDecision.Event(ConditionCodeOf(type)).PromoteToIncident()
                    with { SubKey = row.Lane };

            // ===== (3) Sự cố GẮN VỚI MỘT XE: định danh phụ là biển số =====
            //
            // Cùng một xe vi phạm liên tục trước camera chỉ tính MỘT sự việc, nhưng hai xe khác nhau cùng vi
            // phạm tại đó là HAI việc và cả hai phải lên. Bỏ biển số ra khỏi khoá là gộp cả hai xe làm một.
            //
            // KHÔNG so lại với ngưỡng: camera đã tự kết luận theo cấu hình của nó. So lại bằng ngưỡng của ta
            // sẽ loại bỏ chính sự kiện camera vừa báo.
            case VdsEventTypes.SlowMoving:
            case VdsEventTypes.ReverseDriving:
            case VdsEventTypes.Motorcycle:
            case VdsEventTypes.StoppedVehicle:
            case VdsEventTypes.WrongLane:
            case VdsEventTypes.OccupyEmergencyLane:
            case VdsEventTypes.IllegalParking:
                return EventDecision.Event(ConditionCodeOf(type)).PromoteToIncident()
                    with { SubKey = row.LicensePlate };

            // ===== (4) Sự việc của HIỆN TRƯỜNG: KHÔNG có định danh phụ =====
            //
            // Cháy, người đi bộ, tai nạn, vật rơi, hạn chế tầm nhìn (sương mù, khói bụi) không gắn với một xe
            // cụ thể — và bản tin của chúng thường không mang biển số. Vì vậy mọi bản tin cùng loại tại cùng
            // camera trong cửa sổ chống trùng là CÙNG một sự việc.
            case VdsEventTypes.Conflagration:
            case VdsEventTypes.Pedestrian:
            case VdsEventTypes.TrafficAccident:
            case VdsEventTypes.FallenObject:
            case VdsEventTypes.RestrictedVisibility:
            case VdsEventTypes.Animal:
                return EventDecision.Event(ConditionCodeOf(type)).PromoteToIncident();

            default:
                // Loại lạ vẫn đi tiếp; mã TMS chỉ lấy từ ánh xạ, chưa ánh xạ (unknown) hoặc nhóm event thì promoter không ghi hồ sơ.
                return EventDecision.Event(string.Empty).PromoteToIncident();
        }
    }

    /// <summary>
    /// Slug điều kiện của một loại bản tin.
    ///
    /// Tách khỏi <see cref="Decide"/> để các loại dùng chung một cách chặn trùng gộp được vào một
    /// <c>case</c>, mà slug của từng loại vẫn nằm cạnh tên loại chứ không biến thành một bảng tra rời.
    ///
    /// <para><b>Ba slug KHÁC tên loại, không được "sửa cho nhất quán":</b> chúng khớp mã đã seed trong danh
    /// mục và khớp dữ liệu sự cố cũ. Đổi là làm lệch dữ liệu lịch sử.</para>
    ///
    /// <para>Nhánh <c>_</c> chỉ tới được nếu ai đó thêm <c>case</c> vào <see cref="Decide"/> mà quên thêm
    /// slug ở đây; test <c>VdsEventRulesTests</c> soát các loại sự cố nên lỗi đó bị bắt lúc chạy test.</para>
    /// </summary>
    private static string ConditionCodeOf(string canonicalType) => canonicalType switch
    {
        VdsEventTypes.Congestion => "congestion_detected",

        VdsEventTypes.SlowMoving => "slow_moving",
        VdsEventTypes.ReverseDriving => "reverse_driving",
        VdsEventTypes.Motorcycle => "motorcycle_on_road",
        VdsEventTypes.StoppedVehicle => "stopped_suddenly",   // KHÁC tên loại — giữ nguyên.
        VdsEventTypes.WrongLane => "wrong_lane",
        VdsEventTypes.OccupyEmergencyLane => "occupy_emergency_lane",
        VdsEventTypes.IllegalParking => "illegal_parking",

        VdsEventTypes.Conflagration => "conflagration_detected",
        VdsEventTypes.Pedestrian => "pedestrian_on_road",
        VdsEventTypes.TrafficAccident => "traffic_accident",
        VdsEventTypes.FallenObject => "abandoned_object",     // KHÁC tên loại — giữ nguyên.
        VdsEventTypes.RestrictedVisibility => "restricted_visibility",
        VdsEventTypes.Animal => "animal",

        _ => string.Empty,
    };

    /// <summary>
    /// Định danh phụ của nhánh quá tốc độ. Khoá chống trùng chỉ gộp (thiết bị, loại, định danh phụ) nên
    /// giữ tiền tố over để khớp khoá đã lưu trước khi nâng cấp API v1.8.
    /// Slow Moving có EventType riêng nên không dùng khoá này.
    /// </summary>
    private static string SpeedSubKey(string direction, string? licensePlate) =>
        string.IsNullOrWhiteSpace(licensePlate) ? direction : $"{direction}|{licensePlate.Trim()}";
}
