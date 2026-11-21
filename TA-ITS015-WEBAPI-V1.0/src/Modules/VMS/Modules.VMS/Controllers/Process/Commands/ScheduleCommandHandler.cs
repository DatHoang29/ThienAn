using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Process;
using Modules.VMS.Infrastructure.Services.Schedule;
using Modules.TMS.Core.Entities;
using Shared.DTO.Constants.Module;
using Shared.Utility.Utilities;
using System.Diagnostics;

namespace Modules.VMS.Controllers.Process.Commands
{
	public class ScheduleCommandHandler : IWolverineHandler
	{
		private readonly ScheduleService _scheduleService;
		private readonly BaseRepository<VmsProcess> _vmsProcessRepository;
		private readonly VmsProcessService _VmsProcessService;
		private readonly BaseRepository<VmsProcessImg> _vmsProcessImgRepository;
		private readonly IWebHostEnvironment _env;
		private readonly IConfiguration _config;
		public ILogger<ScheduleCommandHandler> _logger;


		// Dùng để lưu danh sách thiết bị
		List<TmsEquipment> equipmentList = new List<TmsEquipment>();

		#region Khai báo biến dùng cho lập lịch
		///// <summary>
		///// Author: TuanHA
		///// Description: Thread chạy lập lịch
		///// Created date: 06/03/2025
		///// </summary>

		//public static Dictionary<string, CancellationTokenSource> CancellationTokenSourceDictionary = new Dictionary<string, CancellationTokenSource>();

		//public static Dictionary<string, Task> TaskDictionary = new Dictionary<string, Task>();

		//public static Dictionary<string, int> RunningProcess = new Dictionary<string, int>();

		//private static Dictionary<string, int> StartProcess = new Dictionary<string, int>();

		//public static Dictionary<string, Thread> ThreadDictionary = new Dictionary<string, Thread>();

		#endregion

		public ScheduleCommandHandler(BaseRepository<VmsProcess> vmsProcessRepository,
			VmsProcessService VmsProcessService,
			BaseRepository<VmsProcessImg> vmsProcessImgRepository, IConfiguration config,
			IWebHostEnvironment env, ILogger<ScheduleCommandHandler> logger,
			ScheduleService scheduleService)
		{
			_vmsProcessRepository = vmsProcessRepository;
			_VmsProcessService = VmsProcessService;
			_vmsProcessImgRepository = vmsProcessImgRepository;
			_env = env;
			_config = config;
			_logger = logger;
			_scheduleService = scheduleService;
		}


		#region Lập lịch
		/// <summary>
		/// Author: TuanHA
		/// Description: Hàm bắt đầu xử lý lập lịch
		/// Created date: 05/03/2025
		/// </summary>
		/// <param name="input">Đối tượng AddVmsProcessInput</param>
		/// <returns></returns>

		public async Task HandleAsync(ScheduleProcessInput command)
		{
			switch (command.ActionType)
			{
				case "start":
					// Create variable to store list VmsProcess and list VmsProcessImg
					var listVmsProcessMemory = new List<VmsProcess>();
					var listVmsProcessImgMemory = new List<VmsProcessImg>();

					// Thiết lập tiến trình lập lịch
					await InitialSchedule(command, listVmsProcessMemory, listVmsProcessImgMemory);

					// Bắt đầu thực thi tiến trình lập lịch
					//await StartSchedule(listVmsProcessMemory, listVmsProcessImgMemory);
					await _scheduleService.StartSchedule(listVmsProcessMemory, listVmsProcessImgMemory); // đổi ở đây

					break;
				case "stop":
					// Dừng tiến trình lập lịch
					await StopSchedule(command);
					break;
			}
		}
		#endregion


