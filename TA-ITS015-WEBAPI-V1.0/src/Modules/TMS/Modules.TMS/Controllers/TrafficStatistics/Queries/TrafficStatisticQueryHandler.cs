using Modules.TMS.Controllers.TrafficStatistics.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.TrafficStatistics.Queries
{
    public class TrafficStatisticQueryHandler
    {
        private BaseRepository<TmsTrafficStatistic> _baseRepository;
        private BaseCacheService _cache;

        public TrafficStatisticQueryHandler(BaseRepository<TmsTrafficStatistic> baseRepository, BaseCacheService cache)
        {
            _cache = cache;
            this._baseRepository = baseRepository;

        }
        /// <summary>
        /// Author:KhoaND
        /// Description: Hàm list danh sách theo điều kiện tìm kiếm hoặc theo phân trang mặc định SqlSugar
        /// Created date: 05/11/2024
        /// </summary>
        /// <param name="command">Đối tượng PagetmsTrafficStatisticInput</param>
        /// <returns>Danh sách PagetmsTrafficStatisticOutput</returns>

        public async Task<SqlSugarPagedList<PagetmsTrafficStatisticOutput>> HandleAsync(PagetmsTrafficStatisticInput command)
        {
            var wuList = await this._baseRepository.AsQueryable()
               .WhereIF(!string.IsNullOrWhiteSpace(command.KmNumber.ToString()), wu => wu.KmNumber.ToString() != null && wu.KmNumber.ToString().Contains(command.KmNumber.ToString()))
               //.WhereIF(!string.IsNullOrWhiteSpace(command.Code), wu => wu.Code != null && wu.Code.Contains(command.Code))
               //.WhereIF(!string.IsNullOrWhiteSpace(command.Phone), wu => wu.Phone != null && wu.Phone.Contains(command.Phone))
               //.WhereIF(command.Type != null, wu => wu.Type == command.Type)
               .Select((st) => new PagetmsTrafficStatisticOutput() { }, true)
               .OrderBuilder(command, "")
               .ToPagedListAsync(command.Page, command.PageSize);

            return wuList;

        }
        /// <summary>
        /// Author:KhoaND
        /// Description: Hàm list danh sách theo điều kiện tìm kiếm hoặc theo phân trang mặc định
        /// Created date: 05/11/2024
        /// </summary>
        /// <param name="command">Đối tượng tmsTrafficStatisticInput</param>
        /// <returns>Danh sách tmsTrafficStatisticOutput</returns>

        public async Task<List<TrafficStatisticOutput>> HandleAsync(TrafficStatisticInput command)
        {

            // TODO: Cache
            //var wuList = this._cache.GetOrAdd($"tmsTrafficStatistic:{command.Code}", _=>
            //{
            //    return this._baseRepository.AsQueryable()
            //        .Select((st) => new tmsTrafficStatisticOutput() { }, true)
            //        .OrderBy(st => st.CreateTime)
            //        .ToListAsync().Result;
            //});

            var wuList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.KmNumber.ToString()), wu => wu.KmNumber.ToString() != null && wu.KmNumber.ToString().Contains(command.KmNumber.ToString()))
                //.WhereIF(!string.IsNullOrWhiteSpace(command.Code), wu => wu.Code != null && wu.Code.Contains(command.Code))
                //.WhereIF(!string.IsNullOrWhiteSpace(command.Phone), wu => wu.Phone != null && wu.Phone.Contains(command.Phone))
                //.WhereIF(command.Type != null, wu => wu.Type == command.Type)
                .Select((st) => new TrafficStatisticOutput() { }, true)
                .OrderBy(st => st.CreateTime)
                .ToListAsync();

            return wuList;

        }

        /// <summary>
        /// Author:Tuan HA
        /// Description: Hàm lấy tmsTrafficStatistic by Equipment
        /// Created date: 27/02/2025
        /// </summary>
        /// <param name="command">Đối tượng tmsTrafficStatisticInput</param>
        /// <returns>Danh sách tmsTrafficStatisticOutput</returns>

        public async Task<TrafficStatisticOutput> HandleAsync(TrafficStatisticByEquipmentInput command)
        {
            var entity = await this._baseRepository.AsQueryable()
                        .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), s => s.EquipmentId == command.EquipmentId)
                        .OrderByDescending(st => st.ToTime)
                        .Take(1)
                        .Select(item => new TrafficStatisticOutput() { SpaceOccupy = SqlFunc.Round(item.SpaceOccupy, 3) }, true)
                        .FirstAsync();

            if (entity == null)
            {
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            }

            //if (float.TryParse(entity.SpaceOccupy, out float spaceOccupyDecimal))
            //{
            //	// Round to 5 decimal places
            //	spaceOccupyDecimal = (float)Math.Round(spaceOccupyDecimal, 5);
            //	entity.SpaceOccupy = spaceOccupyDecimal.ToString("G29");
            //}

            //var convertToDecimalSpace = Convert.ToDouble(entity.SpaceOccupy);
            //entity.SpaceOccupy = Math.Round(Convert.ToDecimal(entity.SpaceOccupy)).ToString("G29");
            //entity.TimeOccupy = Math.Round(Convert.ToDecimal(entity.TimeOccupy)).ToString();
            return entity;

        }
        /// <summary>
        /// Author: Ngọc Thắng
        /// Created Date: 24/02/2025
        /// </summary>
        public async Task<List<TrafficStatisticOutput>> HandleAsync(TrafficStatisticByDateInput command)
        {
            // TODO: Cache			
            var dataList = await this._baseRepository.AsQueryable()
                .Where(item => item.FromTime >= command.FromDate && item.ToTime <= command.ToDate)
                .Select(item => new TrafficStatisticOutput() { }, true)
                .ToListAsync();

            return dataList;
        }
    }
}
