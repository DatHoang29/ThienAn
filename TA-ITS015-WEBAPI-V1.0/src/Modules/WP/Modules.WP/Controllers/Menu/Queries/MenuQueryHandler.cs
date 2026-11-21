using Mapster;
using Modules.WP.Controllers.Menu.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using SqlSugar;

namespace Modules.WP.Controllers.Menu.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các truy vấn đọc danh sách menu hệ thống WP (dạng cây, không phân trang)
    /// Created date: 25/06/2026
    /// </summary>
    public class MenuQueryHandler
    {
        private BaseRepository<WpMenu> _menuRep;
        private BaseCacheService _cache;

        public MenuQueryHandler(BaseRepository<WpMenu> menuRep, BaseCacheService cache)
        {
            _menuRep = menuRep;
            _cache = cache;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Menu theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<List<WpMenuOutput>> HandleAsync(WpMenuInput command)
        {
            // Pid được dùng làm gốc của cây (mặc định "0")
            var rootValue = string.IsNullOrWhiteSpace(command.Pid) ? "0" : command.Pid;

            var filtered = _menuRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Title), u => u.Title.Contains(command.Title))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Type), u => u.Type == command.Type)
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                // Sắp theo thứ tự hiển thị (OrderNo tăng dần), cùng thứ tự thì bản ghi tạo trước đứng trước
                .OrderBy(u => u.OrderNo)
                .OrderBy(u => u.CreateTime);

            // Có tìm kiếm nhưng không còn node gốc -> trả về list phẳng các bản ghi khớp
            
            if (!await filtered.AnyAsync(u => u.Pid == rootValue || u.Pid == null))
            {
                return (await filtered.ToListAsync()).Adapt<List<WpMenuOutput>>();
            }

            return (await filtered.ToTreeAsync(u => u.Children, u => u.Pid, rootValue))
                .Adapt<List<WpMenuOutput>>();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy cây menu rút gọn (ID/Title/Pid) để chọn menu cha (tree-select).
        /// Trả về cây đầy đủ; việc chặn chọn chính nó/cấp con làm cha được xử lý ở API Add.
        /// Created date: 25/06/2026
        /// </summary>
        public async Task<List<WpMenuTreeSelectOutput>> HandleAsync(WpMenuTreeSelectInput command)
        {
            var all = await _menuRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Type), u => u.Type == command.Type)
                .OrderBy(u => u.OrderNo)
                .OrderBy(u => u.CreateTime)
                .ToListAsync();

            return BuildTreeSelect(all, "0");
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Dựng cây tree-select (đệ quy) theo parentId
        /// Created date: 25/06/2026
        /// </summary>
        private List<WpMenuTreeSelectOutput> BuildTreeSelect(List<WpMenu> all, string parentId)
        {
            return all
                .Where(u => u.Pid == parentId)
                .Select(u => new WpMenuTreeSelectOutput
                {
                    ID = u.ID,
                    Title = u.Title,
                    Pid = u.Pid,
                    Children = BuildTreeSelect(all, u.ID)
                })
                .ToList();
        }
    }
}
