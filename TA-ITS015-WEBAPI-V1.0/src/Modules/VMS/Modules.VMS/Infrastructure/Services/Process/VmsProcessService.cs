using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.Equipment;
using Modules.TMS.Core.Dto.IncidentControl;
using Modules.TMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Controllers.ProcessImg.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure.Services.Schedule;
using Shared.DTO.Constants.Module;
using Shared.DTO.Dtos.Tms;
using Shared.DTO.ResponseWrapper;
using Shared.Utility.Endpoints.Tms;
using Shared.Utility.Endpoints.Vms;
using Shared.Utility.Utilities;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Modules.TMS.Core.Helper;

namespace Modules.VMS.Infrastructure.Services.Process
{
	public class VmsProcessService:IScoped
	{
		//private readonly IWebHostEnvironment _hostEnvironment;
		//private readonly IUploadService _iUploadService;
		//private readonly IRecurringJobManager _jobManager;
		//private BaseRepository<VmsCurrent> _vmsCurrent;
		private BaseRepository<VmsProcess> _vmsProcess;
		private BaseRepository<VmsProcessImg> _vmsProcessImg;
		private readonly IConfiguration _config;
		private readonly IMessageBus _messageBus;
		private readonly IServiceProvider _serviceProvider;
		private readonly string _url;

        private static readonly SemaphoreSlim _dbLock = new(1, 1);

        #region Khai báo biến lập lịch
        private static readonly Dictionary<string, string> RunningProcess = new Dictionary<string, string>();
		//private static readonly object lastImageLock = new object(); // Lock object for lastDisplayedImageUrls
		//private static readonly Dictionary<string, string> lastDisplayedImageUrls = new Dictionary<string, string>(); // To store last displayed image URLs
		//private static readonly Dictionary<string, string> lastDisplayedData = new Dictionary<string, string>(); // To store last displayed image URLs
		#endregion

		public VmsProcessService(BaseRepository<VmsProcess> VmsProcess, BaseRepository<VmsProcessImg> VmsProcessImg, 
            IConfiguration config ,IMessageBus messageBus, IServiceProvider serviceProvider)
		{
			_vmsProcess = VmsProcess;
			_vmsProcessImg = VmsProcessImg;
			_config = config;
			_messageBus = messageBus;
			_serviceProvider = serviceProvider;
			_url = _config.GetValue<string>("ApiGateway:BaseUrl");
        }

		/// <summary>
		/// Author: KhoaND
		/// Description: Hàm lưu file hình xuống thư mục upload
		/// Created date: 12/09/2024
		/// </summary>
		/// <param name="base64Image">Chuỗi dữ liệu truyền vào theo định dạng base 64</param>
		/// <returns></returns>
		public async Task<List<VmsProcessImg>> ProcessImageAsync(AddVmsProcessImgInput process, string processId)
		{
			var processImgList = new List<VmsProcessImg>();

			foreach (var item in process.ListUrl)
			{
				// Create a new instance for each image
				var processImg = new VmsProcessImg
				{
					ProcessId = processId,  // Set the associated Process ID
					Url = item.ImageUrl,    // Use the image URL
					TimeToLive = item.TimeToLive // Set the TTL for the image
				};

				processImgList.Add(processImg);
			}
			// Return the list of images associated with this process
			return processImgList;
		}

        /// <summary>
        /// Lấy dữ liệu qua MessBus (Wolverine) thay cho BaseApiClient.
        /// Hàm dùng chung cho các handler điều khiển VMS/LCS (Image/Text/Template/Timer/Schedule/LcsImage).
        /// Author: ChuongLP
        /// Created date: 05/08/2026
        /// </summary>
        /// <param name="equipmentIds">Danh sách ID thiết bị cần kiểm tra</param>
        /// <returns>Danh sách thiết bị hợp lệ lấy được (dùng lại cho các bước sau nếu cần)</returns>
        public async Task<List<TmsEquipment>> ValidateEquipmentExist(List<string> equipmentIds)
		{
			// Lấy danh sách thiết bị theo ID qua MessBus (không còn gọi BaseApiClient)
			var equipmentList = await _messageBus.InvokeAsync<List<TmsEquipment>>(
									new GetEquipmentByIdsInput { EquipmentIds = equipmentIds })
								?? new List<TmsEquipment>();

			// Xác định các ID không tồn tại
			var invalidIds = equipmentIds.Except(equipmentList.Select(e => e.ID)).ToList();
			if (invalidIds.Any())
			{
				// Thông báo thiết bị không tồn tại
				throw Oops.Oh(BaseMsg.BaseException.NotExist, BaseMsg.VmsProcess.Entity.VmsId);
			}

			return equipmentList;
		}

