#region using reference

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Process;
using Shared.Infrastructure.Persistence.SqlSugar;

#endregion

namespace Modules.VMS.Controllers.Current
{

    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp controller xử lý nghiệp vụ cho đối tượng VmsCurrent
    /// Created date: 18/10/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class VmsCurrentController: BaseController
    {
        private readonly IWebHostEnvironment _env;
        private readonly BaseRepository<Core.Entities.VmsCurrent> _baseRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly VmsProcessService _vmsProcessService;

        public VmsCurrentController(IWebHostEnvironment env, BaseRepository<Core.Entities.VmsCurrent> baseRepository, 
                               IHttpContextAccessor httpContextAccessor, VmsProcessService vmsProcessService)
        {
            _env = env;
            _baseRepository = baseRepository;
            _httpContextAccessor = httpContextAccessor;
            _vmsProcessService = vmsProcessService;

        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm list danh sách VmsCurrent theo điều kiện phân trang SqlSugar
        /// Created date: 18/10/2024
        /// </summary>
        /// <param name="input">Đối tượng PageVmsCurrentInput</param>
        /// <returns>Danh sách PageVmsCurrentOutput</returns>
        [HttpPost("Page")]
        [DisplayName("List PageVmsCurrentOutput based on pagination conditions")]
        public async Task<SqlSugarPagedList<PageVmsCurrentOutput>> Page(PageVmsCurrentInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageVmsCurrentOutput>>(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm list danh sách VmsCurrent theo điều kiện tìm kiếm
        /// Created date: 18/10/2024
        /// </summary>
        /// <param name="input">Đối tượng PageVmsCurrentInput</param>
        /// <returns>Danh sách PageVmsCurrentOutput</returns>
        [HttpGet("List")]
        [DisplayName("List VmsCurrentOutput based on search conditions")]
        public async Task<List<VmsCurrentOutput>> GetList([FromQuery] VmsCurrentInput input)
        {
            return await this.MessBus.InvokeAsync<List<VmsCurrentOutput>>(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Thêm mới một record vào bảng VmsCurrent
        /// Created date: 18/10/2024
        /// </summary>
        /// <param name="input">Đối tượng AddVmsCurrentInput</param>
        /// <returns></returns>
        [HttpPost("Add")]
        [DisplayName("Add a new record to the VmsCurrent table")]
        public async Task AddVmsCurrent(AddVmsCurrentInput input)
        {
            //var authorizationHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
            //string token = "";

            //if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            //{
            //    token = authorizationHeader.Substring("Bearer ".Length).Trim();
            //}
            //foreach (var vmsId in input.ListEquipment)
            //{
            //    Core.Entities.VmsCurrent msg = await this._baseRepository.GetByIdAsync(input.ID);

            //    // Iterate over each URL in ProcessImgInput.listUrl
            //    if (this._VmsCurrentService.IsBase64String(input.Url))
            //    {
            //        input.Url = await this._VmsCurrentService.SaveImageAsync(input.Url, msg.ID);
            //    }
            //    else
            //    {
            //        input.Url = this._VmsCurrentService.WriteTextToImage(input, input.BackColor, 100);
            //    }
            //    var currentInput = new AddVmsCurrentInput
            //    {
            //        EquipmentId = vmsId.EquipmentId,
            //        Code = vmsId.Code,
            //        Name = vmsId.Name,  // Add 7 hours to TimeEnd
            //        Url = input.Url,
            //        Priority= input.Priority, // Add 7 hours to TimeStart
            //        RowData = input.RowData,
            //        IncidentId = input.IncidentId,
            //        BackColor = input.BackColor,
            //        Color = input.Color,
            //    };
            //    await this.MessBus.InvokeAsync(currentInput);
            //    await _VmsProcessService.logLastImageAsync(vmsId.EquipmentId, input.Url, input.RowData, token, "User", null, 0, input.IncidentId); // Pass the last image URL
            //}

			await this.MessBus.InvokeAsync(input);
		}

        /// <summary>
        /// Author: KhoaND
        /// Description: Cập nhật dữ liệu cho bảng VmsCurrent
        /// Created date: 18/10/2024
        /// </summary>
        /// <param name="input">Đối tượng UpdateVmsCurrentInput</param>
        /// <returns></returns>
        [HttpPost("Update")]
        [DisplayName("Update the data for the VmsCurrent table")]
        public async Task UpdateVmsCurrent(UpdateVmsCurrentInput input)
        {
            //var authorizationHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
            //string token = "";

            //if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            //{
            //    token = authorizationHeader.Substring("Bearer ".Length).Trim();
            //}
            //foreach (var vmsId in input.ListEquipment)
            //{
            //    // Iterate over each URL in ProcessImgInput.listUrl
            //    Core.Entities.VmsCurrent msg = await this._VmsCurrentService.GetvmsCurrentEquipmentIDAsync(vmsId.EquipmentId);
            //    if (this._VmsCurrentService.IsBase64String(input.Url))
            //    {
            //        input.Url = await this._VmsCurrentService.SaveImageAsync(input.Url, msg.ID);
            //    }
            //    else
            //    {
            //        input.Url = this._VmsCurrentService.WriteTextToImage(input, input.BackColor, 100);
            //    }
            //    var currentInput = new UpdateVmsCurrentInput
            //    {
            //        ID = msg.ID,
            //        EquipmentId = vmsId.EquipmentId,
            //        Code = vmsId.Code,
            //        Name = vmsId.Name,  
            //        Url = input.Url,
            //        Priority = input.Priority,
            //        DefaultUrl = msg.DefaultUrl,
            //        RowData = input.RowData,
            //        OldRowData = input.OldRowData,
            //        OldUrl = input.OldUrl,
            //        IncidentId = input.IncidentId,
            //        BackColor = input.BackColor,
            //        Color = input.Color,
                                      
            //    };
            //    await this.MessBus.InvokeAsync(currentInput);
            //    _VmsProcessService.logLastImageAsync(vmsId.EquipmentId, input.Url, input.RowData, token, "User",null,0,input.IncidentId); // Pass the last image URL
            //}

			await this.MessBus.InvokeAsync(input);
		}

        [HttpPost("UpdateByProcess")]
        [DisplayName("Update the data for the VmsCurrent table")]
        public async Task UpdateVmsCurrent(UpdateVmsCurrentInputByProcess input)
        {
			await this.MessBus.InvokeAsync(input);
		}


		[HttpGet("GetByEquipment")]
		[DisplayName("Update the data for the VmsCurrent table")]
		public async Task<VmsCurrentOutput> GetByEquipment([FromQuery] VmsCurrentByEquipment input)
		{
			return await this.MessBus.InvokeAsync<VmsCurrentOutput>(input);
		}

		/// <summary>
		/// Author: KhoaND
		/// Description: Hàm xóa một dòng trong bảng VmsCurrent
		/// Created date: 18/10/2024
		/// </summary>
		/// <param name="input">Đối tượng DeleteVmsCurrentInput</param>
		/// <returns></returns>
		[HttpPost("Delete")]
        [DisplayName("Delete a record in the VmsCurrent table")]
        public async Task DeleteVmsCurrent(DeleteVmsCurrentInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều dòng trong bảng VmsCurrent
        /// Created date: 18/10/2024
        /// </summary>
        /// <param name="input">Danh sách DeleteVmsCurrentInput</param>
        /// <returns></returns>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record in the VmsCurrent table")]
        public async Task BatchDeleteVmsCurrent(List<DeleteVmsCurrentInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }


        [HttpPost("Restart/{Incidentid}")]
        [DisplayName("Retrieve a list of SysConfigData based on field code")]
        public async Task RestartEquipment(string Incidentid)
        {
            await _vmsProcessService.ResetIncidentToDefault(Incidentid);

            //var authorizationHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
            //string token = "";

            //if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            //{
            //    token = authorizationHeader.Substring("Bearer ".Length).Trim();
            //}
            //// Check if the input Incidentid is null or empty
            //if (string.IsNullOrWhiteSpace(Incidentid))
            //{
            //    return;
            //}

            //// Fetch incident logs by ID
            //var IncidentID = await _VmsProcessService.GetIncidentAsync(Incidentid,token);

            //// Check if the IncidentID result is null or empty
            //if (IncidentID == null || !IncidentID.Any())
            //{
            //    return;
            //}

            //foreach (var item in IncidentID)
            //{
            //    var VmsCurrent = await _VmsCurrentService.GetvmsCurrentEquipmentIDAsync(item.EquipmentID);

            //    var currentInput = new Core.Entities.VmsCurrent
            //    {
            //        ID = VmsCurrent.ID,
            //        EquipmentId = item.EquipmentID,
            //        Code = VmsCurrent.Code,
            //        Name = VmsCurrent.Name,
            //        Url = VmsCurrent.DefaultUrl,
            //        DefaultUrl = VmsCurrent.DefaultUrl,
            //        RowData = VmsCurrent.DefaultRowData,
            //        DefaultRowData = VmsCurrent.DefaultRowData,
            //        Color = VmsCurrent.DefaultColor,
            //        DefaultColor = VmsCurrent.DefaultColor,
            //        DefaultBackColor = VmsCurrent.DefaultBackColor,
            //        BackColor = VmsCurrent.DefaultBackColor,
            //    };

            //    await _VmsCurrentService.UpdateVmsCurrent(currentInput);
            //    _VmsProcessService.logLastImageAsync(item.EquipmentID, VmsCurrent.DefaultUrl, VmsCurrent.RowData, token, "Reset"); // Pass the last image URL
            //    ItsDataTransporter.BroadcastData(TypeOfItsEvent.VmsUpdate, new { ActionType = "vms-success", Data = currentInput });
            //}

            //await _VmsCurrentService.UpdateVmsCurrent(null, incidentid: Incidentid);
        }

        /// <summary>
        /// Thêm mới hoặc cập nhật nội dung mặc định cho một bản ghi VmsCurrent
        /// Author: ChuongLP
        /// Date: 14/07/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("AddDefaultContent")]
        [DisplayName("Save (upsert) default content for a VmsCurrent record")]
        public async Task SaveDefaultContent(VmsDefaultCurrentInput input)
        {
            await MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Xóa nội dung mặc định cho một bản ghi VmsCurrent
        /// Author: ChuongLP
        /// Date: 14/07/2026
        /// </summary>
        [HttpPost("DeleteDefaultContent")]
        [DisplayName("Clear default content of a VmsCurrent record")]
        public async Task DeleteDefaultContent(DeleteVmsDefaultCurrentInput input)
        {
            await MessBus.InvokeAsync(input); 
        }

        /// <summary>
        /// Phân trang danh sách nội dung mặc định VMS (trang quản lý)
        /// Author: ChuongLP
        /// Date: 14/07/2026
        /// </summary>
        /// <param name="input">Đối tượng PageVmsDefaultCurrentInput</param>
        /// <returns>Danh sách PageVmsDefaultCurrentOutput</returns>
        [HttpPost("PageDefaultContent")]
        [DisplayName("List default content of VmsCurrent based on pagination conditions")]
        public async Task<SqlSugarPagedList<PageVmsDefaultCurrentOutput>> PageDefaultContent(PageVmsDefaultCurrentInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageVmsDefaultCurrentOutput>>(input);
        }

        /// <summary>
        /// Tìm kiếm danh sách nội dung mặc định VMS theo điều kiện (không phân trang)
        /// Author: ChuongLP
        /// Date: 14/07/2026
        /// </summary>
        /// <param name="input">Đối tượng VmsDefaultCurrentListInput</param>
        /// <returns>Danh sách VmsDefaultCurrentOutput</returns>
        [HttpGet("ListDefaultContent")]
        [DisplayName("List default content of VmsCurrent based on search conditions")]
        public async Task<List<VmsDefaultCurrentOutput>> GetListDefaultContent([FromQuery] VmsDefaultCurrentListInput input)
        {
            return await MessBus.InvokeAsync<List<VmsDefaultCurrentOutput>>(input);
        }
    }
}
