using Microsoft.AspNetCore.Http;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Current;
using Modules.VMS.Infrastructure.Services.Imaging;
using Modules.VMS.Infrastructure.Services.Process;
using Shared.Infrastructure.Extensions;


namespace Modules.VMS.Controllers.Current.Commands
{
	/// <summary>
	/// Author: Ngọc Thắng
	/// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng VmsCurrent
	/// Created date: 09/08/2024
	/// </summary>
	public class CurrentCommandHandler : IScoped
	{
		private VmsCurrentService _VmsCurrentService;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private VmsProcessService _VmsProcessService;
		private ImageService _imageService;
		private BaseRepository<Core.Entities.VmsCurrent> _baseRepository;
        public CurrentCommandHandler(VmsCurrentService VmsCurrentService, IHttpContextAccessor httpContextAccessor, 
			VmsProcessService vmsProcessService, ImageService imageService, BaseRepository<Core.Entities.VmsCurrent> baseRepository)
		{
			this._VmsCurrentService = VmsCurrentService;
			_httpContextAccessor = httpContextAccessor;
			_VmsProcessService = vmsProcessService;
            _imageService = imageService;
			this._baseRepository = baseRepository;
		}

		/// <summary>
		/// Author: KhoaND
		/// Description: Hàm lưu thông tin xuống bảng VmsCurrent
		/// Created date: 09/08/2024
		/// </summary>
		/// <param name="input">Đối tượng AddVmsCurrentInput</param>
		/// <returns></returns>
		public async Task HandleAsync(AddVmsCurrentInput command)
		{
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            var authorizationHeader =  _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
			string token = "";

			if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
			{
				token = authorizationHeader.Substring("Bearer ".Length).Trim();
			}
			foreach (var vmsId in command.ListEquipment)
			{
				Core.Entities.VmsCurrent entity = await this._baseRepository.GetByIdAsync(command.ID);

				// Iterate over each URL in ProcessImgInput.listUrl
				if (this._imageService.IsBase64String(command.Url))
				{
					command.Url = await this._imageService.SaveImageAsync(command.Url, entity.ID, "upload\\vms\\template\\preview");
                }
				else
				{
					command.Url = this._VmsCurrentService.WriteTextToImage(command, command.BackColor, 100);
				}
				var currentInput = new AddVmsCurrentInput
				{
					EquipmentId = vmsId.EquipmentId,
					Code = vmsId.Code,
					Name = vmsId.Name,  // Add 7 hours to TimeEnd
					Url = command.Url,
					Priority = command.Priority, // Add 7 hours to TimeStart
					RowData = command.RowData,
					IncidentId = command.IncidentId,
					BackColor = command.BackColor,
					Color = command.Color,
				};

				///////////////////////
				if (await this._baseRepository.IsAnyAsync(msg => msg.IsDelete == null && msg.Name == currentInput.Name && msg.EquipmentId == currentInput.EquipmentId))
					throw Oops.Oh(BaseMsg.BaseEntity.Name + " " + BaseMsg.BaseException.Exist);
				VmsCurrent msg = command.Adapt<VmsCurrent>();

				currentInput.Url = this._VmsCurrentService.WriteTextToImage(currentInput, currentInput.BackColor, 100);

				// Upload new image
				if (this._imageService.IsBase64String(currentInput.Url))
                    msg.Url = await this._imageService.SaveImageAsync(currentInput.Url, msg.ID, "upload\\vms\\template\\preview");
                else
                    msg.Url = currentInput.Url;

				await this._baseRepository.InsertWithDiffLogAsync(currentInput.Adapt<VmsCurrent>());
				ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.VmsUpdateImage, new { ActionType = "vms-success", Data = currentInput.Adapt<VmsCurrent>() });
				///////////////////////

				_VmsProcessService.logLastImageAsync(vmsId.EquipmentId, command.Url, command.RowData, token, "User", null, 0, command.IncidentId); // Pass the last image URL
			}            
		}

		public async Task HandleAsync(UpdateVmsCurrentInputByProcess command)
		{
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            var vmsCurrent = command.Adapt<VmsCurrent>();
            var existing = await _baseRepository.GetFirstAsync(x => x.Code == vmsCurrent.Code);
            if (existing != null)
            {
                // Tiến trình chỉ cập nhật nội dung hiển thị hiện tại; KHÔNG được ghi đè các giá trị mặc định.
                // Giữ nguyên các cột Default* và Size vì command (từ ContentInfoInput) không mang các giá trị này (nếu update full sẽ bị set NULL).
                vmsCurrent.DefaultUrl = existing.DefaultUrl;
                vmsCurrent.DefaultColor = existing.DefaultColor;
                vmsCurrent.DefaultBackColor = existing.DefaultBackColor;
                vmsCurrent.DefaultRowData = existing.DefaultRowData;
                vmsCurrent.DefaultFont = existing.DefaultFont;
                vmsCurrent.Size = existing.Size;

                await _baseRepository.UpdateAsync(vmsCurrent);
            }
            else
			    await this._baseRepository.InsertAsync(vmsCurrent);
		}

		