		#region Hẹn Giờ - TuanHA
		/// <summary>
		/// Lấy danh sách thông tin thiết bị qua MessBus (Wolverine) thay vì HTTP tự gọi.
		/// Trước: dùng HttpClient(_url + TmsEndpoint.Equipment.List) → timeout khi BE vừa restart
		///        vì tự gọi chính mình qua HTTP, DB/middleware chưa warm up hết.
		/// Sau:   dùng _messageBus.InvokeAsync → gọi trực tiếp handler trong cùng process, không qua network.
		/// Author: TuanHA — Created date: 21/02/2025 · ChuongLP — refactor khỏi HTTP
		/// </summary>
		public async Task<List<EquipmentInfoInput>> GetListEquipmentInfo(List<VmsProcess>? listProcess = null) 
		{
			// Lấy danh sách thiết bị qua MessBus (không qua HTTP, không có timeout mạng)
			List<TmsEquipment> listTmsEquipment;
			if (listProcess == null)
			{
				// Lấy toàn bộ thiết bị enable (dùng cho LCS, ResetIncident...)
				listTmsEquipment = await _messageBus.InvokeAsync<List<TmsEquipment>>(new GetAllEquipmentInput())
								   ?? new List<TmsEquipment>();
			}
			else
			{
				// Chỉ lấy thiết bị cần thiết theo danh sách process (dùng cho schedule/timer restart)
				var vmsIds = listProcess.Select(p => p.VmsId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
				listTmsEquipment = vmsIds.Count > 0
					? (await _messageBus.InvokeAsync<List<TmsEquipment>>(new GetEquipmentByIdsInput { EquipmentIds = vmsIds }) ?? new List<TmsEquipment>())
					: new List<TmsEquipment>();
			}

			var listEquipmentInfo = new List<EquipmentInfoInput>();
			if (listProcess == null)
			{
				// Lấy tất cả: map trực tiếp từ entity
				listEquipmentInfo = listTmsEquipment.Select(e => new EquipmentInfoInput
				{
					EquipmentId    = e.ID           ?? "",
					EquipmentName  = e.Name         ?? "",
					Ip             = e.Ip           ?? "",
					Username       = e.Username     ?? "",
					Password       = e.Password     ?? "",
					EquipmentCode  = e.Code         ?? "",
					Config         = e.Config       ?? "",
					Vendor         = e.Vendor       ?? "",
					Port           = e.Port         ?? "",
				}).ToList();
			}
			else
			{
				// Lấy theo process: Priority lấy từ process (không phải từ equipment)
				foreach (var item in listProcess)
				{
					var equipment = listTmsEquipment.FirstOrDefault(e => e.ID == item.VmsId);
					if (equipment == null) continue;

					listEquipmentInfo.Add(new EquipmentInfoInput
					{
						EquipmentId    = equipment.ID       ?? "",
						EquipmentName  = equipment.Name     ?? "",
						Ip             = equipment.Ip       ?? "",
						Username       = equipment.Username ?? "",
						Password       = equipment.Password ?? "",
						EquipmentCode  = equipment.Code     ?? "",
						Priority       = item.Priority      ?? 0,
						Config         = equipment.Config   ?? "",
						Vendor         = equipment.Vendor   ?? "",
						Port           = equipment.Port     ?? "",
					});
				}
			}
			return listEquipmentInfo;
		}

		public async Task<HashSet<string>> GetEquipmentIdIncident(List<string> equimentId) 
		{
			if (equimentId == null || equimentId.Count == 0)
				return new HashSet<string>();

            // Sử dụng message bus để lấy danh sách incident control theo danh sách equipmentId
            var control = await _messageBus.InvokeAsync<List<TmsIncidentControl>>(new GetIncidentControlByEquipmentInput { EquipmentIds = equimentId });

            // Trả về danh sách equipmentId có incident control (distinct)
            return control?.Select(c => c.EquipmentId).Where(id => !string.IsNullOrEmpty(id)).ToHashSet() ?? new HashSet<string>();
        } 

		/// <summary>
		/// Author: TuanHA
		/// Description: Hàm chuẩn bị dữ liệu để gửi ra hàm xử lý thiết bị
		/// Created date: 19/02/2025
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public DataSendToDevice PrepareContentSendToDevice(PrepareVmsData prepareVmsData, string equipmentId = null)
		{
			try
			{
				DataSendToDevice dataSendToDevice = new DataSendToDevice();

				// Check action type for prepare data
				var processType = prepareVmsData.ProcessInfoInput.ProcessType;
				var actionVms = prepareVmsData.ProcessInfoInput.ActionType;

				// Get brightness
				dataSendToDevice.Brightness = prepareVmsData.ContentInfoInput.Brightness;

				// Lấy thông tin thiết bị
				var equipmentInfo = prepareVmsData.ListEquipmentInfos.FirstOrDefault(e => e.EquipmentId == equipmentId);
				dataSendToDevice.EquipmentInfoInput = equipmentInfo;

				if (equipmentInfo != null)
				{
					dataSendToDevice.Url = prepareVmsData.ContentInfoInput.Url;
				}

				switch (processType)
				{
					case "timer":
						
						break;
					case "schedule":
					
						break;
					default:
						// TODO: Xử lý trường hợp khác
						// Trường hợp có hành động thủ công chen ngang	
						// Tạm thời để ở default				
						//UpdateStateStopInProcesssTimer();
						break;
				}
				Console.WriteLine("3. RunThread - PrepareContentSendToDevice", dataSendToDevice);
				return dataSendToDevice;
			}
			catch (Exception ex)
			{
				// TODO: log
				return null;
			}
			
		}

		/// <summary>
		/// Author: TuanHA
		/// Description: Lấy các tiến trình đang chạy có trạng thái type = "timer" và state = "run"
		/// Created date: 19/02/2025
		/// </summary>
		/// <returns></returns>
		private void UpdateStateStopInProcesssTimer()
		{
			_vmsProcess.AsUpdateable().Where(a => a.State == "run" && a.Type == "timer")
						.SetColumns(s => s.State, "stop")
						.ExecuteCommand();
		}	

		/// <summary>
		/// Description: xử lý khi thực hiện THÀNH CÔNG event gửi ra thiết bị 
		///Author: TuanHA
		///Create Date:
		/// </summary>
		public async Task UpdateVmsCurrentSuccess(object sender, EventInfoInput e, PrepareVmsData prepareVmsData, bool isWriteLog = false)
		{			
			Debug.WriteLine($"UpdateVmsCurrentSuccess - {prepareVmsData.ContentInfoInput.ThreadId} - urlImage: {prepareVmsData.ContentInfoInput.Url}");
			await UpdateVmsCurrent_WriteLog_SendSocket(sender, e, prepareVmsData, isWriteLog: isWriteLog);			
		}

		/// <summary>
		/// Description: xử lý khi thực hiện THẤT BẠI event gửi ra thiết bị 
		///Author: TuanHA
		///Create Date:
		/// </summary>
		public async Task UpdateVmsCurrentError(object sender, EventInfoInput e, PrepareVmsData prepareVmsData, bool isWriteLog = false)
		{
			// Dùng tạm hình ảnh để test
			var urlFail = @"\Upload\FailSendToDevice.bmp";
			await UpdateVmsCurrent_WriteLog_SendSocket(sender, e, prepareVmsData, urlFail, isWriteLog);
		}


		/// <summary>
		/// Description: Hàm xử lý UpdateVmsCurrent, WriteLog, SendSocket khi thực hiện được hàm trigger event gửi ra thiết bị gọi
		///Author: TuanHA
		///Create Date:
		/// </summary>s
		private async Task UpdateVmsCurrent_WriteLog_SendSocket(object sender, EventInfoInput e, PrepareVmsData prepareVmsData,
														string urlImageFail = null, bool isWriteLog = false)
		{
            await _dbLock.WaitAsync();
            try
			{
				var oldUrl = string.Empty;
				var oldData = string.Empty;
				var newData = string.Empty;

				var equipmentId = e.EquipmentId;
				var equipmentInfo = prepareVmsData.ListEquipmentInfos.FirstOrDefault(e => e.EquipmentId == equipmentId);				
				var url = prepareVmsData.ContentInfoInput.Url;

				// Gửi thất bại (kể cả timeout): log + socket vẫn dùng ảnh fail như trước (FE áp gói socket thẳng lên bản đồ,
				// ảnh fail là chỉ báo lỗi người trực nhìn thấy), nhưng KHÔNG ghi ảnh fail vào VmsCurrent: chưa biết bảng
				// đang hiện gì, và các luồng sự cố đọc VmsCurrent.Url (SeedOverrideImageAsync, RestoreRemainingIncidentContentAsync).
				var isFail = !string.IsNullOrEmpty(urlImageFail);
				if (isFail)
				{
					url = @"\Vms\Template\fail.png";
				}

				// Create object vms current to insert or update
				var vmsCurrentDataToUpdate = new UpdateVmsCurrentInputByProcess();
				vmsCurrentDataToUpdate = prepareVmsData.ContentInfoInput.Adapt<UpdateVmsCurrentInputByProcess>();
				vmsCurrentDataToUpdate.Url = url;
				if(equipmentInfo != null)
				{
					vmsCurrentDataToUpdate.EquipmentId = equipmentInfo.EquipmentId;
					vmsCurrentDataToUpdate.Name = equipmentInfo.EquipmentName;
					vmsCurrentDataToUpdate.Code = equipmentInfo.EquipmentCode;
                   
                }
				vmsCurrentDataToUpdate.ExecutedDate = DateTime.Now;
                vmsCurrentDataToUpdate.RowData = JsonConvert.SerializeObject(new
                {
                    Row1 = prepareVmsData.ContentInfoInput.Content1, Row2 = prepareVmsData.ContentInfoInput.Content2
                });
                
				// TODO : Xem xét có cần để không
				//vmsCurrentDataToUpdate.Url = prepareVmsData.ContentInfoInput.Url; // Nếu gửi thất bại thì lưu lại url hình fail

                //var b = vmsCurrentDataToUpdate;
                // 1. Update VmsCurrent
                // Check VmsCurrent ExistOrNot
                //var vmsCurrentFromDb = _vmsCurrent.AsQueryable().Where(v => v.EquipmentId == equipmentId).First();

                // Call Api get vms current by quipmentId
                var vmsCurrentFromDb = await GetVmsCurrentByEquipment(equipmentId);

				// Nếu tồn tại VmsCurrent
				if (vmsCurrentFromDb != null && !string.IsNullOrEmpty(vmsCurrentFromDb.ID))
				{
					vmsCurrentDataToUpdate.ID = vmsCurrentFromDb.ID;

					// Lưu url cũ
					oldUrl = vmsCurrentFromDb.Url;

					// Lưu dữ liệu cấu hình cũ
					ContentInfoInput contentInfoInput = new ContentInfoInput();
					contentInfoInput = vmsCurrentFromDb.Adapt<ContentInfoInput>();
					oldData = JsonConvert.SerializeObject(contentInfoInput);
				}

				// Call api update vmsCurrent
				if (!isFail)
				{
					await UpdateVmsCurrentByProcess(vmsCurrentDataToUpdate);
				}
			


				// 2. Ghi log db vào bảng tmsSignalLog
				if (isWriteLog)
				{

					Debug.WriteLine("Write log - " + $"{prepareVmsData.ContentInfoInput.ThreadId} " + $"- urlImage: {prepareVmsData.ContentInfoInput.Url}");
					Debug.WriteLine("Write log - " + $" equipmentId: {equipmentId} ");

					TmsSignalLogAddRequest tmsSignalLogAddRequest = new TmsSignalLogAddRequest();
					tmsSignalLogAddRequest.EquipmentID = equipmentId;
					tmsSignalLogAddRequest.OldUrl = oldUrl;
					tmsSignalLogAddRequest.NewUrl = url;
					tmsSignalLogAddRequest.OldData = oldData;
					tmsSignalLogAddRequest.Priority = prepareVmsData.ContentInfoInput.Priority;
					tmsSignalLogAddRequest.Type = prepareVmsData.ProcessInfoInput.ProcessType;			
					tmsSignalLogAddRequest.ExecuteBy = prepareVmsData.ProcessInfoInput.ExecuteBy ??  prepareVmsData.ProcessInfoInput.ProcessType;
					tmsSignalLogAddRequest.TimeToLive = prepareVmsData.ProcessInfoInput.TimeToLive;
					tmsSignalLogAddRequest.ExecuteTime = DateTime.Now;
					tmsSignalLogAddRequest.Remark = prepareVmsData.ProcessInfoInput.ActionType;
                    tmsSignalLogAddRequest.IncidentId = prepareVmsData.ContentInfoInput.IncidentId;
                    tmsSignalLogAddRequest.State = "success";

					// Nếu là lập lịch thì lưu NewEndDate
					if(prepareVmsData.ProcessInfoInput.ProcessType == "schedule")
					{
						tmsSignalLogAddRequest.NewEndDate = DateTime.Now;
					}

					// Nếu có hình ảnh fail => trạng thái thất bại
					if (!string.IsNullOrEmpty(urlImageFail))
					{
						tmsSignalLogAddRequest.State = "fail";
					}

					// NewData là Luu noi dung gồm data, cau hinh vms font, color vv..
					tmsSignalLogAddRequest.NewData = JsonConvert.SerializeObject(prepareVmsData.ContentInfoInput);

					// TODO: Format and optimize call api
					#region Call api post lưu log
					//var respone = await BaseApiClient<ISignalLogApi>.Client(_url).Add(tmsSignalLogAddRequest);
					await InsertIntoTmsSignaLog(tmsSignalLogAddRequest);
                    #endregion

                    // Ghi nhận sự kiện điều khiển thiết bị (song song TmsSignalLog)
                    // Chỉ ghi khi lệnh thuộc một sự cố; lệnh thủ công/lịch không có IncidentId sẽ tạo bản ghi rác chiếm biển.
                    // Vòng playlist sự cố (schedule-incident) không ghi: quyền giữ đã do AppendImageToIncidentProcess ghi đúng
                    // State/Priority của từng sự cố. Ghi lại ở đây sẽ đè Priority bằng của tiến trình, đổi State về override
                    // và có thể tạo lại bản ghi của sự cố vừa kết thúc nếu ảnh của nó đang được gửi dở.
                    if (!string.IsNullOrEmpty(prepareVmsData.ContentInfoInput.IncidentId)
                        && prepareVmsData.ProcessInfoInput?.ProcessType != VmsProcessTypeConst.ScheduleIncident)
                    try
                    {
                        await _messageBus.InvokeAsync(new UpdateTmsIncidentControlInput
                        {
                            EquipmentId = equipmentId,
                            IncidentId = prepareVmsData.ContentInfoInput.IncidentId,
							Priority = prepareVmsData.ContentInfoInput.Priority,
                            // LCS nối tiếp gửi thẳng nhưng vẫn giữ quyền các sự cố khác -> State = append; còn lại override như cũ
                            State = prepareVmsData.ContentInfoInput.PlayMode == VmsPlayModeConst.Append ? VmsPlayModeConst.Append : null,
                            Url = prepareVmsData.ContentInfoInput.Url   // nội dung dự định (url có thể là ảnh fail)
                        });
                    }
                    catch (Exception)
                    {
                    }

                }


                //// 3. Gửi socket
                //ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.VmsUpdateImage, new { ActionType = "vms-success", Data = vmsCurrentDataToUpdate });
                var actionType = string.IsNullOrEmpty(urlImageFail) ? "vms-success" : "vms-fail";   // [THÊM]

                ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.VmsUpdateImage, new { ActionType = actionType, Data = vmsCurrentDataToUpdate });

                // 4. Luồng RESET: chỉ ghi log Reset + xóa IncidentControl khi gửi THÀNH CÔNG
                if (prepareVmsData.ProcessInfoInput.ProcessType == "reset")
                {
                    if (string.IsNullOrEmpty(urlImageFail)) // success (error thì urlImageFail có giá trị)
                    {
                        // Ghi log Reset (dùng InsertIntoTmsSignaLog - không cần token, đồng bộ format chung)
                        await InsertIntoTmsSignaLog(new TmsSignalLogAddRequest
                        {
                            EquipmentID = equipmentId,
                            NewUrl = url,
                            OldUrl = oldUrl,
                            Type = "Reset Incident",
                            State = "success",
                            ExecuteBy = "Reset",
                            ExecuteTime = DateTime.Now,
                            IncidentId = prepareVmsData.ContentInfoInput.IncidentId,
                        });
                    }
                    // error + reset: KHÔNG xóa IncidentControl (giữ để lần sau thử lại)
                }
            }
			finally 
			{ 
				_dbLock.Release();
			}
		}

