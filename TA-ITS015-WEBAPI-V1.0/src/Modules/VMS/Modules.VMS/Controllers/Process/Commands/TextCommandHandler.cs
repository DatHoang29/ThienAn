using Microsoft.AspNetCore.Hosting;
using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure.Services.Imaging;
using Modules.VMS.Infrastructure.Services.Process;
using Modules.VMS.Infrastructure.Services.Schedule;
using Shared.DTO.Constants.Module;
using Shared.Infrastructure.Services;

namespace Modules.VMS.Controllers.Process.Commands
{
	public class TextCommandHandler :  IWolverineHandler
	{
		private readonly VmsProcessService _vmsProcessService;
		private readonly VmsImageGenService _vmsImageGenService;
		private readonly ScheduleService _scheduleService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager _userManager;

		public TextCommandHandler(VmsProcessService vmsProcessService, VmsImageGenService vmsImageGenService,
			IWebHostEnvironment env, UserManager userManager, ScheduleService scheduleService)
		{
			_vmsProcessService = vmsProcessService;
			_vmsImageGenService = vmsImageGenService;
			_env = env;
            _userManager = userManager;
            _scheduleService = scheduleService;
        }

		public async Task HandleAsync(TextToImageProcessInput command)
		{
			// 1. Kiểm tra thông tin thiết lập
			// 1.1. Kiểm tra thiết bị có được chọn hay không
			if (command.EquipmentIds.Count < 1)
			{
				// TODO: Thiết bị chưa được chọn
				throw Oops.Oh(BaseMsg.VmsProcess.Validation.NoDeviceSelected);
			}

			// 1.2. Kiểm tra thiết bị có tồn tại hay không (lấy dữ liệu qua MessBus thay cho BaseApiClient)
			await _vmsProcessService.ValidateEquipmentExist(command.EquipmentIds);

			// 1.3. Kiểm tra content truyền vào
			if(string.IsNullOrEmpty(command.TextToImageInput.Content1) || string.IsNullOrEmpty(command.TextToImageInput.Content2))
			{
				throw Oops.Oh(BaseMsg.Vms.Validation.NoContent);
			}

			// 1.4. Kiểm tra size hình, nếu không có set mặc định
			if (string.IsNullOrEmpty(command.TextToImageInput.Size))
			{
				command.TextToImageInput.Size = "384x64";
			}

            #region. Dừng các tiến trình lập lịch
            //// Dừng các tiến trình lập lịch
            //// TODO: Ham rieng		

            //// Lấy danh sách VmsId hiện tại
            //List<VmsProcess> processList = await _vmsProcessRepository.AsQueryable().Where(a => command.EquipmentIds.Contains(a.VmsId)).ToListAsync();
            //var listVmsId = processList.Select(l => l.VmsId).ToList();

            //// Tìm các process có State = "run" và Type = "schedule" (lập lịch) để dừng
            //// Cập nhật các tiến trình lập lịch State về stop
            //await _vmsProcessRepository.AsUpdateable().Where(l => listVmsId.Contains(l.VmsId) && l.State == "run" && l.Type == "schedule")
            //						.SetColumns(l => l.State, "stop")
            //						.ExecuteCommandAsync();

            //// Dừng thread chạy tiến trình lập lịch
            //foreach (var vmsId in listVmsId)
            //{
            //	ScheduleCommandHandler.StopThreadRunSchedule(vmsId);
            //}

            #endregion

            // Convert text to image
            string savePath = _env.WebRootPath;
            AddControlInput controlInput = command.TextToImageInput.Adapt<AddControlInput>();
            controlInput.ControlType = command.TextToImageInput.Code; // Code mang loại điều khiển (text/message/phrase)
            var urlImage = _vmsImageGenService.GenerateImage(controlInput, savePath);

            var equipmentIds = command.EquipmentIds.ToList();
            var isAppendModeEnabled = App.GetConfig<bool?>("VmsConfig:EnableIncidentAppendMode") ?? false;

            if (!isAppendModeEnabled)
            {
                // Fallback: 100% logic gốc cũ
                await _scheduleService.StopScheduleByVmsIds(equipmentIds);

                var allListEquipmentInfos = await _vmsProcessService.GetListEquipmentInfo();
                var listEquipmentInfoSendToDevice = allListEquipmentInfos.Where(q => equipmentIds.Contains(q.EquipmentId)).ToList();

                var prepareData = new PrepareVmsData();
                prepareData.ListEquipmentInfos = listEquipmentInfoSendToDevice.Adapt<List<EquipmentInfoInput>>();
                prepareData.ContentInfoInput = command.TextToImageInput.Adapt<ContentInfoInput>();
                prepareData.ContentInfoInput.IncidentId = command.TextToImageInput.IncidentId;
                prepareData.ContentInfoInput.Url = urlImage;

                var isAutoProcess = !string.IsNullOrEmpty(command.ExecuteBy);
                prepareData.ProcessInfoInput.ProcessType = isAutoProcess ? "auto" : "manual";
                prepareData.ProcessInfoInput.ExecuteBy = isAutoProcess ? command.ExecuteBy : _userManager.Account;

                await _vmsProcessService.ChangeVmsContentMulti(prepareData, equipmentIds, true);
                return;
            }

            // Logic mới (EnableIncidentAppendMode == true)
            var playMode = string.IsNullOrEmpty(command.TextToImageInput.PlayMode) ? VmsPlayModeConst.Override : command.TextToImageInput.PlayMode.ToLower();

            if (playMode == VmsPlayModeConst.Append)
            {
                // Chế độ Nối tiếp:
                var isAuto = !string.IsNullOrEmpty(command.ExecuteBy);
                var sourceType = isAuto ? VmsProcessImgSourceTypeConst.IncidentAuto
                    : (!string.IsNullOrEmpty(command.TextToImageInput.IncidentId) ? VmsProcessImgSourceTypeConst.IncidentManual : VmsProcessImgSourceTypeConst.ManualAppend);

                foreach (var equipId in equipmentIds)
                {
                    await _scheduleService.AppendImageToIncidentProcess(
                        vmsId: equipId,
                        incidentId: command.TextToImageInput.IncidentId,
                        url: urlImage,
                        ttl: command.TextToImageInput.TimeToLive,
                        priority: command.TextToImageInput.Priority,
                        sourceType: sourceType,
                        brightness: command.TextToImageInput.Brightness);
                }
            }
            else // Override
            {
                // 1. Lịch thường: sự cố -> tạm dừng chờ khôi phục (DEC-04); thủ công không gắn sự cố -> dừng hẳn
                await _scheduleService.HoldScheduleBeforeOverrideAsync(equipmentIds, command.TextToImageInput.IncidentId);

                // 2. Dọn sạch tiến trình schedule-incident cũ nếu có (DEC-01)
                await _scheduleService.StopIncidentScheduleByVmsIds(equipmentIds);

                // 3. Xóa quyền các sự cố cũ trên thiết bị (DEC-01)
                await _vmsProcessService.ClearOldIncidentControlsForOverrideAsync(equipmentIds, command.TextToImageInput.IncidentId);

                // 4. Phát lệnh hiển thị ảnh trực tiếp ra thiết bị
                var allListEquipmentInfos = await _vmsProcessService.GetListEquipmentInfo();
                var listEquipmentInfoSendToDevice = allListEquipmentInfos.Where(q => equipmentIds.Contains(q.EquipmentId)).ToList();

                var prepareData = new PrepareVmsData();
                prepareData.ListEquipmentInfos = listEquipmentInfoSendToDevice.Adapt<List<EquipmentInfoInput>>();
                prepareData.ContentInfoInput = command.TextToImageInput.Adapt<ContentInfoInput>();
                prepareData.ContentInfoInput.IncidentId = command.TextToImageInput.IncidentId;
                prepareData.ContentInfoInput.Url = urlImage;
                prepareData.ContentInfoInput.PlayMode = VmsPlayModeConst.Override;

                var isAutoProcess = !string.IsNullOrEmpty(command.ExecuteBy);
                prepareData.ProcessInfoInput.ProcessType = isAutoProcess ? "auto" : "manual";
                prepareData.ProcessInfoInput.PlayMode = VmsPlayModeConst.Override;
                prepareData.ProcessInfoInput.ExecuteBy = isAutoProcess ? command.ExecuteBy : _userManager.Account;

                await _vmsProcessService.ChangeVmsContentMulti(prepareData, equipmentIds, true);
            }
        }
    }
}
