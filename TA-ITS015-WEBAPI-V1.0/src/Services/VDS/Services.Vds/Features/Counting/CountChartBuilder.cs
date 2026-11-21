using Shared.Utility.Counting;
using Shared.Utility.Helpers;
namespace Services.Vds.Features.Counting;

/// <summary>Compatibility facade: the chart implementation is shared with Module.VDS.</summary>
public static class CountChartBuilder
{
    public const string TitleKey = TrafficCountChartBuilder.TitleKey;
    public const string HourFormat = TrafficCountChartBuilder.HourFormat;
    public const string OtherSizeCode = TrafficCountChartBuilder.OtherSizeCode;
    public const string OtherSizeFallbackName = TrafficCountChartBuilder.OtherSizeFallbackName;
    public static EChartHelper.EChartResponse Build(IReadOnlyList<TrafficCountHour>? hours,
        Func<string, string?>? displayName = null) => TrafficCountChartBuilder.Build(hours, displayName);
}