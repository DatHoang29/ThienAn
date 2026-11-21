using Modules.VMS.Controllers.Message.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VMS.Controllers.Message
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class VmsMessageController: BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy danh sách theo phân trang
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageVmsMessageOutput>> Page(PageVmsMessageInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageVmsMessageOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy danh sách theo tham số
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("List")]
        [DisplayName("List based on parameters")]
        public async Task<List<VmsMessageOutput>> List(VmsMessageInput input)
        {
            return await this.MessBus.InvokeAsync<List<VmsMessageOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin theo id hoặc code
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Single")]
        [DisplayName("Get single based on id or code")]
        public async Task<VmsMessageOutput> Single(VmsMessageSingleInput input)
        {
            return await this.MessBus.InvokeAsync<VmsMessageOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới một record vào bảng
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a record to the table")]
        public async Task Add(AddVmsMessageInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record trong bảng
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record in the table")]
        public async Task Update(UpdateVmsMessageInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record trong bảng
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record in the table")]
        public async Task Delete(DeleteVmsMessageInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record trong bảng
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple records in the table")]
        public async Task BatchDelete(List<DeleteVmsMessageInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
