using Modules.VMS.Controllers.EventDefault.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VMS.Controllers.EventDefault
{
    /// <summary>
    /// Author: 
    /// Description: Controller quản lý nội dung mặc định VMS theo sự kiện sự cố (Thêm/Sửa/Xóa/Phân trang/Danh sách).
    /// Created date: 15/07/2026
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class VmsEventDefaultController : BaseController
    {
        /// <summary>
        /// Phân trang danh sách nội dung mặc định theo sự kiện sự cố.
        /// Author: ChuongLP
        /// Date: 15/07/2026
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List VmsEventDefault based on pagination conditions")]
        public async Task<SqlSugarPagedList<PageVmsEventDefaultOutput>> Page(PageVmsEventDefaultInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageVmsEventDefaultOutput>>(input);
        }

        /// <summary>
        /// Tìm kiếm danh sách (không phân trang) nội dung mặc định theo sự kiện sự cố.
        /// Author: ChuongLP
        /// Date: 15/07/2026
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List VmsEventDefault based on search conditions")]
        public async Task<List<VmsEventDefaultOutput>> GetList([FromQuery] VmsEventDefaultListInput input)
        {
            return await MessBus.InvokeAsync<List<VmsEventDefaultOutput>>(input);
        }

        /// <summary>
        /// Thêm mới một bản ghi nội dung mặc định theo sự kiện sự cố.
        /// Author: ChuongLP
        /// Date: 15/07/2026
        /// </summary>
        /// <param name="input">Đối tượng AddVmsEventDefaultInput</param>
        [HttpPost("Add")]
        [DisplayName("Add a new VmsEventDefault record")]
        public async Task AddVmsEventDefault(AddVmsEventDefaultInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Cập nhật một bản ghi nội dung mặc định theo sự kiện sự cố.
        /// Author: ChuongLP
        /// Date: 15/07/2026
        /// </summary>
        /// <param name="input">Đối tượng UpdateVmsEventDefaultInput</param>
        [HttpPost("Update")]
        [DisplayName("Update a VmsEventDefault record")]
        public async Task UpdateVmsEventDefault(UpdateVmsEventDefaultInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Xóa một bản ghi nội dung mặc định theo sự kiện sự cố.
        /// Author: ChuongLP
        /// Date: 15/07/2026
        /// </summary>
        /// <param name="input">Đối tượng DeleteVmsEventDefaultInput</param>
        [HttpPost("Delete")]
        [DisplayName("Delete a VmsEventDefault record")]
        public async Task DeleteVmsEventDefault(DeleteVmsEventDefaultInput input)
        {
            await MessBus.InvokeAsync(input);
        }
    }
}