		#region Thiết lập tiến trình lập lịch
		/// <summary>
		/// Author: TuanHA
		/// Description: Thiết lập tiến trình lập lịch
		/// Created date: 06/03/2024
		/// </summary>
		private async Task InitialSchedule(ScheduleProcessInput command, List<VmsProcess> listVmsProcessMemory, List<VmsProcessImg> listVmsProcessImgMemory)
		{
			// Declare variable to store list url image copy
			var listControlScheduleInputCopy = new List<ControlScheduleInput>();

			// 1. Kiểm tra thông tin thiết lập
			// 1.1. Kiểm tra thiết bị có được chọn hay không
			if (command.EquipmentIds.Count < 1)
			{
				// TODO: Thiết bị chưa được chọn
				throw Oops.Oh(BaseMsg.VmsProcess.Validation.NoDeviceSelected);
			}

			// 1.2. Kiểm tra thiết bị có tồn tại hay không để lấy thông tin IP/Port/Vendor
			// Lấy dữ liệu qua MessBus thay cho BaseApiClient
			equipmentList = await _VmsProcessService.ValidateEquipmentExist(command.EquipmentIds);

			// 1.3. Kiểm tra nội dung hình ảnh tồn tại hay ko			
			if (command.ScheduleList != null && command.ScheduleList.Count > 0)
			{
				// Lấy đường dẫn local host
				var pathRoot = FileServerUtil.GetPath(FileServerUtil.PathTypeEnum.WebRoot);

				// Loop trong danh sách url image gửi từ FE
				for (int i = 0; i < command.ScheduleList.Count; i++)
				{
					var scheduleProcess = command.ScheduleList[i];

					if (!string.IsNullOrEmpty(scheduleProcess.Url))
					{
						var filePath = pathRoot + scheduleProcess.Url;

						// Check exists with base url get image
						var isExistsImage = await FileServerUtil.UrlFileExistAsync(filePath);
						if (!isExistsImage)
						{
							// Thông báo hình ảnh không tồn tại
							throw Oops.Oh(BaseMsg.BaseException.NotFound, BaseMsg.VmsProcess.Entity.UrlImage);
						}

						// Xử lý copy ảnh ra thư mục lưu trữ
						// Lấy đường dẫn gốc từ file wwwroot
						var sourceFilePath = _env.WebRootPath + scheduleProcess.Url;

						// Tại sao phải copy? 
						// Vì lịch chạy lâu dài. Ảnh gốc có thể bị người dùng xoá hoặc ghi đè trong lúc lịch vẫn đang chạy
						// Tạo một bản sao bất biến đảm bảo lịch luôn có ảnh để hiển thị, không phụ thuộc vào việc ảnh gốc còn hay mất

						// Define the folder path to save the image copy
						var relativePathSaveVmsImageCopy = "/" + "Upload" + "/" + "VmsImageSendToDevice" + "/" + "ScheduleVms";
						var pathFolderSaveVmsImageCopy = Path.Combine(_env.WebRootPath, "Upload", "VmsImageSendToDevice", "ScheduleVms");

						// Ensure the directory exists, if not then create
						if (!Directory.Exists(pathFolderSaveVmsImageCopy))
						{
							Directory.CreateDirectory(pathFolderSaveVmsImageCopy);
						}

						// Define the destination file path to save image copy
						var extensionFileRoot = Path.GetExtension(sourceFilePath);
						var fileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + "ScheduleVms" + "_" + $"{i + 1}" + extensionFileRoot;
						var destinationFilePath = Path.Combine(pathFolderSaveVmsImageCopy, fileName);

						// Copy the file to the destination folder
						try
						{
							using (FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
							{
								using (FileStream destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
								{
									await sourceStream.CopyToAsync(destinationStream);
								}
							}
						}
						catch (Exception ex)
						{
							//TODO: Log
						}

						// Add relative path to list object image
						ControlScheduleInput scheduleProcessCopy = new ControlScheduleInput();
						scheduleProcessCopy.Url = Path.Combine(relativePathSaveVmsImageCopy, fileName);
						scheduleProcessCopy.TimeToLive = scheduleProcess.TimeToLive;

						// Add list copy to use for start schedule
						listControlScheduleInputCopy.Add(scheduleProcessCopy);

					}
				}
			}

			// Lấy tất cả tiến trình từ db có type schedule và bao gồm vmsId
			var listVmsProcessFromDb = await _vmsProcessRepository.AsQueryable().Where(msg => msg.Type == "schedule"
																					&& command.EquipmentIds.Contains(msg.VmsId)).ToListAsync();

			// 2. Dừng tất cả các tiến trình thread lập lịch đang chạy tương ứng với VmsIds
			List<string> listThreadId = listVmsProcessFromDb.Select(p => p.ThreadId).ToList();

			Parallel.ForEach(listThreadId, threadId =>
			{
                // Stop thread
                ScheduleService.StopThreadRunSchedule(threadId);
			});

			// 3. Xóa tất cả các process trong bảng VmsProcess liên quan đến Vms có tiến trình lập lịch (type = schedule)             
			var listIdProcess = listVmsProcessFromDb.Select(msg => msg.ID).ToList();
			 
			// Check not null listIdVmsProcess ||| TODO dùng sqlsugar xóa cả 2 bảng
			if (listIdProcess != null && listIdProcess.Count > 0)
			{
				// 3.1. Xóa trong bảng VmsProcessImg trước
				await _vmsProcessImgRepository.AsDeleteable().Where(msg => listIdProcess.Contains(msg.ProcessId)).ExecuteCommandAsync();

				// 3.2. Xóa trong bảng VMSProcess
				await _vmsProcessRepository.AsDeleteable().Where(msg => listIdProcess.Contains(msg.ID) && msg.Type == "schedule").ExecuteCommandAsync();
			}

			// 4. Thiết lập mới trong bảng VMSProcess và VMSProcessImg		
			// Bắt transaction đảm bảo dữ liệu được lưu đồng thời	
			try
			{
				await _vmsProcessRepository.Context.Ado.BeginTranAsync();
				var isAppendModeEnabled = App.GetConfig<bool?>("VmsConfig:EnableIncidentAppendMode") ?? false;
				foreach (var vmsId in command.EquipmentIds)
				{
					var equipmentFromDb = equipmentList.FirstOrDefault(e => e.ID == vmsId);
					
					// Kiểm tra nếu thiết bị đang có sự cố thì lưu ở trạng thái pause-incident, không start ngay
					var isUnderIncident = false;
					if (isAppendModeEnabled)
					{
						// Chỉ tính sự cố còn đang xử lý: dòng IncidentId null / sự cố đã kết thúc sẽ không có reset nào đánh thức lịch
						var activeControls = await _VmsProcessService.GetIncidentControlsByEquipmentAsync(vmsId, onlyActiveIncident: true);
                        isUnderIncident = activeControls != null && activeControls.Count > 0; // Nếu có tiến trình sự cố đang hoạt động thì thiết bị đang ở trạng thái sự cố
                    }

					// 3.1. Thiết lập trong bảng VMSProcess
					var vmsProcess = new VmsProcess
					{
						VmsId = vmsId,
						Type = VmsProcessTypeConst.Schedule,
						State = isUnderIncident ? VmsProcessStateConst.PauseIncident : VmsProcessStateConst.Run,
						Brightness = command.Brightness,
						ThreadId = $"{equipmentFromDb.ID}",
						Priority = command.Priority,
					};
					await _vmsProcessRepository.InsertAsync(vmsProcess);
					listVmsProcessMemory.Add(vmsProcess);


					// 3.2. Thiết lập trong bảng VMSProcessImg
					foreach (var scheduleProcessCopy in listControlScheduleInputCopy)
					{
						var vmsProcessImg = new VmsProcessImg
						{
							ProcessId = vmsProcess.ID,
							Url = scheduleProcessCopy.Url,
							TimeToLive = scheduleProcessCopy.TimeToLive
						};
						await _vmsProcessImgRepository.InsertAsync(vmsProcessImg);
						listVmsProcessImgMemory.Add(vmsProcessImg);
					}
				}
				await _vmsProcessRepository.Context.Ado.CommitTranAsync();
			}
			catch (Exception ex)
			{
				// Xóa danh sách của NGƯỜI GỌI (gán null chỉ đổi biến cục bộ): giữ lại thì StartSchedule vẫn chạy thread
				// cho các tiến trình vừa bị rollback, phát ảnh mãi mà không có bản ghi nào để dừng.
				listVmsProcessMemory.Clear();
				listVmsProcessImgMemory.Clear();

				// Rollback transaction
				await _vmsProcessRepository.Context.Ado.RollbackTranAsync();
				// TODO: log
			}
		}
		#endregion

		#region Thực thi tiến trình lập lịch
		///// <summary>
		///// Author: TuanHA
		///// Description: Bắt đầu thực thi tiến trình lập lịch
		///// Created date: 06/03/2024
		///// </summary>
		//private async Task StartSchedule(List<VmsProcess> listVmsProcessMemory, List<VmsProcessImg> listVmsProcessImgMemory)
		//{
		//	// 5. Bắt đầu chạy tiến trình lập lịch
		//	if (listVmsProcessMemory != null && listVmsProcessMemory.Count > 0)
		//	{
		//		// Get list equipment info
		//		var equipmentListInfos = await _VmsProcessService.GetListEquipmentInfo(listVmsProcessMemory);

		//		foreach (var vmsProcess in listVmsProcessMemory) // Danh sách các tiến trình lập lịch (mỗi tiến trình tương ứng với một biển)
		//              {
		//			try
		//			{
		//				var id = vmsProcess.ID;
		//				StartProcess[vmsProcess.ThreadId] = 1;

		//				// Create RunThreadInput for thread execute function RunThread
		//				RunThreadInput runThreadInput = new RunThreadInput();
		//				runThreadInput.ListVmsProcessImage = listVmsProcessImgMemory != null ? listVmsProcessImgMemory.Where(l => l.ProcessId == id).ToList() : new List<VmsProcessImg>(); //các ảnh của biển này (lọc theo ProcessId)
		//                      runThreadInput.ProcessID = id;
		//				runThreadInput.VmsID = vmsProcess.VmsId;
		//				runThreadInput.StartProcess = StartProcess[vmsProcess.ThreadId];
		//				runThreadInput.ListEquipmentInfos = equipmentListInfos.Adapt<List<EquipmentInfoInput>>();  //Thông tin IP/Port/Vendor
		//				runThreadInput.Brightness = vmsProcess.Brightness.Value;
		//				runThreadInput.Priority = vmsProcess.Priority;
		//				runThreadInput.ThreadId = vmsProcess.ThreadId;
		//				runThreadInput.ProcessType = "schedule";


		//                      // Chuẩn bị thread lập lịch
		//                      //if (!ThreadDictionary.ContainsKey(vmsProcess.ThreadId))
		//                      //{
		//                      //	// Add thread to dictionary
		//                      //	ThreadDictionary.Add(vmsProcess.ThreadId, new Thread(new ThreadStart(() => RunThread(runThreadInput)))
		//                      //	{
		//                      //		IsBackground = true,
		//                      //		Name = vmsProcess.ThreadId
		//                      //	});
		//                      //	ThreadDictionary[vmsProcess.ThreadId].Start();
		//                      //}


		//                      // Nếu biển đó đã có lập lịch rồi thì không tạo thêm cái thứ hai (sẽ gây xung đột, hai vòng cùng đẩy ảnh ra một biển)
		//                      if (!TaskDictionary.ContainsKey(vmsProcess.ThreadId) && !CancellationTokenSourceDictionary.ContainsKey(vmsProcess.ThreadId))
		//				{
		//                          // Tạo một CancellationTokenSource để quản lý việc hủy bỏ tiến trình lập lịch
		//                          CancellationTokenSourceDictionary.Add(vmsProcess.ThreadId, new CancellationTokenSource());

		//                          // Tạo một Task để chạy tiến trình lập lịch
		//                          TaskDictionary.Add(vmsProcess.ThreadId, new Task(() => RunThread(runThreadInput), CancellationTokenSourceDictionary[vmsProcess.ThreadId].Token));

		//					TaskDictionary[vmsProcess.ThreadId].Start();
		//				}

		//			}
		//			catch (Exception ex)
		//			{
		//				// TODO: log
		//			}
		//		}
		//	}
		//}
		#endregion

		#region Hàm chạy tiến trình lập lịch được thread thực thi
		///// <summary>
		///// Description: Hàm dùng thread để chạy tiến trình lập lịch gửi dữ liệu ra thiết bị
		/////Author: TuanHA
		/////Create Date: 07/03/2025
		///// </summary>
		//public async void RunThread(RunThreadInput runThreadInput)
		//{
		//	var detectWriteLog = true;
		//	var isWriteLog = true;

		//	while (!CancellationTokenSourceDictionary[runThreadInput.ThreadId].IsCancellationRequested) //while (chưa bị yêu cầu hủy) ← vòng lặp vô hạn
		//	{
		//              Debug.WriteLine("1. RunThread - " + runThreadInput.ThreadId);
		//		if (runThreadInput.ListVmsProcessImage.Any())  
		//              {
		//			try
		//			{
		//				foreach (var vmsProcessImg in runThreadInput.ListVmsProcessImage) // foreach từng ảnh trong danh sách ← xoay qua từng ảnh
		//                      {
		//					try
		//					{
		//						// Nếu có yêu cầu kết thúc thread
		//						if (CancellationTokenSourceDictionary[runThreadInput.ThreadId].IsCancellationRequested) 
		//							return; // nếu bị yêu cầu hủy → return  ← thoát ngay giữa chừng

		//                              var sw = new Stopwatch(); // Bắt đầu đo thời gian trước khi gửi dữ liệu ra thiết bị
		//                              sw.Start(); 
		//						RunningProcess[runThreadInput.ThreadId] = 1;

		//                              // Xử lý dữ liệu gửi ra thiết bị
		//                              PrepareVmsData prepareVmsData = new PrepareVmsData
		//						{
		//							ListEquipmentInfos = runThreadInput.ListEquipmentInfos.Adapt<List<EquipmentInfoInput>>()
		//						};

		//                              // Nếu có thông tin thiết bị → mới chuẩn bị dữ liệu gửi ra thiết bị, không thì thôi (tránh lỗi null)
		//                              if (prepareVmsData.ListEquipmentInfos?.Count > 0)
		//                              {
		//							// ContentInfoInput
		//							prepareVmsData.ContentInfoInput.Brightness = runThreadInput.Brightness;
		//							prepareVmsData.ContentInfoInput.ThreadId = runThreadInput.ThreadId;
		//							prepareVmsData.ContentInfoInput.Priority = runThreadInput.Priority;
		//							prepareVmsData.ContentInfoInput.Url = vmsProcessImg.Url;

		//							// ProcessInfoInput
		//							prepareVmsData.ProcessInfoInput.ProcessType = runThreadInput.ProcessType;
		//							prepareVmsData.ProcessInfoInput.ActionType = "run";
		//							prepareVmsData.ProcessInfoInput.TimeToLive = vmsProcessImg.TimeToLive.Value;
		//						}

		//						Debug.WriteLine($"2. RunThread - Key: {runThreadInput.ThreadId}, " +
		//										//	$"Value: {ThreadDictionary[runThreadInput.ThreadId].Name}" + " - " +
		//										$"Image: {vmsProcessImg.Url}");


		//						// Disable log
		//						if (!detectWriteLog)
		//						{
		//							isWriteLog = false;
		//						}

		//                              // Khi gửi dữ liệu ra thiết bị thì vẫn phải kiểm tra nếu có yêu cầu hủy bỏ thì sẽ không gửi nữa
		//                              if (!CancellationTokenSourceDictionary[runThreadInput.ThreadId].IsCancellationRequested)
		//						{								   
		//							_VmsProcessService.ChangeVmsContent(prepareVmsData, runThreadInput.VmsID, isWriteLog);
		//						}

		//                              // Kiểm tra nếu đã gửi xong ảnh cuối cùng trong danh sách rồi thì tắt log, tránh việc log liên tục khi chạy lại từ đầu danh sách ảnh
		//                              if (runThreadInput.ListVmsProcessImage.IndexOf(vmsProcessImg) == runThreadInput.ListVmsProcessImage.Count - 1)
		//						{
		//							detectWriteLog = false;
		//						}
		//						RunningProcess[runThreadInput.ThreadId] = 0;

		//						sw.Stop(); // Dừng đo thời gian sau khi đã gửi xong dữ liệu ra thiết bị
		//                              var timeElapsed = sw.ElapsedMilliseconds; // Lấy thời gian đã tốn để gửi dữ liệu ra thiết bị

		//                              // Tính toán thời gian cần ngủ để đảm bảo mỗi ảnh hiển thị đúng với TimeToLive đã thiết lập
		//                              var timeToSleep = Math.Max(5, (int)(vmsProcessImg.TimeToLive.Value * 1000 - timeElapsed)); // timeToSleep = TimeToLive × 1000 - thời gian đã tốn

		//                              // Task.Delay(timeToSleep).Wait(token)  ← NGỦ đúng số giây của ảnh
		//                              Task.Delay(TimeSpan.FromMilliseconds(timeToSleep)).Wait(CancellationTokenSourceDictionary[runThreadInput.ThreadId].Token);


		//					}
		//					catch (TaskCanceledException ex)
		//					{
		//						RunningProcess[runThreadInput.ThreadId] = 0;

		//						//if (ThreadDictionary[runThreadInput.VmsID].IsAlive)
		//						//{
		//						//	_vmsProcessRepository.AsUpdateable()
		//						//		.Where(l => runThreadInput.ProcessID == l.ID && l.State == "run" && l.Type == "schedule")
		//						//		.SetColumns(l => l.State, "stop")
		//						//		.ExecuteCommand();

		//						//	StartProcess[runThreadInput.VmsID] = 0;
		//						//	ThreadDictionary[runThreadInput.VmsID].Abort();
		//						//}


		//						if (TaskDictionary[runThreadInput.ThreadId].Status == TaskStatus.WaitingToRun || TaskDictionary[runThreadInput.ThreadId].Status == TaskStatus.Running)
		//						{
		//							_vmsProcessRepository.AsUpdateable()
		//								.Where(l => runThreadInput.ProcessID == l.ID && l.State == "run" && l.Type == "schedule")
		//								.SetColumns(l => l.State, "stop")
		//								.ExecuteCommand();

		//							StartProcess[runThreadInput.ThreadId] = 0;

		//							// Wait for the task to finish
		//							TaskDictionary[runThreadInput.ThreadId].Wait();

		//							// Then dispose
		//							TaskDictionary[runThreadInput.ThreadId].Dispose();
		//						}

		//						// TODO: log error
		//					}
		//					catch (Exception ex)
		//					{
		//						// TODO: log error
		//					}
		//					finally
		//					{

		//					}
		//				}
		//			}
		//			catch (Exception ex)
		//			{
		//				// TODO: log error

		//			}
		//		}
		//	}
		//}
		#endregion


		#region Hàm dừng thread tiến trình chạy lập lịch
		///// <summary>
		///// Description: Dừng thread tiến trình chạy lập lịch
		/////Author: TuanHA
		/////Create Date: 11/03/2025
		///// </summary>
		///// <param name="threadId"> threadId chính là equimentId </param>
		///// <returns></returns>

		//public static void StopThreadRunSchedule(string threadId)
		//{
		//	try
		//	{
		//		var sw = new Stopwatch();
		//		sw.Start();  // Bắt đầu đo thời gian trước khi dừng thread chạy lập lịch
		//              // Kiểm tra thread có tồn tại trong dictionary hay không
		//              if (!TaskDictionary.ContainsKey(threadId) || !CancellationTokenSourceDictionary.ContainsKey(threadId)) return;
		//		if (TaskDictionary[threadId] != null && CancellationTokenSourceDictionary[threadId] is not null) // Nếu thread đang chạy thì mới thực hiện dừng
		//              {

		//                  // Vì Cancel() chỉ "gửi tín hiệu" dừng, luồng chưa dừng ngay.
		//			// Nếu xoá khỏi dictionary trong khi luồng vẫn đang đọc dictionary đó (ở RunThread),
		//                  // sẽ gây lỗi. Phải chờ (Wait) cho luồng thoát hẳn rồi mới dọn dẹp — đảm bảo trình tự an toàn.
		//                  CancellationTokenSourceDictionary[threadId].Cancel();

		//			if (CancellationTokenSourceDictionary[threadId].IsCancellationRequested)
		//			{
		//				// Wait for the task to finish
		//				TaskDictionary[threadId].Wait();

		//				// Wait for data send to device complete
		//				//if (RunningProcess.ContainsKey(threadId) && RunningProcess[threadId] == 1)
		//				//{
		//				//	Debug.WriteLine("StopThreadRunSchedule - Wait for data send to device complete: " + RunningProcess[threadId]);
		//				//	Thread.Sleep(18);
		//				//}

		//				// Then dispose
		//				TaskDictionary[threadId].Dispose();

		//				// Dispose cancellation
		//				CancellationTokenSourceDictionary[threadId].Dispose();

		//				// Remove thread from dictionary
		//				TaskDictionary.Remove(threadId);
		//				CancellationTokenSourceDictionary.Remove(threadId);

		//                      Debug.WriteLine("StopThreadRunSchedule - Cancel token with thread id: " + threadId);
		//				sw.Stop(); // Dừng đo thời gian sau khi đã dừng thread chạy lập lịch
		//                      Debug.WriteLine("StopThreadRunSchedule - Time: " + sw.ElapsedMilliseconds);
		//			}
		//		}

		//	}
		//	catch (Exception ex)
		//	{
		//		// TODO: log
		//		throw ex;
		//	}

		//}

		#endregion

		#region Hàm remove key khỏi task dictionary chạy lập lịch
		///// <summary>
		///// Description: Xóa key trong dictionary Task
		/////Author: TuanHA
		/////Create Date: 11/03/2025
		///// </summary>
		///// <param name="threadId"> threadId chính là equimentId </param>
		///// <returns></returns>
		//private static void RemoveFromDictionary(string threadId)
		//{
		//	try
		//	{
		//		if (TaskDictionary.ContainsKey(threadId) && CancellationTokenSourceDictionary.ContainsKey(threadId))
		//		{
		//			// Remove thread from dictionary
		//			TaskDictionary.Remove(threadId);
		//			CancellationTokenSourceDictionary.Remove(threadId);
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		// TODO: log
		//		throw ex;
		//	}

		//}
		#endregion

		#region Hàm chức năng dừng chạy lập lịch
		/// <summary>
		/// Description: Hàm dừng lập lịch
		///Author: TuanHA
		///Create Date: 12/03/2025
		/// </summary>
		/// <returns></returns>
		private async Task StopSchedule(ScheduleProcessInput command)
		{
			var sw = new Stopwatch();
			sw.Start(); // Bắt đầu đo thời gian trước khi dừng các tiến trình lập lịch trong database

			// 1. Dừng tất cả các tiến trình thread lập lịch đang chạy tương ứng với VmsIds
			// Gồm cả lịch đang pause-incident: người dùng đã dừng thì khi sự cố kết thúc ResumeScheduleByVmsIds không được chạy lại.
			await _vmsProcessRepository.AsUpdateable().Where(msg => msg.Type == "schedule"
																 && command.EquipmentIds.Contains(msg.VmsId)
																 && (msg.State == "run" || msg.State == VmsProcessStateConst.PauseIncident))
									   .SetColumns(msg => msg.State, "stop")
									   .ExecuteCommandAsync();

			sw.Stop();  // Dừng đo thời gian sau khi đã cập nhật trạng thái dừng cho các tiến trình lập lịch trong database
			Debug.WriteLine("1. StopSchedule - Time: " + sw.ElapsedMilliseconds);
			sw.Start(); // Bắt đầu đo thời gian trước khi dừng các thread đang chạy

			// 2. Dừng tất cả các thread đang chạy
			Parallel.ForEach(command.EquipmentIds, vmsId =>
			{
				// Stop thread - vmsId chính là threadId
				ScheduleService.StopThreadRunSchedule(vmsId);
			});

			sw.Stop(); // Dừng đo thời gian sau khi đã dừng các thread đang chạy
			Debug.WriteLine("2. StopSchedule - Time: " + sw.ElapsedMilliseconds);
			sw.Start(); // Bắt đầu đo thời gian trước khi ghi log và xóa khỏi dictionary

            // 3. Ghi log và xóa khỏi dictionary --- ChuongLP -- 14/07/2026: Sửa lại dùng ForEachAsync để chạy song song, tránh bị treo khi có nhiều biển
            await Parallel.ForEachAsync(command.EquipmentIds, async (vmsId, ct) =>
            {
                await _VmsProcessService.WriteStopScheduleLog(vmsId, command.TimeToLive); 
            });

            #region Code xử lý cũ tạm đóng để giành cho code mới - ChuongLP -- 14/07/2026 tạm đóng
            //         await Parallel.ForEachAsync(command.EquipmentIds, async (vmsId, ct) =>   // [SỬA] ForEach -> ForEachAsync + await
            //         {
            //             // Xóa khỏi dictionary Task
            //             // vmsId chính là threadId
            //             //RemoveFromDictionary(vmsId);

            //             // Ghi log dừng lập lịch
            //             // Get vms current from db
            //             // Call Api get vms current by quipmentId
            //             var vmsCurrentFromDb = await _VmsProcessService.GetVmsCurrentByEquipment(vmsId);

            //	// Lưu dữ liệu cấu hình cũ - Code cũ tạm đóng
            //	//ContentInfoInput contentInfoInput = new ContentInfoInput();
            //	//contentInfoInput = vmsCurrentFromDb.Adapt<ContentInfoInput>();
            //	//string oldData = JsonConvert.SerializeObject(contentInfoInput);

            //	// ChuongLP -- 14/07/2026
            //             ContentInfoInput contentInfoInput = vmsCurrentFromDb.Adapt<ContentInfoInput>();
            //             string oldData = JsonConvert.SerializeObject(contentInfoInput);

            //             TmsSignalLogAddRequest tmsSignalLogAddRequest = new TmsSignalLogAddRequest();
            //	tmsSignalLogAddRequest.EquipmentID = vmsId;
            //	tmsSignalLogAddRequest.OldUrl = vmsCurrentFromDb.Url;
            //	tmsSignalLogAddRequest.NewUrl = vmsCurrentFromDb.Url;
            //	tmsSignalLogAddRequest.OldData = oldData;
            //	tmsSignalLogAddRequest.NewData = oldData;
            //	tmsSignalLogAddRequest.Priority = vmsCurrentFromDb.Priority;
            //	tmsSignalLogAddRequest.Type = "schedule";
            //	tmsSignalLogAddRequest.ExecuteBy = "schedule";
            //	tmsSignalLogAddRequest.TimeToLive = command.TimeToLive;
            //	tmsSignalLogAddRequest.ExecuteTime = DateTime.Now;
            //	tmsSignalLogAddRequest.State = "success";
            //	tmsSignalLogAddRequest.NewEndDate = DateTime.Now;
            //	tmsSignalLogAddRequest.Remark = "stop";


            //             // TODO: Format and optimize call api
            //             #region Call api post lưu log
            //             //var respone = await BaseApiClient<ISignalLogApi>.Client(_url).Add(tmsSignalLogAddRequest);
            //             await  _VmsProcessService.InsertIntoTmsSignaLog(tmsSignalLogAddRequest);
            //	#endregion
            //});
            //sw.Stop();
            //Debug.WriteLine("3. StopSchedule - Time: " + sw.ElapsedMilliseconds);
            #endregion
        }
        #endregion


        #region Hàm khởi động lại tiến trình chạy lập lịch
        /// <summary>
        /// Description: Handler xử lý khởi động lại tiến trình chạy lập lịch
        ///Author: TuanHA
        ///Create Date: 19/03/2025
        /// </summary>
        /// <param name="listProcessSchedule"> Danh sách các tiến trình lập lịch đang chạy </param>
        /// <returns></returns>
        public async Task HandleAsync(RestartProcessScheduleInput input)
		{
			try
			{
				await _scheduleService.StartSchedule(input.ListProcess, input.ListProcessImg);
			}
			catch (Exception ex)
			{
				// Retry again
				try
				{
					await _scheduleService.StartSchedule(input.ListProcess, input.ListProcessImg);
				}
				catch (Exception)
				{

					//TODO: log
					throw;
				}
			}
		}
		#endregion
	}
}