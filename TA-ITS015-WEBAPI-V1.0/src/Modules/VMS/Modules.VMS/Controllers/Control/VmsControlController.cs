using Modules.VMS.Controllers.Control.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.VMS.Controllers.Control
{

	/// <summary>
	/// Điều khiển thiết bị VMS
	/// Author: SonTH
	/// Created date: 14/03/2025
	/// </summary>
	[ApiController]
	[ApiDescriptionSettings(GroupName)]
	public class VmsControlController : BaseController
	{
		/// <summary>
		/// Author: TuanHA
		/// Description: Thêm mới một record vào bảng VmsProcess
		/// Created date: 21/02/2024
		/// </summary>
		/// <param name="input">Đối tượng AddVmsProcessInput</param>
		/// <returns></returns>
		[HttpPost("Add")]
		[DisplayName("Add a new record to the VmsProcess table")]
		public async Task AddVmsProcess(AddControlInput input)
		{
			await MessBus.InvokeAsync(input);
		}

        /// <summary>
        /// Description: Sinh ảnh preview VMS trả base64 (không lưu file)
		/// Author: ChuongLP
        /// Date: 2026-06-24
        /// </summary>
        [HttpPost("Preview")]
        [DisplayName("Generate VMS preview image (base64)")]
        public async Task<PreviewOutput> Preview(PreviewInput input)
        {
            return await MessBus.InvokeAsync<PreviewOutput>(input);
        }

    }
}