		/// <summary>
		/// Author: KhoaND
		/// Description: Hàm cập nhật dữ liệu cho bảng VmsCurrent
		/// Created date: 09/08/2024
		/// </summary>
		/// <param name="command">Đối tượng UpdateVmsCurrentInput</param>
		/// <returns></returns>
		public async Task HandleAsync(UpdateVmsCurrentInput command)
		{
			var authorizationHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
			string token = "";

			if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
			{
				token = authorizationHeader.Substring("Bearer ".Length).Trim();
			}
			foreach (var vmsId in command.ListEquipment)
			{
				// Iterate over each URL in ProcessImgInput.listUrl
				Core.Entities.VmsCurrent entity = await this._VmsCurrentService.GetvmsCurrentEquipmentIDAsync(vmsId.EquipmentId);
				if (this._imageService.IsBase64String(command.Url))
				{
                    command.Url = await this._imageService.SaveImageAsync(command.Url, entity.ID, "upload\\vms\\template\\preview");
                }
                else
				{
					command.Url = this._VmsCurrentService.WriteTextToImage(command, command.BackColor, 100);
				}
				var currentInput = new UpdateVmsCurrentInput
				{
					ID = entity.ID,
					EquipmentId = vmsId.EquipmentId,
					Code = vmsId.Code,
					Name = vmsId.Name,
					Url = command.Url,
					Priority = command.Priority,
					DefaultUrl = entity.DefaultUrl,
					RowData = command.RowData,
					OldRowData = command.OldRowData,
					OldUrl = command.OldUrl,
					IncidentId = command.IncidentId,
					BackColor = command.BackColor,
					Color = command.Color,
				};


				///////////////////////
				//if (await this._baseRepository.IsAnyAsync(msg => msg.IsDelete == null && msg.ID != command.ID))
				//    throw Oops.Oh(VmsCurrentMsg.BaseEntity.Name + " " + VmsCurrentMsg.BaseException.Exist);

				VmsCurrent msg = await this._baseRepository.GetByIdAsync(currentInput.ID);
				if (msg == null)
					throw Oops.Oh(BaseMsg.BaseException.DataNotExist);


				// TODO: Check authorization
				//if (!this._userManager.SuperAdmin || msg.CreateUId != this._userManager.UserId)
				//    throw Oops.Oh(VmsCurrentMsg.BaseException.NoPermission);

				await this._baseRepository.UpdateWithDiffLogAsync(currentInput.Adapt(msg));
				ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.VmsUpdateImage, new { ActionType = "vms-success", Data = currentInput.Adapt(msg) });
				///////////////////////

				_VmsProcessService.logLastImageAsync(vmsId.EquipmentId, command.Url, command.RowData, token, "User", null, 0, command.IncidentId); // Pass the last image URL
			}
		}

		/// <summary>
		/// Author: KhoaND
		/// Description: Hàm xóa một record trong bảng VmsCurrent
		/// Created date: 09/08/2024
		/// </summary>
		/// <param name="command">Đối tượng DeleteVmsCurrentInput</param>
		/// <returns></returns>
		public async Task HandleAsync(DeleteVmsCurrentInput command)
		{
			VmsCurrent msg = await this._baseRepository.GetFirstAsync(msg => msg.ID == command.ID);
			if (msg == null)
				throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

			// TODO: Check authorization
			//if (!this._userManager.SuperAdmin || msg.CreateUId != this._userManager.UserId)
			//    throw Oops.Oh(VmsCurrentMsg.BaseException.NoPermission);

			await this._baseRepository.SoftDeleteAsync<VmsCurrent>(msg);
		}

		/// <summary>
		/// Author: KhoaND
		/// Description: Hàm xóa nhiều record trong bảng VmsCurrent
		/// Created date: 09/08/2024
		/// </summary>
		/// <param name="command">Danh sách DeleteVmsCurrentInput</param>
		/// <returns></returns>
		public async Task HandleAsync(List<DeleteVmsCurrentInput> command)
		{
			List<string> ids = command.Select(item  => item.ID).ToList();
			List<VmsCurrent> msgList = await this._baseRepository.AsQueryable().Where(msg => ids.Contains(msg.ID)).ToListAsync();

			if (msgList?.Count() != ids.Count)
				throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

			// TODO: Check authorization
			//if (!this._userManager.SuperAdmin || msgList.Any(msg => msg.CreateUId != this._userManager.UserId))
			//    throw Oops.Oh(VmsCurrentMsg.BaseException.NoPermission);

			await this._baseRepository.SoftDeleteAsync<VmsCurrent>(msg => ids.Contains(msg.ID));
		}



	}
}
