
using Modules.Samplev2.Controllers.Category.Dto;
using Modules.Samplev2.Core.Entities;
using Modules.Samplev2.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Shared.Utility.Helpers;
using Shared.Utility.Utilities;
using SqlSugar;

namespace Modules.Samplev2.Controllers.Category.Queries
{
    public class CategoryQueryHandler
    {
        private SampleBaseRepository<SampCategory> _categoryRep;
        private BaseCacheService _cache;

        public CategoryQueryHandler(SampleBaseRepository<SampCategory> categoryRep,BaseCacheService cache)
        {
            _categoryRep = categoryRep;
            _cache = cache;
        }

        public  async Task<SqlSugarPagedList<PageCategoryOutput>> HandleAsync(PageCategoryInput command)
        {
            return await _categoryRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Description), u => u.Description.Contains(command.Description))
                .Select((u) => new PageCategoryOutput()
                {
                }, true).OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<CategoryOutput>> HandleAsync(CategoryInput command)
        {
            //var list =  _cache.GetOrAdd($"sysCategory:{command.Name}",   _ =>
            //{
                var list =    _categoryRep.AsQueryable()
                    .Select((u) => new CategoryOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;

            //});


            list = list.WhereIF(!string.IsNullOrEmpty(command.Name),u=>u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.Description), u => u.Description.Contains(command.Description)).ToList();
            return list;
        }

       
        public async Task<CategoryChartOutput> HandleAsync(CategoryChartInput command)
        {
            //var list =  _cache.GetOrAdd($"sysCategory:{command.Name}",   _ =>
            
            //Các điều kiện tìm kiếm
            var list =  await  _categoryRep.AsQueryable()
                .Where(l=> l.CreateTime >= BaseUtil.TimeServer.GetTimeServer(command.DateStart))
                .Where( l => l.CreateTime <= BaseUtil.TimeServer.GetTimeServer(command.DateEnd))
                .Select((u) => new CategoryOutput()
                {
                }, true)
                .OrderBy(u => u.CreateTime).ToListAsync();

            
            //Note: Nên tách phần generate chart qua service để xử lý

            // vd _categoryService.GenerateChartByCreateUser();


            var chart = new CategoryChartOutput();

            //Ví dụ chart theo người tạo. Nhớ order by theo cùng với dữ liệu
            var legend = list.Select(u => u.CreateUId).Distinct().OrderBy(u => u).ToArray();
            
            //Legend
            chart.legend = new BaseHelper.EChartHp.EChartLegend();
            chart.legend.top = "5%";
            chart.legend.data = legend;
            

            //XAxis là thời gian
            var timeData = BaseHelper.EChartHp.GetListDay(command.DateStart, command.DateEnd);            
            
            var xAxis = new BaseHelper.EChartHp.EChartAxis();

            xAxis.data = timeData.OrderBy(l=>l.Key).Select(l=>l.Value).ToArray();
            xAxis.type = "category";
            xAxis.boundaryGap = true;
            chart.xAxis = xAxis;


            //Series
            var series = new List<BaseHelper.EChartHp.EChartSeries>();
            foreach (var item in legend.OrderBy(l=>l))
            {
                var data = new BaseHelper.EChartHp.EChartSeries();
                data.name = item;
                data.type = "line"; // bar/line tham khảo echart
                foreach (var time in timeData.OrderBy(l=>l.Key))
                {
                    var count = list.Count(u => u.CreateUId == item && u.CreateTime.Date.ToString("yyyy-MM-dd") == time.Key);
                    data.data.Add(count.ToString());
                }
                series.Add(data);

            }
            chart.series = series;

            //Title
            var title = new BaseHelper.EChartHp.EChartTitle();
            title.text = "Biểu đồ thống kê theo người tạo"; // Tiêu đề biểu đồ nhớ localizer
            title.subtext = command.DateStart.ToString("dd/MM/yyyy HH:mm") + " - " + command.DateEnd.ToString("dd/MM/yyyy HH:mm");
            title.top = "0%"; //vị trí
            title.left = "left";
            chart.title = title;


            return chart;
        }
    }
}
