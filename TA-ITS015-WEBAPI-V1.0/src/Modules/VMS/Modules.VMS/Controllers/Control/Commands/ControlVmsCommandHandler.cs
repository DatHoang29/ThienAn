using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Dto.Process;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Module;
using Shared.Infrastructure.Services;

namespace Modules.VMS.Controllers.Control.Commands
{
	/// <summary>
	/// Điều khiển VMS
	/// </summary>
	public class ControlVmsCommandHandler : IWolverineHandler
	{
		private IMessageBus _messBus;
		public ControlVmsCommandHandler(IMessageBus messBus)
		{
			_messBus = messBus;
        }

		public async Task HandleAsync(AddControlInput input)
		{
			switch (input.ControlType)
			{
				//TODO: const lựa chọn
				case VmsControlTypeConst.Timer: // Hẹn giờ
					var timerProcessInput = input.Adapt<TimerProcessInput>(); // Gáng giá trị từ input vào timerProcessInput
					timerProcessInput.EquipmentIds = input.EquipmentList.Select(q => q.ID).ToList();
					await _messBus.InvokeAsync(timerProcessInput);  // InvokeAsync sẽ gửi message đến handler tương ứng để xử lý, ở đây là TimerProcessInputHandler
					break;
				case VmsControlTypeConst.Schedule: // Lập lịch
					var scheduleProcessInput = input.Adapt<ScheduleProcessInput>();
					scheduleProcessInput.EquipmentIds = input.EquipmentList.Select(q => q.ID).ToList();
					await _messBus.InvokeAsync(scheduleProcessInput);
					break;
				case VmsControlTypeConst.Image:		// ChuongLP - 20260710 - Bổ sung thêm luồng xử lý của LCS
					switch (input.DeviceType?.Trim().ToLower())
					{
						case VmsDeviceTypeConst.VMS:
                            var imageInput = input.Adapt<ImageControlInput>();
                            await _messBus.InvokeAsync(imageInput);
                            break;
						case VmsDeviceTypeConst.LCS:
                            var lcsImageInput = input.Adapt<LcsImageControlInput>();
                            await _messBus.InvokeAsync(lcsImageInput);
                            break;
                    }
					break;
				case VmsControlTypeConst.Template:
					var templateInput = input.Adapt<TemplateControlInput>();  
					await _messBus.InvokeAsync(templateInput);
					break;
                case VmsControlTypeConst.Text:
                case VmsControlTypeConst.Message:
                case VmsControlTypeConst.Phrase: // text; message, phrase library || TODO: Tạo const cụ thể tránh gửi sai vào đây
					TextToImageProcessInput textProcessInput = new TextToImageProcessInput();		// Tạo DTO trung gian cho luồng text
                    textProcessInput.EquipmentIds = input.EquipmentList.Select(q => q.ID).ToList(); // Lấy danh sách theo ID
					textProcessInput.TextToImageInput = input.Adapt<TextToImageInput>();			// Mapter các field
                    textProcessInput.TextToImageInput.Code = input.ControlType;						// Ghi lại nguồn để truy vết
                    textProcessInput.ExecuteBy = input.ExecuteBy;                                   // Chuyển actor (auto = SYSTEM_AUTOMATION, manual = null)
					await _messBus.InvokeAsync(textProcessInput);
					break;
                default:
                    // Theo TODO "tránh gửi sai vào đây": chặn controlType không hợp lệ
                    throw Oops.Oh(BaseMsg.BaseException.IncorrectAcceptValue, BaseMsg.BaseEntity.Type);
            }
		}
	}
}
