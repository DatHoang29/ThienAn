using System.Text.Json;
using Services.Vds.Configuration;
using Services.Vds.Shared.State;

namespace Services.Vds.Features.Summary;

public sealed record SummaryProgress(long Revision = 0, DateTime? HourThrough = null, DateTime? DayThrough = null, DateTime? MonthThrough = null);

public sealed class SummaryProgressStore
{
    private readonly object _sync = new();
    private readonly string _path;
    private SummaryProgress _current;
    public SummaryProgressStore(IVdsConfigManager config)
    {
        _path = StatePath.Resolve(config.Current.StateDirectory, "summary-progress-utc.json");
        _current = File.Exists(_path) ? JsonSerializer.Deserialize<SummaryProgress>(File.ReadAllText(_path)) ?? new() : new();
    }
    public SummaryProgress Current { get { lock (_sync) return _current; } }
    public bool Commit(SummaryProgress progress, long expectedRevision)
    {
        lock (_sync)
        {
            if (_current.Revision != expectedRevision) return false;
            Save(progress with { Revision = expectedRevision + 1 });
            return true;
        }
    }
    public void Rewind(DateTime fromUtc)
    {
        lock (_sync)
        {
            var hour = global::Shared.Utility.Counting.TrafficCountMath.FloorHour(fromUtc);
            var day = SummaryCalendar.DayStartUtc(hour);
            var month = SummaryCalendar.MonthStartUtc(hour);
            static DateTime Min(DateTime? a, DateTime b) => a.HasValue && a.Value < b ? a.Value : b;
            Save(new(_current.Revision + 1, Min(_current.HourThrough, hour), Min(_current.DayThrough, day), Min(_current.MonthThrough, month)));
        }
    }
    private void Save(SummaryProgress next)
    {
        StatePath.WriteAtomic(_path, JsonSerializer.Serialize(next));
        _current = next;
    }
}

public static class SummaryCalendar
{
    public static DateTime DayStartUtc(DateTime utc, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Local;
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone).Date, DateTimeKind.Unspecified), zone);
    }
    public static DateTime MonthStartUtc(DateTime utc, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Local;
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);
        return TimeZoneInfo.ConvertTimeToUtc(new DateTime(local.Year, local.Month, 1), zone);
    }
    public static DateTime NextDayUtc(DateTime utc) => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local).Date.AddDays(1), DateTimeKind.Unspecified), TimeZoneInfo.Local);
    public static DateTime NextMonthUtc(DateTime utc) => TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local).Date.AddMonths(1), DateTimeKind.Unspecified), TimeZoneInfo.Local);
}
