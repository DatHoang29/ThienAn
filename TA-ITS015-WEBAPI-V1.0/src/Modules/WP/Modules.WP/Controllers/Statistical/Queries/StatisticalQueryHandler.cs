
using Modules.WP.Controllers.Statistical.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Services;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.WP.Controllers.Statistical.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thống kê cho WP
    /// Created date: 03/07/2026
    /// </summary>
    public class StatisticalQueryHandler
    {
        private BaseRepository<WpStatistical> _statisticalRep;
        private BaseCacheService _cache;

        public StatisticalQueryHandler(BaseRepository<WpStatistical> statisticalRep, BaseCacheService cache)
        {
            _statisticalRep = statisticalRep;
            _cache = cache;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Statistical Summary theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<WpStatisticalSummaryOutput> HandleAsync(WpStatisticalSummaryInput command)
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);

            var list = await _statisticalRep
                .AsQueryable()
                .Where(x => x.StatDate == today || x.StatDate == yesterday)
                .ToListAsync();

            var todayRows = list.Where(x => x.StatDate == today).ToList();
            var todayVisits = todayRows.Sum(x => x.Count);
            var yesterdayVisits = list.Where(x => x.StatDate == yesterday).Sum(x => x.Count);

            var changePercent = yesterdayVisits > 0
                ? Math.Round((todayVisits - yesterdayVisits) * 100.0 / yesterdayVisits, 1)
                : (todayVisits > 0 ? 100 : 0);

            var peak = todayRows.OrderByDescending(x => x.Count).FirstOrDefault();

            return new WpStatisticalSummaryOutput
            {
                TodayVisits = todayVisits,
                YesterdayVisits = yesterdayVisits,
                ChangePercent = changePercent,
                PeakHour = peak?.StatHour ?? 0,
                PeakHourVisits = peak?.Count ?? 0,
            };
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Statistical Chart theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<List<WpStatisticalChartOutput>> HandleAsync(WpStatisticalChartInput command)
        {
            var output = new List<WpStatisticalChartOutput>();

            switch (command.TypeStat)
            {
                // 24 giờ của 1 ngày cụ thể
                case BaseEnums.Statistical.Hour:
                    {
                        var date = (command.Date ?? DateTime.Now).Date;

                        var list = await _statisticalRep
                            .AsQueryable()
                            .Where(x => x.StatDate == date)
                            .ToListAsync();

                        var dict = list.ToDictionary(x => x.StatHour, x => x.Count);
                        for (int h = 0; h < 24; h++)
                            output.Add(new WpStatisticalChartOutput
                            {
                                Label = h.ToString(),
                                Visits = (dict.TryGetValue(h, out var c) ? c : 0)
                            });
                        break;
                    }

                // Từng ngày trong 1 tháng
                case BaseEnums.Statistical.Date:
                    {
                        var year = command.Year ?? DateTime.Now.Year;
                        var month = command.Month ?? DateTime.Now.Month;

                        var list = await _statisticalRep
                            .AsQueryable()
                            .Where(x => x.StatYear == year && x.StatMonth == month)
                            .GroupBy(x => x.StatDate)
                            .Select(x => new { Date = x.StatDate, Total = SqlFunc.AggregateSum(x.Count) })
                            .ToListAsync();

                        var dict = list.ToDictionary(x => x.Date, x => x.Total);
                        var daysInMonth = DateTime.DaysInMonth(year, month);
                        for (int d = 1; d <= daysInMonth; d++)
                        {
                            var day = new DateTime(year, month, d);
                            output.Add(new WpStatisticalChartOutput
                            {
                                Label = day.ToString("yyyy-MM-dd"),
                                Visits = (dict.TryGetValue(day, out var c) ? c : 0)
                            });
                        }
                        break;
                    }

                // 12 tháng của 1 năm
                case BaseEnums.Statistical.Month:
                    {
                        var year = command.Year ?? DateTime.Now.Year;

                        var list = await _statisticalRep
                            .AsQueryable()
                            .Where(x => x.StatYear == year)
                            .GroupBy(x => x.StatMonth)
                            .Select(x => new { Month = x.StatMonth, Total = SqlFunc.AggregateSum(x.Count) })
                            .ToListAsync();

                        var dict = list.ToDictionary(x => x.Month, x => x.Total);
                        for (int m = 1; m <= 12; m++)
                            output.Add(new WpStatisticalChartOutput
                            {
                                Label = m.ToString(),
                                Visits = (dict.TryGetValue(m, out var c) ? c : 0)
                            });
                        break;
                    }

                // 5 năm gần nhất
                case BaseEnums.Statistical.Year:
                    {
                        var currentYear = DateTime.Now.Year;
                        var fromYear = currentYear - 4;

                        var list = await _statisticalRep
                            .AsQueryable()
                            .Where(x => x.StatYear >= fromYear)
                            .GroupBy(x => x.StatYear)
                            .Select(x => new { Year = x.StatYear, Total = SqlFunc.AggregateSum(x.Count) })
                            .ToListAsync();

                        var dict = list.ToDictionary(x => x.Year, x => x.Total);
                        for (int y = fromYear; y <= currentYear; y++)
                            output.Add(new WpStatisticalChartOutput
                            {
                                Label = y.ToString(),
                                Visits = (dict.TryGetValue(y, out var c) ? c : 0)
                            });
                        break;
                    }
            }

            return output;
        }
    }
}