		/// <summary>
		/// Author: TuanHA
		/// Description: Lấy url image trong bảng vmsProcessImg
		/// Created date: 19/02/2025
		/// </summary>
		/// <returns></returns>
		public async Task<string> GetUrlImageInProcessImage(List<string> listProcessIds)
		{
			var  urlImage = await _vmsProcessImg.AsQueryable().Where(msg => listProcessIds.Contains(msg.ProcessId))
																			.Select(msg => msg.Url)
																			.FirstAsync() ?? string.Empty;
			return urlImage;
		}

		/// <summary>
		/// Author: TuanHA
		/// Description: Call api get VmsCurrent by equimentId
		/// Created date: 10/03/2025
		/// </summary>
		/// <returns></returns>
		public async Task<VmsCurrentOutput> GetVmsCurrentByEquipment(string equipmentId) 
		{
			var vmsCurrent = await _messageBus.InvokeAsync<VmsCurrentOutput>(new VmsCurrentByEquipment { EquipmentId = equipmentId });
			return vmsCurrent;
		}

		/// <summary>
		/// Author: TuanHA
		/// Description: Call api update VmsCurrent by run process
		/// Created date: 10/03/2025
		/// </summary>
		/// <returns></returns>
		public async Task UpdateVmsCurrentByProcess(UpdateVmsCurrentInputByProcess updateVmsCurrentInputByProcess)
		{
			//// Call api update vmsCurrent
			//using (var httpClient = new HttpClient())
			//{
			//	// Set timeout 2s
			//	httpClient.Timeout = TimeSpan.FromMilliseconds(2000);

			//	// Create content to send api
			//	using StringContent jsonContent = new(JsonConvert.SerializeObject(updateVmsCurrentInputByProcess),
			//										Encoding.UTF8,
			//										"application/json");

			//	// Send Http Post api
			//	using HttpResponseMessage objResponse = await httpClient.PostAsync(
			//										_url + VmsEndpoint.VmsCurrent.UpdateByProcess,
			//										jsonContent);

			//	// Trường hợp ko thành công
			//	if (!objResponse.IsSuccessStatusCode)
			//	{
			//		// TODO: Log
			//		var jsonResponse = await objResponse.Content.ReadAsStringAsync();
			//		Console.WriteLine(jsonResponse);
			//	}
			//}
			//call qua messbus 2026-02-26 SONTH
            await _messageBus.InvokeAsync<VmsCurrent>(updateVmsCurrentInputByProcess);


        }

