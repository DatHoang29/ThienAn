using Mapster;
using Modules.Integration.VMS.Base.Core.Exceptions;
using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.VMS.Controllers.Current.Queries
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các truy vấn ra của đối tượng VmsCurrent
    /// Create date: 18/10/2024
    /// </summary>
    public class CurrentQueryHandler : IScoped
    {
        private BaseRepository<VmsCurrent> _baseRepository;
        private BaseCacheService _cache;

        public CurrentQueryHandler(BaseRepository<VmsCurrent> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm list danh sách theo điều kiện tìm kiếm hoặc theo phân trang mặc định SqlSugar
        /// Created date: 18/10/2024
        /// </summary>
        /// <param name="command">Đối tượng PageVmsCurrentInput</param>
        /// <returns>Danh sách PageVmsCurrentOutput</returns>
        public async Task<SqlSugarPagedList<PageVmsCurrentOutput>> HandleAsync(PageVmsCurrentInput command)
        {
            var imgList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Name), img => img.Name != null && img.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.Code), img => img.Code != null && img.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrEmpty(command.EquipmentId), img => img.EquipmentId == command.EquipmentId)
                .Select((img) => new PageVmsCurrentOutput() { }, true)
                .ToPagedListAsync(command.Page, command.PageSize);

            return imgList;
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm list danh sách theo giá trị trường "code" truyền vào
        /// Created date: 18/10/2024
        /// </summary>
        /// <param name="command">Đối tượng VmsCurrentInput</param>
        /// <returns>Danh sách VmsCurrentOutput</returns>
        public async Task<List<VmsCurrentOutput>> HandleAsync(VmsCurrentInput command)
        {
            // TODO: Cache

            var imgList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Name), img => img.Name != null && img.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.Code), img => img.Code != null && img.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrEmpty(command.EquipmentId), img => img.EquipmentId == command.EquipmentId)
                .Select((img) => new VmsCurrentOutput() { }, true)
                .ToListAsync();

            return imgList;
        }

		public async Task<VmsCurrentOutput> HandleAsync(VmsCurrentByEquipment command)
		{
			// Ưu tiên dòng đang giữ nội dung mặc định (DefaultUrl != null), rồi tới dòng cũ nhất.
			// => Reset luôn đọc đúng dòng đã lưu default, kể cả khi 1 thiết bị có nhiều dòng VmsCurrent.
			var vmsCurrentFromDb = await _baseRepository.AsQueryable()
				.Where(v => v.EquipmentId == command.EquipmentId)
				.OrderBy(v => v.DefaultUrl, OrderByType.Desc)
				.OrderBy(v => v.CreateTime, OrderByType.Asc)
				.FirstAsync();

			if (vmsCurrentFromDb == null) // -> trả null để caller tự quyết định insert mới, KHÔNG throw DataNotExist.
            {
                return null;
			}

            var vmsCurrentOutPut = vmsCurrentFromDb.Adapt<VmsCurrentOutput>();
			return vmsCurrentOutPut;
		}
	}
}
