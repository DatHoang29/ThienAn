using Modules.TMS.Controllers.Weather.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.Weather.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 01/11/2024
    /// </summary>
    public class TmsWeatherQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsWeather> _weatherRepo;

        public TmsWeatherQueryHandler(BaseRepository<TmsWeather> weatherRepo)
        {
            this._weatherRepo = weatherRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 01/11/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageTmsWeatherOutput>> HandleAsync(PageTmsWeatherInput command)
        {
            // TODO: Cache

            var wList = await this._weatherRepo.AsQueryable()
                .Where(w => w.IsDelete == null)
                .WhereIF(command.TimeDetect != null, w => w.TimeDetect != null && w.TimeDetect.Value.Date == command.TimeDetect.Value.Date)
                .Select(w => new PageTmsWeatherOutput() { }, true)
                .OrderBuilder(command)
                .ToPagedListAsync(command.Page, command.PageSize);

            return wList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thông tin một record
        /// Created date: 01/11/2024
        /// </summary>
        public async Task<TmsWeatherOutput?> HandleAsync(TmsWeatherInput command)
        {
            DateTime valueDate = command.TimeDetect ?? DateTime.Now;

            var wList = await this._weatherRepo.AsQueryable()
                .Where(w => w.IsDelete == null)
                .Where(w => w.TimeDetect != null && w.TimeDetect.Value.Date == valueDate.Date)
                .Select(w => new TmsWeatherOutput() { }, true)
                .OrderByDescending(w => w.CreateTime)
                .ToListAsync();

            return wList.FirstOrDefault<TmsWeatherOutput>();
        }
    }
}