		/// <summary>
		/// Author: TuanHA
		/// Description: Ghi log điều khiển vào bảng tmsSignalLog
		/// Created date: 10/03/2025
		/// Updated: ChuongLP - 23/10/2026 - Gọi IN-PROCESS qua MessBus (handler bên TMS) thay cho HttpClient tự gọi
		///          ApiGateway:BaseUrl: server cấu hình https theo IP nên lỗi SSL RemoteCertificateNameMismatch,
		///          lỗi bung ngược lên API điều khiển dù lệnh đã gửi xuống thiết bị.
		/// </summary>
		/// <returns></returns>
		public async Task InsertIntoTmsSignaLog(TmsSignalLogAddRequest tmsSignalLogAddRequest)
		{
			// Ghi log là bước phụ sau khi đã gửi thiết bị: lỗi ở đây KHÔNG được làm hỏng lệnh điều khiển
			// (nếu ném ra sẽ bỏ qua luôn bước ghi TmsIncidentControl + bắn socket cập nhật FE phía sau).
			try
			{
				await _messageBus.InvokeAsync(tmsSignalLogAddRequest);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[InsertIntoTmsSignaLog] Ghi TmsSignalLog lỗi · EquipmentID={tmsSignalLogAddRequest?.EquipmentID}: {ex.Message}");
			}
		}
        #endregion

