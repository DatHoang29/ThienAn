using Mapster;
using Modules.WP.Controllers.Statistical.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Infrastructure;
using Shared.Core.Interfaces;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.WP.Controllers.Statistical.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các lệnh ghi (Update/Upload Image) cho cấu hình hệ thống WP
    /// Created date: 03/07/2026
    /// </summary>
    public  class StatisticalCommandHandler:IWolverineHandler
    {
        private BaseRepository<WpStatistical> _statisticalRep;
        private BaseCacheService _cache;
        private UserManager _userManager;


        public StatisticalCommandHandler(BaseRepository<WpStatistical> statisticalRep,BaseCacheService cache, UserManager userManager)
        {
            _statisticalRep = statisticalRep;
            _cache = cache;
            _userManager = userManager;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Statistical Count theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpStatisticalCountInput input)
        {
            var now = DateTime.Now;
            var statDate = now.Date;
            var statHour = now.Hour;

            // 1. Thử tăng Count trên record của khung giờ hiện tại (trường hợp phổ biến nhất)
            var affected = await _statisticalRep
                .AsUpdateable()
                .Where(x => x.StatDate == statDate && x.StatHour == statHour)
                .SetColumns(x => new WpStatistical { Count = x.Count + 1 })
                .ExecuteCommandAsync();

            if (affected > 0)
                return;

            // 2. Chưa có record cho khung giờ này -> insert record mới (lượt đầu tiên trong giờ)
            try
            {
                await _statisticalRep.InsertAsync(new WpStatistical
                {
                    StatYear = now.Year,
                    StatMonth = now.Month,
                    StatDate = statDate,
                    StatHour = statHour,
                    Count = 1
                });
            }
            catch (Exception)
            {
                await _statisticalRep
                    .AsUpdateable()
                    .SetColumns(x => new WpStatistical { Count = x.Count + 1 })
                    .Where(x => x.StatDate == statDate && x.StatHour == statHour)
                    .ExecuteCommandAsync();
            }
        }
    }
}

