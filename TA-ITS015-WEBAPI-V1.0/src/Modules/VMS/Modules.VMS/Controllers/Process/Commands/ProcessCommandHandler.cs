using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Infrastructure.Services.Process;
using Modules.VMS.Infrastructure.Services.Schedule;

namespace Modules.VMS.Controllers.Process.Commands
{
    public class ProcessCommandHandler:IWolverineHandler
    {
 
	     private readonly VmsProcessService _vmsProcessService;

		public ProcessCommandHandler(VmsProcessService vmsProcessService)
        {
			_vmsProcessService = vmsProcessService;
		}

		public async Task<List<ProcessStatusOutput>> HandleAsync(ProcessStatusInput command)
		{
            var result = new List<ProcessStatusOutput>();
            foreach (var threadId in ScheduleService.GetRunningThreadIds())
            {
                result.Add(new ProcessStatusOutput { VmsId = threadId, Status = true });
            }
            return result;
        }

		#region Hàm gửi lại nội dung cho thiết bị
		/// <summary>
		/// Description: Handler xử lý gửi lại nội dung ra thiết bị
		///Author: TuanHA
		///Create Date: 26/03/2025
		/// </summary>
		/// <param name="listProcess"> Danh sách các tiến trình gửi ại nội dung cho thiết bị </param>
		/// <returns></returns>
		public async Task HandleAsync(ResendToDeviceInput input)
		{
			try
			{
				if (input.ListProcess != null && input.ListProcess.Count > 0)
				{
					// Get list equipment info
					var listEquipmentInfos = await _vmsProcessService.GetListEquipmentInfo(input.ListProcess);

					// [SỬA 06/10/2026] Dòng trên đã chạm DB nên AsyncLocal của SqlSugarScope đang giữ 1 connection.
					// Khởi chạy Parallel trong SuppressFlow để mỗi nhánh tự lấy connection riêng, không dùng chung
					// (tránh "The connection's current state is connecting"). Không await bên trong khối using.
					Task parallelTask;
					using (ExecutionContext.SuppressFlow())
					{
						parallelTask = Parallel.ForEachAsync(input.ListProcess, async (process, ct) =>
						{
							var vmsCurrent = input.ListCurrent.FirstOrDefault(p => p.EquipmentId == process.VmsId);
							if (vmsCurrent != null)
							{
								var prepareData = new PrepareVmsData();
								prepareData.ListEquipmentInfos = listEquipmentInfos.Adapt<List<EquipmentInfoInput>>();
								prepareData.ContentInfoInput.Url = vmsCurrent?.Url ?? "";
								await _vmsProcessService.ChangeVmsContent(prepareData, process.VmsId, false, input.ListProcess);
							}
						});
					}
					await parallelTask;
				}				
			}
			catch (Exception ex)
			{			

			}
		}
		#endregion
	}
}
