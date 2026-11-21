using Modules.VMS.Controllers.ProcessImg.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;


namespace Modules.VMS.Controllers.ProcessImg.Queries
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các truy vấn ra của đối tượng VmsProcessImg
    /// Create date: 14/10/2024
    /// </summary>
    public class ProcessImgQueryHandler:IWolverineHandler
    {
        private BaseRepository<VmsProcessImg> _baseRepository;
        private BaseCacheService _cache;

        public ProcessImgQueryHandler(BaseRepository<VmsProcessImg> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm list danh sách theo điều kiện tìm kiếm hoặc theo phân trang mặc định SqlSugar
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="command">Đối tượng PageVmsProcessImgInput</param>
        /// <returns>Danh sách PageVmsProcessImgOutput</returns>
        public async Task<SqlSugarPagedList<PageVmsProcessImgOutput>> HandleAsync(PageVmsProcessImgInput command)
        {
            var msgList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.ProcessId), msg => msg.ProcessId != null && msg.ProcessId.Contains(command.ProcessId))
                //.WhereIF(!string.IsNullOrEmpty(command.Code), msg => msg.Code != null && msg.Code.Contains(command.Code))
                //.WhereIF(!string.IsNullOrEmpty(command.Type), msg => msg.Type == command.Type)
                //.WhereIF(!string.IsNullOrEmpty(command.Lang), msg => msg.Lang == command.Lang)
                //.WhereIF(!string.IsNullOrEmpty(command.Content1), msg => msg.Content1 != null && msg.Content1.Contains(command.Content1))
                //.WhereIF(!string.IsNullOrEmpty(command.Content2), msg => msg.Content2 != null && msg.Content2.Contains(command.Content2))
                .Select((msg) => new PageVmsProcessImgOutput() { }, true)
                .ToPagedListAsync(command.Page, command.PageSize);

            return msgList;
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm list danh sách theo giá trị trường "code" truyền vào
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="command">Đối tượng VmsProcessImgInput</param>
        /// <returns>Danh sách VmsProcessImgOutput</returns>
        public async Task<List<VmsProcessImgOutput>> HandleAsync(VmsProcessImgInput command)
        {
            // TODO: Cache

            // Sắp xếp theo thứ tự tạo để đảm bảo thứ tự ảnh đồng nhất
            var msgList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.ProcessId), msg => msg.ProcessId != null && msg.ProcessId.Contains(command.ProcessId))
                .OrderBy(msg => msg.CreateTime)
                .OrderBy(msg => msg.ID)
                .Select((msg) => new VmsProcessImgOutput() { }, true)
                .ToListAsync();

            return msgList;
        }
    }
}