        /// <summary>
        /// Ghi log dừng lập lịch cho 1 thiết bị. DB access được xếp hàng qua _dbLock
        /// để an toàn khi được gọi song song từ Parallel.ForEachAsync.
		/// Author: ChuongLP
		/// Date: 2026-07-14
        /// </summary>
        public async Task WriteStopScheduleLog(string equipmentId, int timeToLive)
        {
            await _dbLock.WaitAsync();                                          // xếp hàng qua quầy, giống luồng start
            try
            {
                var vmsCurrentFromDb = await GetVmsCurrentByEquipment(equipmentId);
                if (vmsCurrentFromDb == null) return;                           // [THÊM] biển chưa có current thì bỏ qua
                                                                                // (code cũ sẽ NullReference ở .Url)
                var contentInfoInput = vmsCurrentFromDb.Adapt<ContentInfoInput>();
                var oldData = JsonConvert.SerializeObject(contentInfoInput);

                var request = new TmsSignalLogAddRequest
                {
                    EquipmentID = equipmentId,
                    OldUrl = vmsCurrentFromDb.Url,
                    NewUrl = vmsCurrentFromDb.Url,
                    OldData = oldData,
                    NewData = oldData,
                    Priority = vmsCurrentFromDb.Priority,
                    Type = "schedule",
                    ExecuteBy = "schedule",
                    TimeToLive = timeToLive,
                    ExecuteTime = DateTime.Now,
                    State = "success",
                    NewEndDate = DateTime.Now,
                    Remark = "stop"
                };

                await InsertIntoTmsSignaLog(request);
            }
            finally
            {
                _dbLock.Release();
            }
        }

        #region Thay đổi nội dung VMS
        /// <summary>
        /// Author: TuanHA
        /// Description: Hàm thay đổi nội dung VMS cho MỘT thiết bị.
        ///              Giữ lại nguyên chữ ký để các luồng cũ (timer/schedule/process/reset) không phải sửa;
        ///              bên trong chuyển tiếp sang ChangeVmsContentMulti với danh sách 1 thiết bị.
        /// Created date: 11/03/2025 (chuyển tiếp sang bản gom nhóm: 22/09/2026 - ChuongLP)
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ChangeVmsContent(PrepareVmsData prepareVmsData, string equipmentId, bool isWriteLog = true, List<VmsProcess> processList = null)
        {
            return await ChangeVmsContentMulti(prepareVmsData, new List<string> { equipmentId }, isWriteLog);
        }

        /// <summary>
        /// Gửi nội dung ra NHIỀU thiết bị bằng MỘT gói tin điều khiển, sau đó ánh xạ kết quả từng thiết bị
        /// để gọi UpdateVmsCurrentSuccess / UpdateVmsCurrentError như luồng cũ.
        /// LƯU Ý khi đọc code: Event được đăng ký ĐÚNG MỘT LẦN, mỗi lần bắn sẽ thêm 1 task vào
        /// danh sách rồi Task.WhenAll ở cuối. Bản cũ gán vào biến afterSendTask nên với nhiều thiết bị
        /// chỉ chờ được task cuối cùng.
        /// Author: ChuongLP
        /// Created date: 22/09/2026
        /// </summary>
        /// <returns>true khi không có thiết bị nào thất bại (hoặc không có gì để gửi); false nếu có thiết bị gửi lỗi.</returns>
        public async Task<bool> ChangeVmsContentMulti(PrepareVmsData prepareVmsData, List<string> equipmentIds, bool isWriteLog = true)
            => (await ChangeVmsContentMultiWithResult(prepareVmsData, equipmentIds, isWriteLog)).Count == 0;

        /// <summary>Như ChangeVmsContentMulti nhưng trả về danh sách thiết bị gửi LỖI (rỗng = không có lỗi).</summary>
        public async Task<HashSet<string>> ChangeVmsContentMultiWithResult(PrepareVmsData prepareVmsData, List<string> equipmentIds, bool isWriteLog = true)
        {
            var failedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ids = (equipmentIds ?? new List<string>())
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct()
                        .ToList();

            if (ids.Count == 0) return failedIds;

            // Chuẩn bị dữ liệu gửi ra thiết bị cho từng thiết bị.
            var dataSendList = ids.Select(id => PrepareContentSendToDevice(prepareVmsData, id))
                                  .Where(d => d?.EquipmentInfoInput != null)
                                  .ToList();

            if (dataSendList.Count == 0) return failedIds;

            using (ManageSendToDevice manageSendToDivice = new ManageSendToDevice())
            {
                // Mỗi thiết bị phản hồi sẽ sinh 1 task cập nhật DB/log/socket; chờ hết ở cuối.
                var afterSendTasks = new List<Task>();
                var taskLock = new object();
                int successCount = 0;
                int failCount = 0;

                manageSendToDivice.OnSuccessSendToDeviceEvent += (sender, e) =>
                {
                    Interlocked.Increment(ref successCount);
                    var task = UpdateVmsCurrentSuccess(sender, e, prepareVmsData, isWriteLog);
                    lock (taskLock) { afterSendTasks.Add(task); }
                };

                manageSendToDivice.OnErrorSendToDeviceEvent += (sender, e) =>
                {
                    Interlocked.Increment(ref failCount);
                    var task = UpdateVmsCurrentError(sender, e, prepareVmsData, isWriteLog);
                    lock (taskLock) { afterSendTasks.Add(task); failedIds.Add(e.EquipmentId); }
                };

                // Phát lệnh và chờ kết quả (hàm này không ném exception: timeout cũng trả về kết quả thất bại).
                await manageSendToDivice.SendToDeviceMultiAsync(dataSendList);

                Task[] pending;
                lock (taskLock) { pending = afterSendTasks.ToArray(); }

                // Đợi cập nhật VmsCurrent / TmsSignalLog / TmsIncidentControl / socket xong hẳn.
                await Task.WhenAll(pending);

                // Fix lỗi V01 (Mục 9.2): Kích hoạt FailurePolicy cho luồng Auto khi toàn bộ thiết bị gửi thất bại
                var isAuto = prepareVmsData.ProcessInfoInput?.ExecuteBy?.Equals("auto", StringComparison.OrdinalIgnoreCase) == true
                             || prepareVmsData.ProcessInfoInput?.ProcessType?.Equals("auto", StringComparison.OrdinalIgnoreCase) == true;
                if (isAuto && successCount == 0 && failCount > 0)
                {
                    throw new Exception($"Gửi nội dung VMS tự động thất bại trên toàn bộ {failCount} thiết bị.");
                }

                return failedIds;
            }
        }

