using Modules.VMS.Controllers.ImageVms.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VMS.Controllers.ImageVms
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class VmsImageController: BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageVmsImageOutput>> Page(PageVmsImageInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageVmsImageOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("List")]
        [DisplayName("List based on parameters")]
        public async Task<List<VmsImageOutput>> GetList(VmsImageInput input)
        {
            return await this.MessBus.InvokeAsync<List<VmsImageOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thông tin một record
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Single")]
        [DisplayName("Get single based on id or code")]
        public async Task<VmsImageOutput> Single(VmsImageSingleInput input)
        {
            return await this.MessBus.InvokeAsync<VmsImageOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới một record
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a record to the table")]
        public async Task Add(AddVmsImageInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record in the table")]
        public async Task Update(UpdateVmsImageInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record in the table")]
        public async Task Delete(DeleteVmsImageInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 01/12/2024
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple records in the table")]
        public async Task BatchDelete(List<DeleteVmsImageInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
