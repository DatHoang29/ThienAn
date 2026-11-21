using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Current;
using Modules.VMS.Infrastructure.Services.Process;
using Modules.VMS.Infrastructure.Services.Schedule;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;

namespace Modules.VMS.Controllers.Process.Commands
{
	public class TimerCommandHandler : IWolverineHandler
	{
		private readonly BaseRepository<VmsProcess> _vmsProcessRepository;
		private readonly ScheduleService _scheduleService;
		private readonly VmsProcessService _VmsProcessService;
		private readonly BaseRepository<VmsProcessImg> _vmsProcessImgRepository;
		private readonly IConfiguration _config;
		private readonly string _url;
		private readonly ILogger<TimerCommandHandler> _logger;

		// Dùng để gửi dữ liệu ra thiết bị và nhận lại event kết quả
		private ManageSendToDevice manageSendToDivice = new ManageSendToDevice();
	

		public TimerCommandHandler(BaseRepository<VmsProcess> vmsProcessRepository,
			UserManager userManager, VmsProcessService VmsProcessService,
			ScheduleService scheduleService, BaseRepository<VmsProcessImg> vmsProcessImgRepository, IConfiguration config,
			ILogger<TimerCommandHandler> logger)
		{
			_vmsProcessRepository = vmsProcessRepository;
			_VmsProcessService = VmsProcessService;
			_vmsProcessImgRepository = vmsProcessImgRepository;
			_config = config;
            _scheduleService = scheduleService;
            _logger = logger;
        }

        #region Hẹn giờ
        /// <summary>
        /// Author: TuanHA, ChuongLP
        /// Description: Hàm bắt đầu xử lý hẹn giờ
        /// Created date: 21/02/2025, Update date: 03/09/2026
        /// </summary>
        /// <param name="input">Đối tượng TimerProcessInput</param>
        /// <returns></returns>
        public async Task HandleAsync(TimerProcessInput command)
		{
			// 1. Kiểm tra thông tin thiết lập
			// 1.1. Kiểm tra thiết bị có được chọn hay không
			if(command.EquipmentIds.Count < 1)
			{
				// TODO: Thiết bị chưa được chọn
				throw Oops.Oh(BaseMsg.VmsProcess.Validation.NoDeviceSelected);
			}

			// 1.2. Kiểm tra thiết bị có tồn tại hay không (lấy dữ liệu qua MessBus thay cho BaseApiClient)
			await _VmsProcessService.ValidateEquipmentExist(command.EquipmentIds);

			// 1.3. Kiểm tra thời gian bắt đầu và thời gian kết thúc không được bằng nhau
			if (command.TimeStart.HasValue && command.TimeEnd.HasValue && command.TimeStart == command.TimeEnd)
			{
				// Thông báo thời gian bắt đầu và kết thúc không được trùng nhau
				throw Oops.Oh(BaseMsg.VmsProcess.Validation.TimeStartAndTimeEndCanNotBeTheSame);
			}

			// 1.3b. Bắt buộc có cả thời gian bắt đầu và kết thúc (tránh NullReferenceException ở bước lưu / tạo job)
			if (!command.TimeStart.HasValue)
				throw Oops.Oh(BaseMsg.BaseException.NotFound, BaseMsg.VmsProcess.Entity.TimeStart);
			if (!command.TimeEnd.HasValue)
				throw Oops.Oh(BaseMsg.BaseException.NotFound, BaseMsg.VmsProcess.Entity.TimeEnd);

			// 1.4. Kiểm tra nội dung hình ảnh tồn tại hay ko
			var pathRoot = FileServerUtil.GetPath(FileServerUtil.PathTypeEnum.WebRoot);
			var filePath = string.Empty;
			var urlImage = string.Empty;
			if (!string.IsNullOrEmpty(command.Url))
			{
				filePath = pathRoot + command.Url;
				urlImage = command.Url;
			}

			// Check dưới ổ cứng
			//if (!File.Exists(filePath))
			//{
			//  // Thông báo hình ảnh không tồn tại
			//	throw Oops.Oh(BaseMsg.BaseException.NotFound, BaseMsg.VmsProcess.Entity.UrlImage);
			//}

			// Check with base url get image
			var isExistsImage = await FileServerUtil.UrlFileExistAsync(filePath);
			if (!isExistsImage)
			{
				// Thông báo hình ảnh không tồn tại
				throw Oops.Oh(BaseMsg.BaseException.NotFound, BaseMsg.VmsProcess.Entity.UrlImage);
			}


			// 2 + 3. Xóa timer cũ và ghi timer mới TRONG CÙNG MỘT transaction:
			// lỗi ở bước ghi thì timer cũ được giữ nguyên, không để mất timer mà vẫn báo thành công.
			try
			{
				_vmsProcessRepository.Context.Ado.BeginTran();

				// 2. Xóa tất cả tiến trình hẹn giờ cũ (type = timer), bảng VmsProcessImg trước
				var listIdVmsProcess = await _vmsProcessRepository.AsQueryable().Where(msg => msg.Type == "timer")
																		  .Select(msg => msg.ID)
																		  .ToListAsync();
				if (listIdVmsProcess != null && listIdVmsProcess.Count > 0)
				{
					await _vmsProcessImgRepository.AsDeleteable().Where(msg => listIdVmsProcess.Contains(msg.ProcessId)).ExecuteCommandAsync();
					await _vmsProcessRepository.AsDeleteable().Where(msg => listIdVmsProcess.Contains(msg.ID) && msg.Type == "timer").ExecuteCommandAsync();
				}

				// 3. Thiết lập mới trong bảng VMSProcess và VMSProcessImg
				foreach (var vmsId in command.EquipmentIds)
				{
					var vmsProcess = new VmsProcess
					{
						VmsId = vmsId,
						Type = "timer",
						State = "stop",
						TimeStart = BaseUtil.TimeServer.GetTimeServer(command.TimeStart.Value),
						TimeEnd = BaseUtil.TimeServer.GetTimeServer(command.TimeEnd.Value),
						Brightness = command.Brightness,
						ThreadId = "Start|Stop|ScheduleVms",
						Priority = command.Priority
					};
					await _vmsProcessRepository.InsertAsync(vmsProcess);

					var vmsProcessImg = new VmsProcessImg
					{
						ProcessId = vmsProcess.ID,
						Url = urlImage
					};
					await _vmsProcessImgRepository.InsertAsync(vmsProcessImg);
				}
				_vmsProcessRepository.Context.Ado.CommitTran();
			}
			catch (Exception)
			{
				_vmsProcessRepository.Context.Ado.RollbackTran();

				// Báo lỗi lên cho người dùng, KHÔNG tạo job Hangfire cho dữ liệu chưa lưu được
				throw;
			}

			// 4. Cấu hình thiết lập trong Hangfire với thời gian mới			
			CreateHangFireJobs(command);		
		}