        /// <summary>
        /// Giữ lại các bản ghi điều khiển của sự cố KHÁC còn đang xử lý. Bản ghi của sự cố đã kết thúc / hủy / bỏ qua / bị xóa
        /// (còn sót do lần reset trước lỗi) là dữ liệu cũ, không được chặn việc trả biển về mặc định.
        /// Không tra được sự cố (lỗi) thì coi như còn hoạt động để không reset nhầm biển đang bị sự cố khác dùng.
        /// </summary>
        private async Task<List<TmsIncidentControl>> KeepActiveOtherIncidentControlsAsync(
            List<TmsIncidentControl>? controls, string currentIncidentId)
        {
            var result = new List<TmsIncidentControl>();
            if (controls == null) return result;

            var groups = controls
                .Where(c => !string.IsNullOrEmpty(c.IncidentId) && c.IncidentId != currentIncidentId)
                .GroupBy(c => c.IncidentId!);

            foreach (var group in groups)
            {
                var active = true;
                try
                {
                    var incident = await _messageBus.InvokeAsync<Modules.TMS.Core.Dto.Incident.TmsIncidentOutput>(new Modules.TMS.Core.Dto.Incident.TmsIncidentSingleInput { Id = group.Key });
                    active = incident != null && incident.IsDelete == null
                        && incident.State != "0" && incident.State != "4" && incident.State != "5";
                }
                catch (Exception)
                {
                }

                if (active) result.AddRange(group);
            }

            return result;
        }

        /// <summary>
        /// Thiết bị chỉ hiển thị một nội dung (LCS, hoặc VMS không có playlist sự cố) và sự cố vừa kết thúc là nội dung đang
        /// hiện -> gửi lại ảnh của sự cố còn lại có ưu tiên cao nhất (cùng mức: sự cố mới nhất). Sự cố vừa kết thúc chỉ đang
        /// chờ (ảnh trên thiết bị là của sự cố khác) thì giữ nguyên. Không ghi log/quyền giữ: quyền đã có sẵn.
        /// Author: ChuongLP
        /// Date: 2026-10-19
        /// </summary>
        private async Task RestoreRemainingIncidentContentAsync(string equipmentId, TmsIncidentControl? finishedControl,
            List<TmsIncidentControl> remainingControls, List<EquipmentInfoInput> infos)
        {
            var hasIncidentPlaylist = await _vmsProcessImg.AsQueryable()
                .InnerJoin<VmsProcess>((img, p) => img.ProcessId == p.ID)
                .Where((img, p) => p.VmsId == equipmentId && p.Type == VmsProcessTypeConst.ScheduleIncident
                    && p.State == VmsProcessStateConst.Run && (img.IsActive == null || img.IsActive == true))
                .AnyAsync();
            if (hasIncidentPlaylist) return;

            var vmsCurrent = await GetVmsCurrentByEquipment(equipmentId);
            if (!string.IsNullOrEmpty(finishedControl?.Url) && vmsCurrent != null
                && !string.Equals(finishedControl.Url, vmsCurrent.Url, StringComparison.OrdinalIgnoreCase))
                return;

            var owner = IncidentControlOccupancyHelper.PickDisplayOwner(
                remainingControls, Modules.TMS.Core.Constants.IncidentControlConst.DefaultPriority);
            if (owner == null || string.IsNullOrEmpty(owner.Url)) return;

            var prepareData = new PrepareVmsData
            {
                ListEquipmentInfos = infos,
                ContentInfoInput = new ContentInfoInput
                {
                    Url = owner.Url,
                    Brightness = 100,
                    IncidentId = owner.IncidentId,
                    Priority = owner.Priority
                }
            };
            prepareData.ProcessInfoInput.ProcessType = "restore";
            prepareData.ProcessInfoInput.ExecuteBy = "Reset";

            await ChangeVmsContent(prepareData, equipmentId, isWriteLog: false);
        }

        private static async Task<T> RetryTransientDbAsync<T>(Func<Task<T>> action, int maxAttempts = 3)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (InvalidOperationException ex) when (attempt < maxAttempts
                    && ex.Message.Contains("connection is closed", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(200 * attempt);
                }
            }
        }

