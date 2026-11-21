using Mapster;
using MassTransit;
using Modules.CCTV.Controllers.Dashboard.Dto;
using Modules.CCTV.Core.Entities;
using Modules.CCTV.Core.Exceptions;
using Modules.TMS.Infrastructure;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;
using static Shared.DTO.Constants.Localization.BaseLocaleManager;

namespace Module.CCTV.Controllers.Dashboard.Commands
{
    public class DashboardCommandHandler : IWolverineHandler
    {
        private BaseRepository<CctvDashboard> _cctvDashboardRep;
        private BaseCacheService _cache;
        private BaseRepository<CctvDashboardItem> _cctvDashboardItemRep;

        public DashboardCommandHandler(BaseRepository<CctvDashboard> cctvDashboardRep, BaseCacheService cache, BaseRepository<CctvDashboardItem> cctvDashboardItemRep)
        {
            _cctvDashboardRep = cctvDashboardRep;
            _cache = cache;
            _cctvDashboardItemRep = cctvDashboardItemRep;
        }

        /// <summary>
        /// Thêm mới 1 record vào bảng CctvDashboard
        /// Author: ChuongLP
        /// Date: 2026-06-03
        /// </summary>
        public async Task<string> HandleAsync(AddDashboardInput command)
        {
            // Trùng tên phân biệt hoa/thường: SQL Server collation mặc định không phân biệt hoa/thường,
            if ((await _cctvDashboardRep.AsQueryable().Where(u => u.Name == command.Name).ToListAsync())
                    .Any(u => string.Equals(u.Name, command.Name, StringComparison.Ordinal)))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Name);

            
            var now = DateTime.Now;
            // Khởi tạo sẵn ID để tí nữa trả về

            var entity = command.Adapt<CctvDashboard>();
            entity.ID = Guid.NewGuid().ToString();   // gán trực tiếp
            entity.CreateUId = App.User.GetUsername();
            entity.CreateTime = DateTime.Now;
            entity.IsDelete = null;
            entity.Tags = command.Tags is null ? "" : string.Join(",", command.Tags);
            entity.Config = string.IsNullOrWhiteSpace(command.Config) ? null : command.Config;
            await _cctvDashboardRep.InsertAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvDashboard);
            return entity.ID;
        }

        /// <summary>
        /// Chỉnh sửa 1 record trong bảng CctvDashboard
        /// Author: ChuongLP
        /// Date: 2026-06-03
        /// </summary>
        public async Task HandleAsync(UpdateDashboardInput command)
        {
            // Trùng tên phân biệt hoa/thường (bỏ qua chính bản ghi đang sửa)
            if ((await _cctvDashboardRep.AsQueryable().Where(u => u.Name == command.Name && u.ID != command.ID).ToListAsync())
                    .Any(u => string.Equals(u.Name, command.Name, StringComparison.Ordinal)))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var now = DateTime.Now;

            var cctvDashboardRep = await _cctvDashboardRep.GetFirstAsync(u => u.ID == command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.NotExist);

            var entity = command.Adapt<CctvDashboard>();
            entity.UpdateUId = App.User.GetUsername();
            entity.UpdateTime = now;
            entity.Config = string.IsNullOrWhiteSpace(command.Config) ? null : command.Config;
            entity.Tags = command.Tags is null ? "" : string.Join(",", command.Tags); 
            await _cctvDashboardRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvDashboard);
        }

        /// <summary>
        /// Hàm xóa 1 record trong bảng CctvDashboard
        /// Author: ChuongLP
        /// Date: 2026-06-03
        /// </summary>  
        public async Task HandleAsync(DeleteDashboardInput command)
        {
            var cctvDashboardRep = await _cctvDashboardRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Update 21/10/2026: xóa mềm dashboard + xóa luôn item của nó trong 1 transaction
            // (FE không có chức năng khôi phục; trước đây item bị bỏ lại mồ côi và cache item không được xóa).
            var db = _cctvDashboardRep.Context;
            await db.AsTenant().BeginTranAsync();
            try
            {
                await _cctvDashboardRep.SoftDeleteAsync<CctvDashboard>(cctvDashboardRep);
                await _cctvDashboardItemRep.DeleteAsync(u => u.DashboardId == cctvDashboardRep.ID);
                await db.AsTenant().CommitTranAsync();
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }

            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvDashboard);
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvDashboardItem);
        }

        /// <summary>
        /// Hàm coppy 1 record trong bảng CctvDashboard
        /// Author: ChuongLP
        /// Date: 2026-06-03
        /// </summary>
        public async Task<string> HandleAsync(CopyDashboardInput command)
       {
            //Bước 1: Kiểm tra xem tên dashboard có bị trùng không
            var exist = await _cctvDashboardRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.NotExist);

            //Bước 2: Kiểm tra tên mới có bị trùng không
            if ((await _cctvDashboardRep.AsQueryable().Where(u => u.Name == command.Name).ToListAsync())
                    .Any(u => string.Equals(u.Name, command.Name, StringComparison.Ordinal)))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Name);
                
            var now = DateTime.Now;
            var newDashboardId = Guid.NewGuid().ToString();

            //Bước 3: Tạo Dashboard mới từ Dashboard cũ
            var newDashboard = new CctvDashboard
            {
                ID = newDashboardId,
                Name = command.Name,
                Remark = exist.Remark,
                Tags = string.IsNullOrWhiteSpace(exist.Tags) ? "" : exist.Tags,
                Config = exist.Config,
                CreateUId = App.User.GetUsername(),
                CreateTime = now,
                UpdateUId = null,
                UpdateTime = null,
                IsDelete = null
            };
            await _cctvDashboardRep.InsertWithDiffLogAsync(newDashboard);

            //Bước 4 lấy toàn bộ item của dashboard cũ, sau đó copy vào dashboard mới
            var oldItem = await _cctvDashboardItemRep.AsQueryable().Where(u => u.DashboardId == command.ID).ToListAsync();

            //Bước 5: Clone từng Item sang dashboard mới
            if (oldItem.Any())
            {
                var newItem = oldItem.Select(item => new CctvDashboardItem
                {
                    ID = Guid.NewGuid().ToString(),
                    EquipmentId = item.EquipmentId,
                    DashboardId = newDashboardId,
                    X = item.X,
                    Y = item.Y,
                    W = item.W,
                    H = item.H,
                    CreateUId = App.User.GetUsername(),
                    CreateTime = now,
                }).ToList();

                await _cctvDashboardItemRep.InsertRangeAsync(newItem);
            }

            // Bước 6: Xóa cache cả 2 bảng
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvDashboard);
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvDashboardItem);

            return newDashboardId;
        }
    }
}
