using Microsoft.AspNetCore.Http;
using Modules.WP.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.WP.Controllers.Statistical.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Input DTO cho tính năng cấu hình hệ thống WP (Config)
/// Created date: 03/07/2026
/// </summary>
public class WpStatisticalChartInput
{
    public string? TypeStat { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public DateTime? Date { get; set; }
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Statistical Count
/// Created date: 14/08/2026
/// </summary>
public class WpStatisticalCountInput
{
}
/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Statistical Summary
/// Created date: 14/08/2026
/// </summary>

public class WpStatisticalSummaryInput
{
}