        /// <summary>
        /// Reset toàn bộ thiết bị của 1 sự cố về trạng thái phù hợp theo Ma trận Mục 5.3 & 9.3:
        /// 1. Vô hiệu hóa ảnh của sự cố này trong VmsProcessImg (luồng luân phiên).
        /// 2. Xóa IncidentControl của RIÊNG sự cố này (Fix lỗi V02 & DEC-05).
        /// 3. Với từng thiết bị:
        ///    - Nếu còn sự cố khác đang active -> giữ nguyên, không reset default (DEC-05).
        ///    - Nếu hết sự cố: dừng schedule-incident, khôi phục lịch thường bị pause nếu có (DEC-04).
        ///    - Nếu không có lịch thường: mới đẩy ảnh DefaultUrl.
        /// Author: ChuongLP
        /// Date: 2026-10-14
        /// </summary>
        public async Task ResetIncidentToDefault(string incidentId)
        {
            if (string.IsNullOrWhiteSpace(incidentId)) return;

            // 1. Lấy danh sách thiết bị từ IncidentControl (đã distinct)
            // Reset chạy đúng lúc vòng playlist sự cố vẫn đang đọc/ghi DB nên có thể gặp lỗi kết nối thoáng qua
            // ("The connection is closed"); bước 1-3 được thử lại thay vì bỏ cuộc làm ảnh sự cố kẹt trên biển.
            var controls = await RetryTransientDbAsync(() =>
                _messageBus.InvokeAsync<List<TmsIncidentControl>>(new GetIncidentControlInput { IncidentId = incidentId }));
            if (controls == null || controls.Count == 0) return;

            var equipmentIds = controls.Select(c => c.EquipmentId).Distinct().ToList();

            // 2. Gỡ bỏ ảnh thuộc incidentId này khỏi VmsProcessImg của các tiến trình luân phiên
            await RetryTransientDbAsync(() => _vmsProcessImg.AsUpdateable()
                .SetColumns(img => img.IsActive, false)
                .Where(img => img.IncidentId == incidentId)
                .ExecuteCommandAsync());

            // 3. (Chuyển xuống cuối hàm) Xóa TmsIncidentControl của riêng incidentId này CHỈ KHI đẩy ảnh mặc định thành công,
            //    để gửi lỗi thì còn bản ghi mà thử lại. Vòng 4.1 đã loại sự cố hiện tại nên không phụ thuộc việc xóa sớm.
            var failedPushIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 4. Với từng thiết bị liên quan: kiểm tra ma trận trạng thái
            var allInfos = await GetListEquipmentInfo();
            var infos = allInfos.Where(e => equipmentIds.Contains(e.EquipmentId)).ToList();
            var scheduleService = _serviceProvider.GetRequiredService<ScheduleService>();

            // Thiết bị không còn sự cố nào khác giữ -> đưa về lịch thường / mặc định ở các bước gom nhóm bên dưới.
            var freeIds = new List<string>();
            foreach (var equipmentId in equipmentIds)
            {
                // 4.1 Kiểm tra thiết bị còn sự cố nào khác đang active không (DEC-05)
                // Chỉ tính sự cố KHÁC thật sự: bản ghi không có IncidentId (do lệnh thủ công/lịch ghi vào) không phải sự cố
                // và không được chặn việc trả biển về mặc định.
                var remainingControls = await KeepActiveOtherIncidentControlsAsync(
                    await GetIncidentControlsByEquipmentAsync(equipmentId), incidentId);
                if (remainingControls != null && remainingControls.Count > 0)
                {
                    // Thiết bị vẫn còn sự cố khác đang chiếm giữ -> KHÔNG reset default.
                    // Có playlist sự cố: vòng phát tự bỏ ảnh của sự cố này. Thiết bị chỉ hiện 1 ảnh (LCS): trả ảnh sự cố còn lại.
                    await RestoreRemainingIncidentContentAsync(equipmentId,
                        controls.FirstOrDefault(c => c.EquipmentId == equipmentId), remainingControls, infos);
                    continue;
                }

                freeIds.Add(equipmentId);
            }

            // Bước 4.2-4.4 xử lý THEO NHÓM thay vì từng biển: cùng kết quả nghiệp vụ, nhưng sự cố có nhiều biển không phải
            // chờ tuần tự từng lệnh gửi (biển offline mất tới cả chục giây mỗi biển).
            if (freeIds.Count > 0)
            {
                // 4.2 Dừng tiến trình schedule-incident của các thiết bị (nếu có)
                await scheduleService.StopIncidentScheduleByVmsIds(freeIds);

                // 4.3 Khôi phục lịch chiếu thường đang bị tạm dừng (DEC-04); biển đã resume thì không đẩy default url nữa
                var resumedList = await scheduleService.ResumeScheduleByVmsIds(freeIds);
                var resumedIds = (resumedList ?? new List<VmsProcess>()).Select(p => p.VmsId).ToHashSet(StringComparer.OrdinalIgnoreCase);

                // 4.4 Không có lịch thường -> Đẩy hình ảnh DefaultUrl ra thiết bị, gom các biển cùng nội dung mặc định vào một lệnh
                var defaultTargets = new List<(string EquipmentId, VmsCurrentOutput Current)>();
                foreach (var equipmentId in freeIds.Where(id => !resumedIds.Contains(id)))
                {
                    var vmsCurrent = await GetVmsCurrentByEquipment(equipmentId);
                    if (vmsCurrent != null && !string.IsNullOrEmpty(vmsCurrent.DefaultUrl))
                        defaultTargets.Add((equipmentId, vmsCurrent));
                }

                foreach (var group in defaultTargets.GroupBy(t => (t.Current.DefaultUrl, t.Current.DefaultColor, t.Current.DefaultBackColor)))
                {
                    var prepareData = new PrepareVmsData
                    {
                        ListEquipmentInfos = infos,
                        ContentInfoInput = new ContentInfoInput
                        {
                            Url = group.Key.DefaultUrl,
                            Color = group.Key.DefaultColor,
                            BackColor = group.Key.DefaultBackColor,
                            Brightness = 100,
                            IncidentId = incidentId,
                            Priority = 10
                        }
                    };
                    prepareData.ProcessInfoInput.ProcessType = "reset";
                    prepareData.ProcessInfoInput.ExecuteBy = "Reset";

                    var groupIds = group.Select(t => t.EquipmentId).ToList();
                    failedPushIds.UnionWith(await ChangeVmsContentMultiWithResult(prepareData, groupIds, isWriteLog: false));
                }
            }

            // 3. Xóa các bản ghi TmsIncidentControl của riêng incidentId này (Sửa lỗi V02!).
            if (failedPushIds.Count == 0)
            {
                await RetryTransientDbAsync(async () =>
                {
                    await _messageBus.InvokeAsync(new DeleteIncidentControlByIncidentInput { IncidentId = incidentId });
                    return true;
                });
                return;
            }

            // Có biển đẩy ảnh mặc định lỗi: chỉ giữ quyền của các biển đó (gọi reset lại sẽ thử lại đúng các biển này),
            // các biển đã xử lý xong vẫn được trả quyền như khi thành công toàn bộ.
            foreach (var equipmentId in equipmentIds.Where(id => !failedPushIds.Contains(id)))
            {
                try
                {
                    await _messageBus.InvokeAsync(new DeleteTmsIncidentControlInput { IncidentId = incidentId, EquipmentId = equipmentId });
                }
                catch (Exception)
                {
                    // Bản ghi đã bị xóa ở luồng khác -> bỏ qua.
                }
            }
        }

        #endregion

        #region Lấy các tiến trình lập lịch đang chạy
        /// <summary>
        /// Author: TuanHA
        /// Description: Hàm thay đổi nội dung VMS
        /// Created date: 18/03/2025
        /// </summary>
        /// <returns></returns>
        public async Task<List<VmsProcess>> GetListProcess(string? type = null, string? state = null)
		{
			var listProcess = await _vmsProcess.AsQueryable()
											   .WhereIF(!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(state),
															p => p.Type == type && p.State == state)
											   .ToListAsync();
			return listProcess;

		}
		#endregion

