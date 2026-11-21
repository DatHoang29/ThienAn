namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: Luật THUẦN "ghép với camera GẦN NHẤT có bắt được xe": một lượt B (biển + camera cuối + mốc kết thúc) chỉ
/// giữ hành trình từ lượt A MUỘN NHẤT trước nó, bất kể đoạn nào.
///
/// <para><b>Vì sao.</b> Bộ sinh đoạn tạo mọi cặp camera cùng tuyến/hướng (06→04, 04→02 và đoạn dài 06→02). Trước
/// 21/10/2026 xe đi 06 → 04 → 02 sinh CẢ 06→02: tính trùng quãng đường trong thống kê và có thể nâng HAI sự cố tốc độ
/// trung bình (khoá chống trùng sự cố theo từng đoạn). Đoạn dài chỉ còn ý nghĩa khi camera ở giữa không đọc được biển
/// — khi đó lượt A muộn nhất chính là camera xa hơn và luật này vẫn giữ nó.</para>
/// <para>"Gần nhất" = lượt A có <c>FromTime</c> muộn nhất (xe qua camera theo thứ tự đường đi); trùng mốc ⇒ đoạn ngắn
/// hơn.</para>
/// Created date: 21/10/2026
/// </summary>
public static class JourneyNearestRules
{
    /// <summary>Hành trình đã ghi kết thúc ở cùng lượt B (mọi đoạn).</summary>
    public readonly record struct Existing(string Id, DateTime? FromTime, int? DistanceMeter);

    /// <summary>Kết quả xét một ứng viên trước khi ghi.</summary>
    /// <param name="Insert">Ghi ứng viên.</param>
    /// <param name="NearerId">Hành trình gần hơn đã có (khi không ghi).</param>
    /// <param name="Superseded">Hành trình XA hơn đã ghi trước đó (bản tin camera giữa đến trễ) — xoá mềm sau khi ghi.</param>
    public sealed record Decision(bool Insert, string? NearerId, IReadOnlyList<string> Superseded);

    /// <summary>Chọn ứng viên gần nhất trong các ứng viên kết thúc ở CÙNG một lượt B (đường ghép theo bản tin).</summary>
    public static JourneyCandidate? PickNearest(IEnumerable<JourneyCandidate?> candidates)
        => candidates.OfType<JourneyCandidate>()
            .OrderByDescending(c => c.FromTime)
            .ThenBy(c => c.DistanceMeter)
            .ThenBy(c => c.SegmentId, StringComparer.Ordinal)
            .FirstOrDefault();

    /// <summary>Xét ứng viên với các hành trình đã ghi kết thúc ở cùng lượt B.</summary>
    public static Decision Decide(DateTime candidateFrom, int candidateDistance, IEnumerable<Existing> existing, TimeSpan tolerance)
    {
        var rows = existing.Where(e => e.FromTime.HasValue).ToList();
        var nearer = rows
            .Where(e => e.FromTime!.Value > candidateFrom + tolerance
                        || ((e.FromTime!.Value - candidateFrom).Duration() <= tolerance && (e.DistanceMeter ?? int.MaxValue) <= candidateDistance))
            .OrderByDescending(e => e.FromTime)
            .FirstOrDefault();
        if (nearer.Id is not null) return new Decision(false, nearer.Id, []);

        var superseded = rows.Where(e => e.FromTime!.Value < candidateFrom - tolerance
                                         || ((e.FromTime!.Value - candidateFrom).Duration() <= tolerance && (e.DistanceMeter ?? int.MaxValue) > candidateDistance))
            .Select(e => e.Id).ToList();
        return new Decision(true, null, superseded);
    }
}
