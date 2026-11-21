using Module.VideoWall.Core.Dto.WallPermission;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Controllers.WallPermission
{
    /// <summary>
    /// Description: API quản lý phân quyền khu vực màn hình theo người dùng hoặc đơn vị (VwWallPermission)
    /// Created date: 11/09/2026
    /// </summary>
    [ApiDescriptionSettings(GroupName, Order = 545)]
    public class VwWallPermissionController : BaseController
    {
        /// <summary>
        /// Lấy danh sách phân quyền khu vực màn hình (phân trang)
        /// </summary>
        [ApiDescriptionSettings(Name = "Page"), HttpPost]
        [DisplayName("Get VwWallPermission page list")]
        public virtual async Task<SqlSugarPagedList<VwPageWallPermissionOutput>> Page(VwPageWallPermissionInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<VwPageWallPermissionOutput>>(input);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách phân quyền khu vực màn hình (không phân trang)
        /// </summary>
        [DisplayName("Get VwWallPermission list")]
        public async Task<List<VwWallPermissionOutput>> GetList([FromQuery] VwWallPermissionInput input)
        {
            return await MessBus.InvokeAsync<List<VwWallPermissionOutput>>(input);
        }

        /// <summary>
        /// Lấy chi tiết phân quyền khu vực màn hình theo ID
        /// </summary>
        [DisplayName("Get VwWallPermission by id")]
        public async Task<VwWallPermissionOutput> GetById([FromQuery] VwIdWallPermissionInput input)
        {
            return await MessBus.InvokeAsync<VwWallPermissionOutput>(input);
        }

        /// <summary>
        /// Lấy vùng lưới hiệu lực của người dùng đang đăng nhập
        /// </summary>
        [ApiDescriptionSettings(Name = "GetMy"), HttpGet]
        [DisplayName("Get effective wall permission for current user")]
        public async Task<VwMyWallPermissionOutput> GetMy()
        {
            return await MessBus.InvokeAsync<VwMyWallPermissionOutput>(new VwGetMyWallPermissionInput());
        }

        /// <summary>
        /// Thêm mới phân quyền khu vực màn hình
        /// </summary>
        [ApiDescriptionSettings(Name = "Add"), HttpPost]
        [DisplayName("Add VwWallPermission")]
        public async Task AddVwWallPermission(VwAddWallPermissionInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Cập nhật phân quyền khu vực màn hình
        /// </summary>
        [ApiDescriptionSettings(Name = "Update"), HttpPost]
        [DisplayName("Update VwWallPermission")]
        public async Task UpdateVwWallPermission(VwUpdateWallPermissionInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Xóa phân quyền khu vực màn hình
        /// </summary>
        [ApiDescriptionSettings(Name = "Delete"), HttpPost]
        [DisplayName("Delete VwWallPermission")]
        public async Task DeleteVwWallPermission(VwDeleteWallPermissionInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Xóa nhiều phân quyền khu vực màn hình
        /// </summary>
        [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
        [DisplayName("Delete multiple VwWallPermission")]
        public async Task BatchDeleteVwWallPermission(List<VwDeleteWallPermissionInput> input)
        {
            await MessBus.InvokeAsync(input);
        }
    }
}
