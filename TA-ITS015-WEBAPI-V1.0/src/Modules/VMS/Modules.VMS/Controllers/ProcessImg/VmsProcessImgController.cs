using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Modules.VMS.Controllers.ProcessImg.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VMS.Controllers.ProcessImg
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp controller xử lý nghiệp vụ cho đối tượng VmsProcessImg
    /// Created date: 14/10/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class VmsProcessImgController: BaseController
    {
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm list danh sách VmsProcessImg theo điều kiện phân trang SqlSugar
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="input">Đối tượng PageVmsProcessImgInput</param>
        /// <returns>Danh sách PageVmsProcessImgOutput</returns>
        [HttpPost("Page")]
        [DisplayName("List PageVmsProcessImgOutput based on pagination conditions")]
        public async Task<SqlSugarPagedList<PageVmsProcessImgOutput>> Page(PageVmsProcessImgInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageVmsProcessImgOutput>>(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm list danh sách VmsProcessImg theo điều kiện tìm kiếm                                                                                
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="input">Đối tượng PageVmsProcessImgInput</param>
        /// <returns>Danh sách PageVmsProcessImgOutput</returns>
        [HttpGet("List")]
        [DisplayName("List VmsProcessImgOutput based on search conditions")]
        public async Task<List<VmsProcessImgOutput>> GetList([FromQuery] VmsProcessImgInput input)
        {
            return await this.MessBus.InvokeAsync<List<VmsProcessImgOutput>>(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Thêm mới một record vào bảng VmsProcessImg
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="input">Đối tượng AddVmsProcessImgInput</param>
        /// <returns></returns>
        [HttpPost("Add")]
        [DisplayName("Add a new record to the VmsProcessImg table")]
        public async Task AddVmsProcessImg(AddVmsProcessImgInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Cập nhật dữ liệu cho bảng VmsProcessImg
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="input">Đối tượng UpdateVmsProcessImgInput</param>
        /// <returns></returns>
        [HttpPost("Update")]
        [DisplayName("Update the data for the VmsProcessImg table")]
        public async Task UpdateVmsProcessImg(UpdateVmsProcessImgInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa một dòng trong bảng VmsProcessImg
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="input">Đối tượng DeleteVmsProcessImgInput</param>
        /// <returns></returns>
        [HttpPost("Delete")]
        [DisplayName("Delete a record in the VmsProcessImg table")]
        public async Task DeleteVmsProcessImg(DeleteVmsProcessImgInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều dòng trong bảng VmsProcessImg
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="input">Danh sách DeleteVmsProcessImgInput</param>
        /// <returns></returns>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record in the VmsProcessImg table")]
        public async Task BatchDeleteVmsProcessImg(List<DeleteVmsProcessImgInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
