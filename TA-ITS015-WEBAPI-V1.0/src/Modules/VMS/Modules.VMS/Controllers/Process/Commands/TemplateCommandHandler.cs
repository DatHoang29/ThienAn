using Microsoft.AspNetCore.Hosting;
using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure.Services.Process;
using Modules.VMS.Infrastructure.Services.Schedule;
using Modules.VMS.Infrastructure.Services.Imaging;
using Shared.DTO.Constants.Module;
using Shared.Infrastructure.Services;

namespace Modules.VMS.Controllers.Process.Commands
{
	public class TemplateCommandHandler :  IWolverineHandler
	{
		private readonly VmsProcessService _vmsProcessService;
        private readonly ScheduleService _scheduleService;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager _userManager;
        private readonly VmsImageGenService _vmsImageGenService;

		public TemplateCommandHandler(VmsProcessService vmsProcessService, ScheduleService scheduleService,
			IWebHostEnvironment env, UserManager userManager, VmsImageGenService vmsImageGenService	)
		{
			_vmsProcessService = vmsProcessService;
			_env = env;
            _userManager = userManager;
			_scheduleService = scheduleService;
            _vmsImageGenService = vmsImageGenService;
        }

		public async Task HandleAsync(TemplateControlInput command)
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

            // 1.3. Kiểm tra content truyền vào
            if (string.IsNullOrEmpty(command.JsonContent))
            {
                throw Oops.Oh(BaseMsg.Vms.Validation.NoContent);
            }

            // 1.4. Kiểm tra size hình, nếu không có set mặc định
            if (string.IsNullOrEmpty(command.Size))
			{
				command.Size = "384x64";
			}

            // 1.5. Render ảnh từ JsonContent đã chỉnh sửa rồi lưu file, lấy url ảnh mới
            command.Url = _vmsImageGenService.GenerateImage(command, _env.WebRootPath);

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
                // Fallback: 100% logic gốc cũ
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
                // Chế độ Nối tiếp:
                var isAuto = !string.IsNullOrEmpty(command.ExecuteBy);
                var sourceType = isAuto ? VmsProcessImgSourceTypeConst.IncidentAuto
                    : (!string.IsNullOrEmpty(command.IncidentId) ? VmsProcessImgSourceTypeConst.IncidentManual : VmsProcessImgSourceTypeConst.ManualAppend);

                foreach (var equipId in equipmentIds)
                {
                    await _scheduleService.AppendImageToIncidentProcess(
                        vmsId: equipId,
                        incidentId: command.IncidentId,
                        url: command.Url,
                        ttl: command.TimeToLive,
                        priority: command.Priority,
                        sourceType: sourceType,
                        brightness: command.Brightness ?? 100);
                }
            }
            else // Override
            {
                // 1. Lịch thường: sự cố -> tạm dừng chờ khôi phục (DEC-04); thủ công không gắn sự cố -> dừng hẳn
                await _scheduleService.HoldScheduleBeforeOverrideAsync(equipmentIds, command.IncidentId);

                // 2. Dọn sạch tiến trình schedule-incident cũ nếu có (DEC-01)
                await _scheduleService.StopIncidentScheduleByVmsIds(equipmentIds);

                // 3. Xóa quyền các sự cố cũ trên thiết bị (DEC-01)
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
