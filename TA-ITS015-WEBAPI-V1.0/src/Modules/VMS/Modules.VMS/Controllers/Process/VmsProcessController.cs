
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Controllers.ProcessImg.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Current;
using Modules.VMS.Infrastructure.Services.Process;
using Shared.Infrastructure.Persistence.SqlSugar;
using System.ComponentModel;
using System.Diagnostics;
using Wolverine;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Modules.VMS.Controllers.Process
{
	/// <summary>
	/// Author: KhoaND
	/// Description: Lớp controller xử lý nghiệp vụ cho đối tượng VmsProcess
	/// Created date: 14/10/2024
	/// </summary>
	[ApiController]
	[ApiDescriptionSettings(GroupName)]
	public class VmsProcessController : BaseController
	{
		//private readonly BaseRepository<VmsProcess> _baseRepository;
		//private readonly BaseRepository<VmsCurrent> _currentRepository;

		//private readonly IHttpContextAccessor _httpContextAccessor;
		//private VmsCurrentService _VmsCurrentService;	


		//public VmsProcessController(BaseRepository<VmsProcess> baseRepository, BaseRepository<VmsCurrent> currentReposity,
		//	IHttpContextAccessor httpContextAccessor, VmsCurrentService vmsCurrentService)
		//{
		//	_baseRepository = baseRepository;
		//	_httpContextAccessor = httpContextAccessor;
		//	_VmsCurrentService = vmsCurrentService;
		//	_currentRepository = currentReposity;
		//}
	

		/// <summary>
		/// Author: TuanHA
		/// Description: Lấy thông tin cấu hình và hình ảnh hiển thị của VMS
		/// Created date: 12/03/2025
		/// </summary>
		/// <param name="input">Đối tượng AddVmsProcessInput</param>
		/// <returns></returns>
		[HttpGet("GetInformationVMS")]
		[DisplayName("Get information about configuration and image display")]
		public async Task<List<InformationProcessOutput>> GetInformationVMS()
		{
			//switch (input.VmsType)
			//{
			//	case "timer": // Hẹn giờ
			//		var timerProcessInput = input.Adapt<TimerProcessInput>();
			//		await this.MessBus.InvokeAsync(timerProcessInput);
			//		break;
			//	case "schedule": // Lập lịch
			//		//var scheduleProcessInput = input.Adapt<ScheduleProcessInput>();
			//		var scheduleProcessInput = new ScheduleProcessInput();
			//		scheduleProcessInput.VmsIds = input.VmsIds;
			//		scheduleProcessInput.ListUrlScheduleProcess = input.ListUrlScheduleProcess;
			//		scheduleProcessInput.Brightness = input.Brightness;
			//		scheduleProcessInput.Priority = input.Priority;
			//		await this.MessBus.InvokeAsync(scheduleProcessInput);
			//		break;
			//	case "image":
   //                 var imageInput = input.Adapt<AddVmsCurrentInput>();
   //                 imageInput.Url = input.UrlImage;
   //                 imageInput.ListEquipment = new List<ListEquipment>();
   //                 imageInput.ActionType = input.VmsType;
   //                 foreach (var id in input.VmsIds)
   //                 {
   //                     imageInput.ListEquipment.Add(new ListEquipment() {EquipmentId = id});
   //                 }
   //                 await this.MessBus.InvokeAsync(imageInput);
			//		break;
			//	default:
			//		break;
			//}
		
			var input = new InformationProcessInput();
			return await this.MessBus.InvokeAsync<List<InformationProcessOutput>>(input);
		}


		/// <summary>
		/// Author: TuanHA
		/// Description: Kiểm tra xem có tiến trình VMS nào đang chạy không
		/// Created date: 13/03/2025
		/// </summary>
		[HttpGet("CheckVmsRunningSchedule")]
		[DisplayName("Get information about vms running schedule")]
		public async Task<List<ProcessStatusOutput>> CheckVmsProcessRunningSchedule()
		{
			return await this.MessBus.InvokeAsync<List<ProcessStatusOutput>>(new ProcessStatusInput());
		}

		/// <summary>
		/// Author: ChuongLP
		/// Description: Lấy danh sách các hình ảnh đang hiển thị/append trên thiết bị phục vụ modal điều khiển sự cố
		/// Created date: 2026-10-15
		/// </summary>
		[HttpPost("GetActivePlaylistByEquipment")]
		[DisplayName("Get active playlist by equipment list")]
		public async Task<List<EquipmentPlaylistOutput>> GetActivePlaylistByEquipment([FromBody] GetActivePlaylistInput input)
		{
			return await this.MessBus.InvokeAsync<List<EquipmentPlaylistOutput>>(input);
		}
	}
}
