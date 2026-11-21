using Mapster;
using Modules.CCTV.Controllers.Dashboard.Dto;
using Modules.CCTV.Core.Entities;
using Modules.CCTV.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;
using Wolverine;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Modules.CCTV.Controllers.Dashboard.Queries
{
    public class DashboardQueryHandler : IWolverineHandler
    {
        private BaseRepository<CctvDashboard> _cctvDashboard;
        private BaseCacheService _cache;
        private IMessageBus _bus;
        public DashboardQueryHandler(BaseRepository<CctvDashboard> cctvDashboard, BaseCacheService cache, IMessageBus bus)
        {
            _cctvDashboard = cctvDashboard;
            _cache = cache;
            _bus = bus;
        }


        /// <summary>
        /// Tìm kiếm danh sách Dashboard theo các tiêu chí lọc.
        /// Nếu không truyền gì → trả về toàn bộ danh sách.
        /// Author: ChuongLP
        /// Date: 2026-06-03
        /// </summary>
        public async Task<List<DashboardOutput>> HandleAsync(DashboardInput command)
        {
            // 1. Lấy dữ liệu thô (Entity) từ Cache hoặc DB
            var entities = _cache.GetOrAdd(CacheConst.Cctv.CctvDashboard, _ =>
            {
                return _cctvDashboard.AsQueryable().OrderBy(u => u.CreateTime).ToListAsync().Result;
            }) ?? new List<CctvDashboard>();

            // 2. Lọc dữ liệu tìm kiếm
            var filteredEntities = entities
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name),u => !string.IsNullOrWhiteSpace(u.Name) && u.Name.Contains(command.Name!)).ToList();

            var result = filteredEntities.Select(u =>
            {
                var output = u.Adapt<DashboardOutput>();
                output.TagList = SplitTags(u.Tags);
                return output;
            }).ToList();

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Lấy chi tiết 1 Dashboard theo ID
        /// Author: ChuongLP
        /// Date: 2026-06-08
        /// </summary>
        public async Task<DashboardOutput> HandleAsync(DashboardIdInput command)
        {
            // 1. Tìm kiếm bản ghi trong DB theo ID
            var entity = await _cctvDashboard.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.NotExist);

            // 2. Map các thuộc tính cơ bản sang DTO Output bằng Mapster
            var output = entity.Adapt<DashboardOutput>();

            // 3. XỬ LÝ TAY: Bẻ chuỗi "a,b,c" từ DB thành List<string> gán vào TagList trả về cho FE
            output.TagList = SplitTags(entity.Tags);

            return output;
        }

        /// <summary>
        /// Lấy danh sách Tags duy nhất từ tất cả Dashboard.
        /// Author: ChuongLP
        /// Date: 2026-06-12
        /// </summary>
        public async Task<List<DashboardTagOutput>> HandleAsync(DashboardTagInput command)
        {
            var entities = _cache.GetOrAdd(CacheConst.Cctv.CctvDashboard, _ =>
            {
                return _cctvDashboard.AsQueryable().OrderBy(u => u.CreateTime).ToListAsync().Result;
            });

            var tags = entities
                .SelectMany(u => SplitTags(u.Tags))                    // gom tag của mọi dashboard
                .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)     // gom nhóm để đếm + loại trùng
                .Select(g => new DashboardTagOutput
                {
                    Name = g.Key    
                })
                .OrderBy(x => x.Name)
                .ToList();


            return await Task.FromResult(tags);
        }

        /// <summary>
        /// Xử lý tay: Chia nhỏ chuỗi tag "a,b,c" thành List<string> ["a", "b", "c"] và loại bỏ khoảng trắng thừa.
        /// /// Author: ChuongLP
        /// Date: 2026-06-12
        /// </summary>
        private static List<string> SplitTags(string? tags)
        { 
            return string.IsNullOrWhiteSpace(tags) ? new List<string>() : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        } 
    }
}
