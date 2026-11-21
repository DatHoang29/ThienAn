
using Modules.WP.Core.Entities;
using Shared.Utility.Helpers;

namespace Modules.WP.Controllers.Statistical.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Output DTO cho tính năng cấu hình hệ thống WP (Config)
/// Created date: 03/07/2026
/// </summary>
public class WpStatisticalChartOutput 
{
    public string? Label { get; set; }
    public int? Visits { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Statistical
/// Created date: 14/08/2026
/// </summary>

public class WpStatisticalOutput
{
}
/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Statistical Summary
/// Created date: 14/08/2026
/// </summary>

public class WpStatisticalSummaryOutput
{
    public int TodayVisits { get; set; }
    public int YesterdayVisits   { get; set; }
    public double ChangePercent { get; set; }
    public int PeakHour { get; set; }
    public int PeakHourVisits { get; set; }
}