		public void CreateHangFireJobs(TimerProcessInput command)
		{
			var timerStart = BaseUtil.TimeServer.GetTimeServer(command.TimeStart.Value);
			var jobStartName = $"StartScheduleVms";
			RecurringJob.AddOrUpdate(jobStartName,
				() => StartTimerVms(),
				Cron.Daily(timerStart.Value.Hour, timerStart.Value.Minute),
				new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });

			var timerEnd = BaseUtil.TimeServer.GetTimeServer(command.TimeEnd.Value);
			var jobEndName = $"StopScheduleVms";
			RecurringJob.AddOrUpdate(jobEndName,
				() => StopTimerVms(),
				Cron.Daily(timerEnd.Value.Hour, timerEnd.Value.Minute),
				new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
		}


        /// <summary>
        /// Author: TuanHA, ChuongLP 
		/// Hàm xử lý bắt đầu tiến trình hẹn giờ
		/// Date: 21/02/2025, Update date: 03/09/2026
        /// </summary>
        public async Task StartTimerVms()
		{
			// 1. Lấy danh sách tiến trình hẹn giờ có Type = "timer" và State = "stop"			
			List<VmsProcess> processList = await _vmsProcessRepository.AsQueryable().Where(a => a.Type == "timer").ToListAsync();

			if (processList.Count() == 0)
			{
				return; // Không có process nào cần xử lý
			}

            // Lấy danh sách VmsId hiện tại
            var listVmsId = processList.Select(l => l.VmsId).ToList();

            // Lấy danh sách thiết bị đang xử lý sự cố ============ BỔ SUNG XỬ LÝ SỰ CỐ THÌ KHÔNG GỬI ẢNH - ChuongLP - 2026/09/03
            var incidentEquipmentId = await _VmsProcessService.GetEquipmentIdIncident(listVmsId);
            var listVmsIdActive = listVmsId.Where(id => !incidentEquipmentId.Contains(id)).ToList(); // Danh sách thiết bị đang hoạt động (không có sự cố)
            if (listVmsIdActive.Count == 0)
                return; // Không có thiết bị nào cần xử lý
            // ===============

            // 2. Cập nhật trạng thái của các process từ State = "stop" sang State = "run"			
            await _vmsProcessRepository.AsUpdateable()
				.Where(a => a.Type == "timer" && listVmsIdActive.Contains(a.VmsId))
				.SetColumns(s => s.State, "run").ExecuteCommandAsync();
			
            #region Buoc 3. Dừng các tiến trình lập lịch
            //// 3. Dừng các tiến trình lập lịch
            //// TODO: Ham rieng		
            //// Tìm các process có State = "run" và Type = "schedule" (lập lịch) để dừng
            //// Cập nhật các tiến trình lập lịch State về stop
            //await _vmsProcessRepository.AsUpdateable().Where(l => listVmsId.Contains(l.VmsId) && l.State == "run" && l.Type == "schedule")
            //						.SetColumns(l => l.State, "stop")
            //						.ExecuteCommandAsync();

            //// Dừng thread chạy tiến trình lập lịch
            //foreach(var vmsId in listVmsId)
            //{
            //	ScheduleCommandHandler.StopThreadRunSchedule(vmsId);
            //}

            #endregion
				
            // Gọi dừng lập lịch từ VMS Control Service 
            await _scheduleService.StopScheduleByVmsIds(listVmsIdActive);

            // 4. Chuẩn bị nội dung thay đổi VMS
            // lấy danh sách Process ID
            var listProcessID = processList.Select(p => p.ID).ToList();
			var listEquipmentInfos = await _VmsProcessService.GetListEquipmentInfo(processList);  // Thông tin biết bị , gửi cho và gửi bằng cách nào
			if (listEquipmentInfos == null || listEquipmentInfos.Count == 0)
			{
				_logger.LogWarning("Hẹn giờ VMS bắt đầu: không lấy được thông tin thiết bị cho {Count} biển, bỏ qua gửi nội dung", listVmsIdActive.Count);
				return;
			}

			// Mọi biển dùng chung 1 cấu hình hẹn giờ (cùng ảnh/độ sáng/priority) => gửi 1 gói cho tất cả,
			// dịch vụ xử lý song song thay vì chờ tuần tự từng biển.
			PrepareVmsData prepareVmsData = new PrepareVmsData();
			prepareVmsData.ListEquipmentInfos = listEquipmentInfos.Adapt<List<EquipmentInfoInput>>();
			// Lấy độ sáng của process đầu tiên (hoặc giá trị mặc định)
			prepareVmsData.ContentInfoInput.Brightness = processList.FirstOrDefault()?.Brightness ?? 100;
			prepareVmsData.ContentInfoInput.ThreadId = processList.First()?.ThreadId ?? string.Empty;
			prepareVmsData.ContentInfoInput.Priority = processList.First()?.Priority ?? 1;
			prepareVmsData.ContentInfoInput.Url = await _VmsProcessService.GetUrlImageInProcessImage(listProcessID);
			prepareVmsData.ProcessInfoInput.ProcessType = "timer";  // (timer/schedule/manual)
			prepareVmsData.ProcessInfoInput.ActionType = "start";

			// Thay đổi nội dung VMS
			var isAllSuccess = await _VmsProcessService.ChangeVmsContentMulti(prepareVmsData, listVmsIdActive);
			if (!isAllSuccess)
				_logger.LogWarning("Hẹn giờ VMS bắt đầu: có biển gửi nội dung thất bại (xem nhật ký điều khiển)");
		}

