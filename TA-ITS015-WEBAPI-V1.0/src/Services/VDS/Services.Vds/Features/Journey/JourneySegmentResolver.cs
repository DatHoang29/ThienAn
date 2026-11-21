// Author: Codex
// Description: Đoạn khai chỉ ghi đè đúng cặp A→B; giữ đoạn liền kề và đoạn dài khác cặp.
// Created date: 02/10/2026

namespace Services.Vds.Features.Journey;

/// <summary>Đoạn khai chỉ ghi đè đúng cặp A→B; giữ đoạn liền kề và đoạn dài khác cặp.</summary>
public sealed class JourneySegmentResolver
{
    private readonly SegmentManager _declared;
    private readonly AutoSegmentManager _auto;
    public JourneySegmentResolver(SegmentManager declared, AutoSegmentManager auto)
    {
        _declared = declared;
        _auto = auto;
    }
    public IReadOnlyList<SegmentInfo> ForArrival(string? id)
        => Merge(FindDeclaredSegments(id), FindAutoSegments(id));
    public IReadOnlyList<SegmentInfo> FindDeclaredSegments(string? id) => _declared.EndingAt(id);
    public IReadOnlyList<SegmentInfo> FindAutoSegments(string? id) => _auto.EndingAt(id);
    public IReadOnlyList<SegmentInfo> ForDeparture(string? id)
        => Merge(_declared.StartingAt(id), _auto.StartingAt(id));
    public IReadOnlyList<SegmentInfo> AllEffective() => Merge(_declared.All, _auto.All);

    public static IReadOnlyList<SegmentInfo> Merge(IEnumerable<SegmentInfo> declared, IEnumerable<SegmentInfo> automatic)
        => declared.Concat(automatic)
            .DistinctBy(s => (s.FromEquipmentId.ToUpperInvariant(), s.ToEquipmentId.ToUpperInvariant())).ToList();
}
