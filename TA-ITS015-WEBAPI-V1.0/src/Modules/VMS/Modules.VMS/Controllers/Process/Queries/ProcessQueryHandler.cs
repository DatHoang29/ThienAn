using Mapster;
using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.VMS.Controllers.Process.Queries
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các truy vấn ra của đối tượng VmsProcess
    /// Create date: 14/10/2024
    /// </summary>
    public class ProcessQueryHandler:IWolverineHandler
    {
        private BaseRepository<VmsProcess> _vmsProcessRepository;
		private BaseRepository<VmsProcessImg> _vmsProcessImgRepository;
		private BaseCacheService _cache;
		private readonly IMessageBus _messageBus;

        public ProcessQueryHandler(BaseRepository<VmsProcess> vmsProcessRepository, BaseRepository<VmsProcessImg> vmsProcessImgRepository, BaseCacheService cache, UserManager userManager,
            IMessageBus messageBus)
        {
            this._cache = cache;
            this._messageBus = messageBus;
            this._vmsProcessRepository = vmsProcessRepository;
            this._vmsProcessImgRepository = vmsProcessImgRepository;
		}

		/// <summary>
		/// Author: TuanHA
		/// Description: Hàm list danh sách theo giá trị trường "code" truyền vào
		/// Created date: 12/03/2025
		/// </summary>
		/// <param name="command">Đối tượng VmsProcessInput</param>
		/// <returns>Thông tin cấu hình và hình ảnh process VMS </returns>
		public async Task<List<InformationProcessOutput>> HandleAsync(InformationProcessInput command)
		{
			// Create object InformationProcessOutput to return
			List<InformationProcessOutput> listInformationProcessOutput = new List<InformationProcessOutput>();

			// Get information of process
			var listProcess = await this._vmsProcessRepository.AsQueryable().ToListAsync();
			if (listProcess == null || listProcess.Count < 1) 
            {
                return listInformationProcessOutput;
			}

			foreach (var process in listProcess) 
			{
				InformationProcessOutput informationProcessOutput = new InformationProcessOutput();

				// Get list process image (sắp xếp theo thứ tự tạo để hiển thị lên FE đúng thứ tự)
				var listVmsProcessImg = await this._vmsProcessImgRepository.AsQueryable()
					.Where(p => p.ProcessId == process.ID)
					.OrderBy(p => p.CreateTime)
					.OrderBy(p => p.ID)
					.ToListAsync();

				// Add information to object InformationProcessOutput
				informationProcessOutput = process.Adapt<InformationProcessOutput>();
				informationProcessOutput.ControlType = process.Type;
				informationProcessOutput.ScheduleList = new List<ControlScheduleInput>();
				informationProcessOutput.EquipmentId = process.VmsId;

				foreach (var vmsProcessImg in listVmsProcessImg)
				{
					if (process.Type == "schedule" || process.Type == "schedule-incident")
					{
						informationProcessOutput.ScheduleList.Add(new ControlScheduleInput()
						{
							Url = vmsProcessImg.Url,
							TimeToLive = vmsProcessImg.TimeToLive.Value,
						});
					}
					else if(process.Type == "timer" && process.State == "stop")
					{
						// Get image default
						informationProcessOutput.Url = "\\Upload\\NoImage.bmp";
					}
					else
					{
						informationProcessOutput.Url = vmsProcessImg.Url;
					}					
				}								
				listInformationProcessOutput.Add(informationProcessOutput);
			}

			return listInformationProcessOutput;

		}

		/// <summary>
		/// Author: ChuongLP
		/// Description: Lấy danh sách hình ảnh đang hiển thị/append trên các thiết bị phục vụ Modal xác nhận điều khiển sự cố
		/// Created date: 2026-10-15
		/// </summary>
		public async Task<List<EquipmentPlaylistOutput>> HandleAsync(GetActivePlaylistInput command)
		{
			var result = new List<EquipmentPlaylistOutput>();
			if (command.EquipmentIds == null || !command.EquipmentIds.Any())
				return result;

			var db = this._vmsProcessRepository.Context;
			int maxItems = await Modules.VMS.Infrastructure.Services.Schedule.ScheduleService.ReadMaxIncidentPlaylistItemsAsync(_messageBus);

			foreach (var eqId in command.EquipmentIds)
			{
				var equipOutput = new EquipmentPlaylistOutput
				{
					EquipmentId = eqId,
					MaxPlaylistItems = maxItems,
					Items = new List<PlaylistItemOutput>()
				};

				// 1. Kiểm tra VmsProcess đang chạy (schedule-incident hoặc schedule)
				var activeProcess = await this._vmsProcessRepository.AsQueryable()
					.Where(p => p.VmsId == eqId && p.State == "run" && (p.Type == "schedule-incident" || p.Type == "schedule"))
					.FirstAsync();

				if (activeProcess != null)
				{
					var imgs = await this._vmsProcessImgRepository.AsQueryable()
						.Where(img => img.ProcessId == activeProcess.ID && (img.IsActive == null || img.IsActive == true))
						.OrderBy(img => img.CreateTime)
						.OrderBy(img => img.ID)
						.ToListAsync();

					int order = 1;
					foreach (var img in imgs)
					{
						equipOutput.Items.Add(new PlaylistItemOutput
						{
							Id = img.ID,
							Url = img.Url,
							TimeToLive = img.TimeToLive ?? 10,
							IncidentId = img.IncidentId,
							IsIncident = !string.IsNullOrEmpty(img.IncidentId) || (img.SourceType != null && img.SourceType.Contains("incident")),
							OrderNo = order++
						});
					}
				}
				else
				{
					// 2. Nếu không có process schedule, lấy ảnh hiện tại từ VmsCurrent
					var current = await db.Queryable<VmsCurrent>()
						.Where(c => c.EquipmentId == eqId)
						.FirstAsync();

					if (current != null && !string.IsNullOrEmpty(current.Url))
					{
						equipOutput.Items.Add(new PlaylistItemOutput
						{
							Id = current.ID,
							Url = current.Url,
							TimeToLive = 10,
							IncidentId = null,
							IsIncident = false,
							OrderNo = 1
						});
					}
				}

				result.Add(equipOutput);
			}

			return result;
		}
	}
}
