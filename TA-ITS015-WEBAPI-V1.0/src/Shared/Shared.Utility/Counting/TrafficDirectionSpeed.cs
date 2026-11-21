// Author: Codex
// Description: Tốc độ trung bình hành trình A→B của một hướng trong cửa sổ hoàn tất.
// Created date: 02/10/2026

namespace Shared.Utility.Counting;

/// <summary>Tốc độ trung bình hành trình A→B của một hướng trong cửa sổ hoàn tất.</summary>
public static class TrafficDirectionSpeed
{
    public static (double? Average, DateTime? LatestAt) Calculate(
        IEnumerable<TrafficDirectionJourney> journeys, IReadOnlyList<string> equipmentIds,
        string direction, DateTime from, DateTime to, int? expectedDistanceMeter = null)
    {
        if (equipmentIds.Count != 2 || string.Equals(equipmentIds[0], equipmentIds[1], StringComparison.OrdinalIgnoreCase))
            return (null, null);
        var valid = journeys.Where(j => j.IsValid == true && j.ToTime >= from && j.ToTime < to
            && j.Direction == direction
            && string.Equals(j.FromEquipmentId, equipmentIds[0], StringComparison.OrdinalIgnoreCase)
            && string.Equals(j.ToEquipmentId, equipmentIds[1], StringComparison.OrdinalIgnoreCase)
            && (expectedDistanceMeter == null || (j.DistanceMeter == expectedDistanceMeter
                && j.TravelSeconds > 0 && j.AvgSpeedKmh is double measured
                && Math.Abs(measured - expectedDistanceMeter.Value * 3.6 / j.TravelSeconds.Value) <= 0.1))
            && j.AvgSpeedKmh is double speed && double.IsFinite(speed) && speed >= 0).ToList();
        return valid.Count == 0 ? (null, null)
            : (TrafficCountMath.Round(valid.Average(j => j.AvgSpeedKmh!.Value)), valid.Max(j => j.ToTime));
    }
}

public sealed class TrafficDirectionJourney
{
    public string? FromEquipmentId { get; set; }
    public string? ToEquipmentId { get; set; }
    public string? Direction { get; set; }
    public DateTime? ToTime { get; set; }
    public double? AvgSpeedKmh { get; set; }
    public bool? IsValid { get; set; }
    public int? DistanceMeter { get; set; }
    public int? TravelSeconds { get; set; }
}
