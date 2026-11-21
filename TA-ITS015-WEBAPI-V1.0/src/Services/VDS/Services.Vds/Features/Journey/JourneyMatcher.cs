using Services.Vds.Shared.Domain;

namespace Services.Vds.Features.Journey;

/// <summary>Một hành trình A→B ứng viên (chưa gắn DB).</summary>
/// <param name="AvgSpeedKmh"><c>DistanceMeter / TravelSeconds * 3.6</c>.</param>
/// <param name="IsValid"><c>TravelSeconds</c> nằm trong [Min, Max] của đoạn.</param>
public sealed record JourneyCandidate(
    string SegmentId, string LicensePlate,
    string FromEquipmentId, string ToEquipmentId,
    DateTime FromTime, DateTime ToTime,
    int TravelSeconds, int DistanceMeter, double AvgSpeedKmh,
    string? VehicleSize, string? Direction, bool IsValid, string? InvalidReason);

/// <summary>
/// Author: KienLT
/// Description: Ghép hành trình A→B theo BIỂN SỐ chính xác (sau chuẩn hoá). Hàm THUẦN, không I/O — kiểm
/// chứng được trực tiếp.
///
/// <para><b>Ghép mỗi lượt B với lượt A GẦN NHẤT TRƯỚC NÓ, mỗi A chỉ dùng cho một B.</b> Đây là điểm cải
/// tiến so với bản cũ (bản cũ lấy lượt A sớm nhất rồi lượt B đầu tiên sau nó ⇒ xe đi HAI lượt trong cùng
/// cửa sổ thì mất lượt thứ hai). Với dữ liệu quét chồng lấn theo chu kỳ, ghép "gần nhất trước đó" cho ra
/// từng chuyến đi riêng biệt.</para>
///
/// <para><c>AvgSpeedKmh = DistanceMeter / TravelSeconds * 3.6</c>. <c>IsValid = false</c> khi
/// <c>TravelSeconds</c> ngoài [Min, Max] của đoạn — thời gian bất thường (kẹt xe/dừng lâu, hoặc hai xe
/// khác nhau bị đọc trùng biển) không được coi là một chuyến hợp lệ, và tầng trên KHÔNG nâng sự cố từ
/// chuyến không hợp lệ.</para>
/// Created date: 04/09/2026
/// </summary>
public static class JourneyMatcher
{
    public static IReadOnlyList<JourneyCandidate> Match(
        SegmentInfo seg, IReadOnlyList<JourneySighting> rows)
    {
        var result = new List<JourneyCandidate>();
        if (rows.Count == 0) return result;

        var byPlate = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.LicensePlate))
            .GroupBy(r => PlateRules.Normalize(r.LicensePlate));

        foreach (var g in byPlate)
        {
            if (string.IsNullOrEmpty(g.Key)) continue;

            // Sắp theo thời gian tăng dần rồi duyệt một lượt: giữ lượt A "đang chờ ghép" gần nhất, gặp lượt
            // B thì ghép với nó và tiêu thụ A (đặt lại null) để A không ghép cho nhiều B.
            var ordered = g.OrderBy(r => r.DetectTime).ToList();

            JourneySighting? pendingA = null;
            foreach (var s in ordered)
            {
                var isA = string.Equals(s.EquipmentId, seg.FromEquipmentId, StringComparison.OrdinalIgnoreCase);
                var isB = string.Equals(s.EquipmentId, seg.ToEquipmentId, StringComparison.OrdinalIgnoreCase);

                // Đoạn có A trùng B (cấu hình sai) thì bỏ qua biển này để không tự ghép với chính mình.
                if (isA && isB) continue;

                if (isA)
                {
                    // Lượt A mới thay cho A cũ chưa ghép: nếu xe qua A hai lần rồi mới tới B, lượt A gần B
                    // nhất mới là điểm bắt đầu đúng của chuyến.
                    pendingA = s;
                    continue;
                }

                if (!isB || pendingA is null) continue;

                var a = pendingA.Value;
                var travel = (int)(s.DetectTime - a.DetectTime).TotalSeconds;
                pendingA = null;   // A đã dùng cho B này
                if (travel <= 0) continue;

                var speed = seg.DistanceMeter / (double)travel * 3.6;
                var valid = travel >= seg.MinTravelSeconds && travel <= seg.MaxTravelSeconds;

                result.Add(new JourneyCandidate(
                    SegmentId: seg.SegmentId,
                    LicensePlate: g.Key,
                    FromEquipmentId: seg.FromEquipmentId,
                    ToEquipmentId: seg.ToEquipmentId,
                    FromTime: a.DetectTime,
                    ToTime: s.DetectTime,
                    TravelSeconds: travel,
                    DistanceMeter: seg.DistanceMeter,
                    AvgSpeedKmh: speed,
                    VehicleSize: a.VehicleSize ?? s.VehicleSize,
                    Direction: seg.Direction,
                    IsValid: valid,
                    InvalidReason: valid ? null : "out_of_travel_bounds"));
            }
        }

        return result;
    }
}
