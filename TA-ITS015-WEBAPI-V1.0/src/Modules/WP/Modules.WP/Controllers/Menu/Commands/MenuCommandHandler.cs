using Mapster;
using Modules.WP.Controllers.Menu.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Core.Exceptions;
using Modules.WP.Infrastructure;
using Shared.Core.Interfaces;
using Shared.Core.Utilities;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.DTO.Upload;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.WP.Controllers.Menu.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các lệnh ghi (Add/Update/Delete) cho menu hệ thống WP
    /// Created date: 25/06/2026
    /// </summary>
    public class MenuCommandHandler:IWolverineHandler
    {
        private BaseRepository<WpMenu> _menuRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public MenuCommandHandler(BaseRepository<WpMenu> menuRep,BaseCacheService cache, UserManager userManager)
        {
            _menuRep = menuRep;
            _cache = cache;
            _userManager = userManager;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới menu cho hệ thống WP.
        /// - Nếu là menu gốc (cha): Pid = "0"
        /// - Nếu là menu con: Pid = ID của menu cha (kiểm tra menu cha tồn tại)
        /// Created date: 25/06/2026
        /// </summary>
        public async Task HandleAsync(WpAddMenuInput command)
        {
            command.Name = command.Name?.Trim();
            command.Title = command.Title?.Trim();
            command.Code = command.Code?.Trim();

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                throw Oops.Oh(BaseMsg.BaseValidation.Required, BaseMsg.BaseEntity.Name);
            }

            command.Pid = await ValidateParentAsync(null, command.Pid);
            await ValidateDuplicateNameAsync(null, command.Name, command.Pid);
            NormalizeByType(command);

            command.CreateUId = null;
            await _menuRep.InsertWithDiffLogAsync(command.Adapt<WpMenu>());
            _cache.RemoveByPrefixKey(CacheConst.WP.WpMenu);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 Menu
        /// Created date: 14/08/2026
        /// </summary>


        public async Task HandleAsync(WpUpdateMenuInput command)
        {
            command.Name = command.Name?.Trim();
            command.Title = command.Title?.Trim();
            command.Code = command.Code?.Trim();

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                throw Oops.Oh(BaseMsg.BaseValidation.Required, BaseMsg.BaseEntity.Name);
            }

            var entity = await _menuRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                       ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Kiểm tra cha hợp lệ (không chọn chính nó/con-cháu làm cha) và trùng tên trong cùng cấp
            command.Pid = await ValidateParentAsync(command.ID, command.Pid);
            await ValidateDuplicateNameAsync(command.ID, command.Name, command.Pid);
            NormalizeByType(command);

            await _menuRep.UpdateWithDiffLogAsync(command.Adapt(entity));
            _cache.RemoveByPrefixKey(CacheConst.WP.WpMenu);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 Menu (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>

        public async Task HandleAsync(WpDeleteMenuInput command)
        {
            var sysMenu = await _menuRep.GetFirstAsync(u => u.ID == command.ID && u.IsDelete == null)
                 ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Không cho xóa menu cha khi còn menu con (tránh sinh node mồ côi)
            var hasChildren = await _menuRep.AsQueryable()
                .AnyAsync(u => u.Pid == command.ID && u.IsDelete == null);
            if (hasChildren)
            {
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse);
            }

            await _menuRep.SoftDeleteAsync<WpMenu>(sysMenu);
            _cache.RemoveByPrefixKey(CacheConst.WP.WpMenu);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều Menu (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>

        public async Task HandleAsync(List<WpDeleteMenuInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var count = await _menuRep.AsQueryable()
                .Where(u => ids.Contains(u.ID) && u.IsDelete == null)
                .CountAsync();

            if (count != ids.Distinct().Count())
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // Menu con còn sống mà không nằm trong danh sách xóa -> chặn (tránh sinh node mồ côi)
            var hasOtherChildren = await _menuRep.AsQueryable()
                .AnyAsync(u => ids.Contains(u.Pid) && !ids.Contains(u.ID) && u.IsDelete == null);
            if (hasOtherChildren)
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse);

            await _menuRep.SoftDeleteAsync<WpMenu>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.WP.WpMenu);

        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chuẩn hóa và kiểm tra menu cha.
        /// - Pid rỗng/"0" -> menu gốc ("0")
        /// - Menu cha phải tồn tại, không phải chính nó hoặc con-cháu của nó (tránh vòng lặp)
        /// Created date: 03/11/2026
        /// </summary>
        private async Task<string> ValidateParentAsync(string? id, string? pid)
        {
            pid = pid?.Trim();
            if (string.IsNullOrWhiteSpace(pid) || pid == "0")
            {
                return "0";
            }

            if (!string.IsNullOrWhiteSpace(id) && pid == id)
            {
                throw Oops.Oh(BaseMsg.BaseException.CannotSelfParent);
            }

            var parentExist = await _menuRep.AsQueryable()
                .AnyAsync(u => u.ID == pid && u.IsDelete == null);
            if (!parentExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.ParentNotExist);
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                var descendantIds = await GetDescendantIdsAsync(id);
                if (descendantIds.Contains(pid))
                {
                    throw Oops.Oh(BaseMsg.BaseException.CannotSelfParent);
                }
            }

            return pid;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Không cho trùng tên trong cùng một cấp (cùng Pid), bỏ qua bản ghi đã xóa và chính nó
        /// Created date: 03/11/2026
        /// </summary>
        private async Task ValidateDuplicateNameAsync(string? id, string name, string pid)
        {
            var isExist = await _menuRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(id), u => u.ID != id)
                .AnyAsync(u => u.Name == name && u.Pid == pid && u.IsDelete == null);

            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Name);
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chuẩn hóa dữ liệu theo loại menu.
        /// - dir (nhóm): không có route/component
        /// - Code mặc định = Name
        /// Created date: 03/11/2026
        /// </summary>
        private static void NormalizeByType(WpAddMenuInput command)
        {
            command.Path = command.Path?.Trim();
            command.Component = command.Component?.Trim();

            if (command.Type == "dir")
            {
                command.Path = null;
                command.Component = null;
            }

            if (string.IsNullOrWhiteSpace(command.Code))
            {
                command.Code = command.Name;
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy ID toàn bộ con-cháu của một menu (duyệt theo chiều rộng)
        /// Created date: 25/06/2026
        /// </summary>
        private async Task<HashSet<string>> GetDescendantIdsAsync(string id)
        {
            var all = await _menuRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .Select(u => new WpMenu { ID = u.ID, Pid = u.Pid })
                .ToListAsync();

            var result = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(id);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var child in all.Where(u => u.Pid == current))
                {
                    if (result.Add(child.ID))
                    {
                        queue.Enqueue(child.ID);
                    }
                }
            }
            return result;
        }
    }
}