		public async Task StopTimerVms()
		
		{
			// 1. Lấy danh sách tiến trình hẹn giờ có Type = "timer" và State = "run"		
			var processList = await _vmsProcessRepository.AsQueryable().Where(a => a.State == "run" && a.Type == "timer").ToListAsync();

			if (processList.Count == 0)
			{
				return; // Không có process nào cần xử lý
			}

            //// Lấy danh sách VmsId hiện tại
            var listVmsId = processList.Select(l => l.VmsId).ToList();

            // Lấy danh sách thiết bị đang xử lý sự cố ============ BỔ SUNG XỬ LÝ SỰ CỐ THÌ KHÔNG GỬI ẢNH - ChuongLP - 2026/09/03
            var incidentEquipmentId = await _VmsProcessService.GetEquipmentIdIncident(listVmsId);
            var listVmsIdActive = listVmsId.Where(id => !incidentEquipmentId.Contains(id)).ToList(); // Danh sách thiết bị đang hoạt động (không có sự cố)
            if (listVmsIdActive.Count == 0)
                return; // Không có thiết bị nào cần xử lý
                        // ================================================================

            // 2. Cập nhật trạng thái của các process từ State = "run" sang State = "stop"
            await _vmsProcessRepository.AsUpdateable()
				.Where(a => a.State == "run" && a.Type == "timer" && listVmsIdActive.Contains(a.VmsId))
				.SetColumns(s => s.State, "stop").ExecuteCommandAsync();
			

            #region Buoc 3. Dừng các tiến trình lập lịch
            //// 3. Dừng các tiến trình lập lịch
            //// TODO: Ham rieng
            //// Tìm các process có State = "run" và Type = "schedule" (lập lịch) để dừng
            //// Cập nhật các tiến trình lập lịch State về stop
            //await _vmsProcessRepository.AsUpdateable().Where(l => listVmsId.Contains(l.VmsId) && l.State == "run" && l.Type == "schedule")
            //	.SetColumns(l => l.State, "stop")
            //	.ExecuteCommandAsync();

            //// Dừng thread chạy tiến trình lập lịch
            //foreach (var vmsId in listVmsId)
            //{
            //	ScheduleCommandHandler.StopThreadRunSchedule(vmsId);
            //}

            #endregion

            await _scheduleService.StopScheduleByVmsIds(listVmsIdActive);

            // 4. Chuẩn bị nội dung thay đổi VMS
            var listEquipmentInfos = await _VmsProcessService.GetListEquipmentInfo(processList); // Lấy 1 lần, dùng chung cho mọi nhóm
			if (listEquipmentInfos == null || listEquipmentInfos.Count == 0)
			{
				_logger.LogWarning("Hẹn giờ VMS kết thúc: không lấy được thông tin thiết bị cho {Count} biển, bỏ qua trả ảnh mặc định", listVmsIdActive.Count);
				return;
			}

            // ====== Bổ sung xử lý lấy hình ảnh Default khi dừng tiến trình hẹn giờ - ChuongLP - 2026/09/03
			// Mỗi biển có ảnh mặc định riêng => gom các biển cùng ảnh mặc định thành 1 gói để dịch vụ gửi song song.
			var defaultUrlByVms = new Dictionary<string, string>();
			foreach (var vmsId in listVmsIdActive.Distinct())
			{
				var vmsCurrent = await _VmsProcessService.GetVmsCurrentByEquipment(vmsId);
				defaultUrlByVms[vmsId] = string.IsNullOrEmpty(vmsCurrent?.DefaultUrl) ? @"\Upload\NoImage.bmp" : vmsCurrent.DefaultUrl;
			}
            // ====== Kết thúc bổ sung xử lý lấy hình ảnh Default khi dừng tiến trình hẹn giờ

			// Các nhóm gửi lần lượt (thường chỉ 1-2 ảnh mặc định), trong mỗi nhóm dịch vụ đã xử lý song song.
			// Không chạy song song các nhóm để tránh dùng chung connection SqlSugar giữa nhiều nhánh.
			var isAllSuccess = true;
			foreach (var group in defaultUrlByVms.GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase))
			{
				PrepareVmsData prepareVmsData = new PrepareVmsData();
				prepareVmsData.ListEquipmentInfos = listEquipmentInfos;
				prepareVmsData.ContentInfoInput.Url = group.Key;
				// Lấy độ sáng của process đầu tiên (hoặc giá trị mặc định)
				prepareVmsData.ContentInfoInput.Brightness = processList.FirstOrDefault()?.Brightness ?? 100;
				prepareVmsData.ContentInfoInput.ThreadId = processList.First()?.ThreadId ?? string.Empty;
				prepareVmsData.ContentInfoInput.Priority = processList.First()?.Priority ?? 1;
				prepareVmsData.ProcessInfoInput.ProcessType = "timer";
				prepareVmsData.ProcessInfoInput.ActionType = "stop";

				// Thay đổi nội dung VMS
				if (!await _VmsProcessService.ChangeVmsContentMulti(prepareVmsData, group.Select(kv => kv.Key).ToList()))
					isAllSuccess = false;
			}

			if (!isAllSuccess)
				_logger.LogWarning("Hẹn giờ VMS kết thúc: có biển trả ảnh mặc định thất bại (xem nhật ký điều khiển)");
		}
		#endregion
	}
}
