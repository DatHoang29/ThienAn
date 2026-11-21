using Microsoft.AspNetCore.Hosting;
using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Imaging;
using Modules.VMS.Infrastructure.Services.Process;
using Modules.VMS.Infrastructure.Services.Schedule;
using Shared.DTO.Constants.Module;
using Shared.Infrastructure.Services;

namespace Modules.VMS.Controllers.Process.Commands
{
	public class ImageCommandHandler :  IWolverineHandler
	{
		private readonly VmsProcessService _vmsProcessService;
        private readonly ScheduleService _scheduleService;
        private readonly UserManager _userManager;
		private readonly VmsImageGenService _vmsImageGenService;
        private readonly IWebHostEnvironment _env;

		public ImageCommandHandler(VmsProcessService vmsProcessService, ScheduleService scheduleService,
		 UserManager userManager, VmsImageGenService vmsImageGenService, IWebHostEnvironment env)
		{
			_vmsProcessService = vmsProcessService;
			_scheduleService = scheduleService;
            _userManager = userManager;
            this._vmsImageGenService = vmsImageGenService;
            this._env = env;
        }

		public async Task HandleAsync(ImageControlInput command)
		{
			// 1. Kiểm tra thông tin thiết lập
			// 1.1. Kiểm tra thiết bị có được chọn hay không
			if (command.EquipmentList.Count < 1)
			{
				// TODO: Thiết bị chưa được chọn
				throw Oops.Oh(BaseMsg.VmsProcess.Validation.NoDeviceSelected);
			}

			// 1.2. Kiểm tra thiết bị có tồn tại hay không (lấy dữ liệu qua MessBus thay cho BaseApiClient)
			await _vmsProcessService.ValidateEquipmentExist(command.EquipmentList.Select(l => l.ID).ToList());

            // 1.3. Kiểm tra size hình, nếu không có set mặc định
            if (string.IsNullOrEmpty(command.Size))
            {
                command.Size = "384x64";
            }

            // 1.4. Nếu có JsonContent (template đã chỉnh sửa) -> render ảnh mới từ JSON và dùng làm Url
            if (!string.IsNullOrEmpty(command.JsonContent))
            {
                command.Url = _vmsImageGenService.GenerateImage(command, _env.WebRootPath);
            }

            // 1.5. Kiểm tra content truyền vào
            if (string.IsNullOrEmpty(command.Url))
            {
                throw Oops.Oh(BaseMsg.Vms.Validation.NoContent);
            }


            #region. Dừng các tiến trình lập lịch
            //// Dừng các tiến trình lập lịch
            //// TODO: Ham rieng		

            //// Lấy danh sách VmsId hiện tại
            //List<VmsProcess> processList = await _vmsProcessRepository.AsQueryable().Where(a => command.EquipmentList.Select(l=>l.ID).Contains(a.VmsId)).ToListAsync();
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

            var equipmentIds = command.EquipmentList.Select(l => l.ID).ToList();
            var isAppendModeEnabled = App.GetConfig<bool?>("VmsConfig:EnableIncidentAppendMode") ?? false;

            if (!isAppendModeEnabled)
            {
                // Fallback: 100% logic gốc cũ để back up nếu luồng mới sai
                await _scheduleService.StopScheduleByVmsIds(equipmentIds);

                var allListEquipmentInfos = await _vmsProcessService.GetListEquipmentInfo();
                var listEquipmentInfoSendToDevice = allListEquipmentInfos.Where(q => equipmentIds.Contains(q.EquipmentId)).ToList();

                var prepareData = new PrepareVmsData();
                prepareData.ListEquipmentInfos = listEquipmentInfoSendToDevice.Adapt<List<EquipmentInfoInput>>();
                prepareData.ContentInfoInput = command.Adapt<ContentInfoInput>();
                prepareData.ContentInfoInput.IncidentId = command.IncidentId;
                prepareData.ContentInfoInput.Url = command.Url;

                var isAutoProcess = !string.IsNullOrEmpty(command.ExecuteBy);
                prepareData.ProcessInfoInput.ProcessType = isAutoProcess ? "auto" : "manual";
                prepareData.ProcessInfoInput.ExecuteBy = isAutoProcess ? command.ExecuteBy : _userManager.Account;

                await _vmsProcessService.ChangeVmsContentMulti(prepareData, equipmentIds, true);
                return;
            }

            // Logic mới (EnableIncidentAppendMode == true)
            var playMode = string.IsNullOrEmpty(command.PlayMode) ? VmsPlayModeConst.Override : command.PlayMode.ToLower();

            if (playMode == VmsPlayModeConst.Append)
            {
                // Chế độ Nối tiếp (Append):
                var isAuto = !string.IsNullOrEmpty(command.ExecuteBy);
                var sourceType = isAuto ? VmsProcessImgSourceTypeConst.IncidentAuto
                    : (!string.IsNullOrEmpty(command.IncidentId) ? VmsProcessImgSourceTypeConst.IncidentManual : VmsProcessImgSourceTypeConst.ManualAppend);

                // Gọi service để append ảnh vào tiến trình sự cố (schedule-incident) hiện tại của thiết bị
                foreach (var equipId in equipmentIds)
                {
                    await _scheduleService.AppendImageToIncidentProcess(vmsId: equipId,incidentId: command.IncidentId, url: command.Url, ttl: command.TimeToLive,
                        priority: command.Priority, sourceType: sourceType, brightness: command.Brightness ?? 100);
                }
            }
            else // Override
            {
                // 1. Lịch thường: sự cố -> tạm dừng chờ khôi phục; thủ công không gắn sự cố -> dừng hẳn
                await _scheduleService.HoldScheduleBeforeOverrideAsync(equipmentIds, command.IncidentId);

                // 2. Dọn sạch tiến trình schedule-incident cũ nếu có 
                await _scheduleService.StopIncidentScheduleByVmsIds(equipmentIds);

                // 3. Xóa quyền các sự cố cũ trên thiết bị
                await _vmsProcessService.ClearOldIncidentControlsForOverrideAsync(equipmentIds, command.IncidentId);

                // 4. Phát lệnh hiển thị ảnh trực tiếp ra thiết bị
                var allListEquipmentInfos = await _vmsProcessService.GetListEquipmentInfo();
                var listEquipmentInfoSendToDevice = allListEquipmentInfos.Where(q => equipmentIds.Contains(q.EquipmentId)).ToList();

                var prepareData = new PrepareVmsData();
                prepareData.ListEquipmentInfos = listEquipmentInfoSendToDevice.Adapt<List<EquipmentInfoInput>>();
                prepareData.ContentInfoInput = command.Adapt<ContentInfoInput>();
                prepareData.ContentInfoInput.IncidentId = command.IncidentId;
                prepareData.ContentInfoInput.Url = command.Url;
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
