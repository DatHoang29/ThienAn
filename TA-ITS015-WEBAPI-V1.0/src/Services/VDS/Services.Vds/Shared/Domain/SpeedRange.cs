using System.Text.Json.Serialization;

namespace Services.Vds.Shared.Domain;

/// <summary>
/// Author: KienLT
/// Description: Ngưỡng tốc độ hợp lệ của một hướng tuyến (km/h). Xe ra ngoài khoảng này là vi phạm.
///
/// Là <c>record</c> để bất biến: ngưỡng đã vào snapshot thì không ai sửa được tại chỗ, muốn đổi phải
/// tạo snapshot mới. Nhờ vậy luồng đang đọc không bao giờ thấy giá trị nửa vời.
/// Created date: 24/08/2026
/// </summary>
/// <param name="Min">Tốc độ tối thiểu (km/h). Dưới ngưỡng này là chạy quá chậm.</param>
/// <param name="Max">Tốc độ tối đa (km/h). Trên ngưỡng này là quá tốc độ.</param>
public sealed record SpeedRange(int Min, int Max)
{
    /// <summary>
    /// Ngưỡng có dùng được không. Khai nửa vời (min &gt; max, hoặc số không dương) thì phải BỎ hướng đó
    /// chứ không sửa thành giá trị đoán — thà không báo vi phạm còn hơn báo theo ngưỡng bịa.
    ///
    /// <c>JsonIgnore</c> vì đây là giá trị TÍNH ĐƯỢC từ Min/Max: ghi vào file cache chỉ làm file rối, và
    /// nếu ai sửa tay thành <c>"IsValid": true</c> cho một ngưỡng sai thì tưởng là bỏ qua được kiểm tra.
    /// </summary>
    [JsonIgnore]
    public bool IsValid => Min > 0 && Max > 0 && Min <= Max;

    /// <summary>Tốc độ nằm trong ngưỡng (tính cả hai biên) ⇒ không vi phạm.</summary>
    public bool Contains(double speedKmh) => speedKmh >= Min && speedKmh <= Max;

    public override string ToString() => $"{Min}–{Max} km/h";
}