		#region Lấy các tiến trình lập lịch đang chạy
		/// <summary>
		/// Author: TuanHA
		/// Description: Lấy danh sách hình ảnh của các tiến trình đang chạy
		/// Created date: 18/03/2025
		/// </summary>
		/// <returns></returns>
		public async Task<List<VmsProcessImg>> GetListProcessImg(List<string> listProcessIds)
		{
			// Sắp xếp theo thứ tự tạo để giữ đúng thứ tự danh sách ảnh của lịch
			var listVmsProcessImg = await _vmsProcessImg.AsQueryable()
				.Where(msg => listProcessIds.Contains(msg.ProcessId))
				.OrderBy(msg => msg.CreateTime)
				.OrderBy(msg => msg.ID)
				.ToListAsync();
			return listVmsProcessImg;
		}
		#endregion

		/// <summary>
		/// Lấy danh sách các sự cố đang chiếm giữ thiết bị
		/// onlyActiveIncident = true: bỏ dòng không gắn sự cố và sự cố đã kết thúc/hủy/bỏ qua (dữ liệu sót lại).
		/// </summary>
		public async Task<List<TmsIncidentControl>> GetIncidentControlsByEquipmentAsync(string equipmentId, bool onlyActiveIncident = false)
		{
			try
			{
				var controls = await _messageBus.InvokeAsync<List<TmsIncidentControl>>(new GetIncidentControlByEquipmentInput
				{
					EquipmentIds = new List<string> { equipmentId },
					OnlyActiveIncident = onlyActiveIncident
				});
				return controls ?? new List<TmsIncidentControl>();
			}
			catch
			{
				return new List<TmsIncidentControl>();
			}
		}

		/// <summary>
		/// Xóa bỏ quyền sở hữu của các sự cố cũ trên thiết bị khi có sự cố mới Ghi đè (DEC-01)
		/// </summary>
		public async Task ClearOldIncidentControlsForOverrideAsync(List<string> equipmentIds, string? currentIncidentId)
		{
			if (equipmentIds == null || equipmentIds.Count == 0) return;
			try
			{
				var oldControls = await _messageBus.InvokeAsync<List<TmsIncidentControl>>(new GetIncidentControlByEquipmentInput
				{
					EquipmentIds = equipmentIds,
					ExcludeIncidentId = currentIncidentId ?? ""
				});

				if (oldControls != null && oldControls.Count > 0)
				{
					foreach (var ctrl in oldControls)
					{
						await _messageBus.InvokeAsync(new DeleteTmsIncidentControlInput
						{
							EquipmentId = ctrl.EquipmentId,
							IncidentId = ctrl.IncidentId
						});
					}
				}
			}
			catch (Exception)
			{
			}
		}

		/// <summary>
		/// Lấy các tiến trình đang chạy cần khởi động lại khi server restart (DEC-07, Section 5.4)
		/// Lấy cả schedule và schedule-incident có State = 'run'
		/// </summary>
		public async Task<List<VmsProcess>> GetActiveProcessesForRestartAsync()
		{
			var activeTypes = new List<string> { VmsProcessTypeConst.Schedule, VmsProcessTypeConst.ScheduleIncident };
			return await _vmsProcess.AsQueryable()
				.Where(p => activeTypes.Contains(p.Type) && p.State == VmsProcessStateConst.Run)
				.ToListAsync();
		}

		public async Task<List<tmsSignalLogResponse>> GetIncidentAsync(string incident, string token)
		{
			try
			{
				// Khởi tạo HttpClient (khuyến khích dùng Singleton hoặc DI trong thực tế)
				using var client = new HttpClient();

				// Thêm token vào header
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
				var path = $"/api/tms/tmssignallog/incident?incident={incident}";
				var host = FileServerUtil.GetFileServer("");
				var fullUrl = $"{host}/{path?.TrimStart('/')}";
				// Gọi API
				var response = await client.GetAsync(fullUrl);
				// Kiểm tra trạng thái HTTP
				if (!response.IsSuccessStatusCode)
				{
					var errorMessage = await response.Content.ReadAsStringAsync();
					throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {errorMessage}");
				}

				var result = response.Content.Adapt<List<tmsSignalLogResponse>>(); //SONTH Fix lại sau do đổi sửa hàm
				// Đọc nội dung response
				//var result = await response.ToResult<List<tmsSignalLogResponse>>();

				return result;
			}
			catch (Exception ex)
			{
				// Ghi log lỗi (nếu cần)
				Console.WriteLine($"Error in GetIncidentAsync: {ex.Message}");
				return null;
			}
		}

		// Method to log the last image when stopping VMS
		public async Task logLastImageAsync(string vmsId, string imageUrl, string data, string token, string type, string OldUrl = null, int timetolive = 0, string incidentId = null)
		{
			try
			{
				using (var _client = new HttpClient())
				{
					// Tạo dữ liệu request
					var requestData = new
					{
						vmsId,
						imageUrl,
						data,
						type,
						OldUrl,
						timetolive,
						incidentId
					};

					// Chuyển object sang JSON
					var jsonContent = JsonConvert.SerializeObject(requestData);
					var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

					// Thêm token vào header
					_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
					var path = "/api/tms/tmssignallog/stopprocess";
					var client = FileServerUtil.GetFileServer("");
					var fullUrl = $"{client}/{path?.TrimStart('/')}";
					// Gọi API
					var response = await _client.PostAsync(fullUrl, httpContent);

					// Kiểm tra kết quả
					if (response.IsSuccessStatusCode)
					{
						var result = await response.Content.ReadAsStringAsync();
						Console.WriteLine($"Response: {result}");
					}
					else
					{
						Console.WriteLine($"Failed to post data: {response.StatusCode}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error logging last image: {ex.Message}");
			}
		}

		/// //////////////////////
		#region lập lịch
		private string FormatImageData(List<string> dataList)
		{
			var formattedStrings = new List<string>();
			for (int i = 0; i < dataList.Count; i++)
			{
				formattedStrings.Add($"{dataList[i]}");
			}
			return string.Join(Environment.NewLine, formattedStrings);
		}

		#endregion
	}
}
