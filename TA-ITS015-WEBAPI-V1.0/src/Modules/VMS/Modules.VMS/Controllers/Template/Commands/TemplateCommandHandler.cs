#region using reference

// For: Entity
using Furion.FriendlyException;
using Mapster;
using Microsoft.AspNetCore.Http;
using Modules.VMS.Controllers.Template.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Template;
using Modules.VMS.Infrastructure.Services.Imaging;
using Shared.Core.Interfaces;
using Shared.Core.Utilities;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

#endregion

namespace Modules.VMS.Controllers.Template.Commands
{
    /// <summary>
    /// Author: Ng?c Th?ng
    /// Description: L?p nghi?p v? x? l? các l?nh vào c?a ð?i tý?ng VmsTemplate
    /// Created date: 09/08/2024
    /// </summary>
    [WolverineHandler]
    public class TemplateCommandHandler : IScoped
    {
        private BaseRepository<VmsTemplate> _vmsTemplateRep;
        private BaseCacheService _cache;
        private IUploadService _uploadService;
        private TemplateService _templateService;
        private VmsImageGenService vmsImageGenService;
        public TemplateCommandHandler(BaseRepository<VmsTemplate> vmsTemplateRep,
            BaseCacheService cache,
            IUploadService uploadService,
            TemplateService templateService,
            VmsImageGenService vmsImageGenService)
        {
            this._cache = cache;
            this._vmsTemplateRep = vmsTemplateRep;
            _uploadService = uploadService;
            _templateService = templateService;
            this.vmsImageGenService = vmsImageGenService;
        }

        /// <summary>
        /// Author: KhoaND SonTH
        /// Description: Hàm lýu thông tin xu?ng b?ng VmsTemplate
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="input">Ð?i tý?ng AddVmsTemplateInput</param>
        /// <returns></returns>
        public async Task HandleAsync(AddTemplateInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Lo?i b? kho?ng tr?ng ? ð?u 

            if (await this._vmsTemplateRep.IsAnyAsync(img => img.Code == command.Code && img.Lang == command.Lang))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            //Handle upload file
            await HandleUploadFile(command);
            //TODO: transaction
            await this._vmsTemplateRep.InsertWithDiffLogAsync(command.Adapt<VmsTemplate>());
            

        }

        /// <summary> 
        /// Author: KhoaND SonTH
        /// Description: Hàm c?p nh?t d? li?u cho b?ng VmsTemplate
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="command">Ð?i tý?ng UpdateTemplateInput</param>
        /// <returns></returns>
        public async Task HandleAsync(UpdateTemplateInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Lo?i b? kho?ng tr?ng ? ð?u 

            if (await this._vmsTemplateRep.IsAnyAsync(img => img.Code == command.Code && img.Lang == command.Lang && img.ID != command.ID))
                throw Oops.Oh( BaseMsg.BaseException.Exist,BaseMsg.BaseEntity.CodeOrName);

            var msg = await this._vmsTemplateRep.GetByIdAsync(command.ID) ?? 
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);


            //Handle upload file
            await HandleUploadFile(command);
            // TODO: Check authorization
            //if (!this._userManager.SuperAdmin || msg.CreateUId != this._userManager.UserId)
            //    throw Oops.Oh(BaseMsg.BaseException.NoPermission);

            await this._vmsTemplateRep.UpdateWithDiffLogAsync(command.Adapt(msg));
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa m?t record trong b?ng VmsTemplate
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="command">Ð?i tý?ng DeleteTemplateInput</param>
        /// <returns></returns>
        public async Task HandleAsync(DeleteTemplateInput command)
        {
            VmsTemplate msg = await this._vmsTemplateRep.GetFirstAsync(msg => msg.ID == command.ID);
            if (msg == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // TODO: Check authorization
            //if (!this._userManager.SuperAdmin || msg.CreateUId != this._userManager.UserId)
            //    throw Oops.Oh(BaseMsg.BaseException.NoPermission);

            await this._vmsTemplateRep.SoftDeleteAsync<VmsTemplate>(msg);
        }

        /// <summary>
        /// Author: KhoaND SonTH
        /// Description: Hàm xóa nhi?u record trong b?ng VmsTemplate
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="command">Danh sách DeleteTemplateInput</param>
        /// <returns></returns>
        public async Task HandleAsync(List<DeleteTemplateInput> command)
        {
            List<string> ids = command.Select(item  => item.ID).ToList();
            List<VmsTemplate> msgList = await this._vmsTemplateRep.AsQueryable().Where(msg => ids.Contains(msg.ID)).ToListAsync();

            if (msgList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // TODO: Check authorization
            //if (!this._userManager.SuperAdmin || msgList.Any(msg => msg.CreateUId != this._userManager.UserId))
            //    throw Oops.Oh(BaseMsg.BaseException.NoPermission);

            await this._vmsTemplateRep.SoftDeleteAsync<VmsTemplate>(msg => ids.Contains(msg.ID));
        }


        /// <summary>
        /// Desc: Hàm x? l? upload file
        /// Author: SonTH
        /// Create Date: 2024/12/04
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [WolverineIgnore]
        private async Task HandleUploadFile(AddTemplateInput input)
        {

            if (!string.IsNullOrEmpty(input.JsonContent))
            {
                //Upload file json
                var fileJson = await _uploadService.UploadTextAsync(new BaseUploadInput()
                {
                    Data = input.JsonContent,
                    Extension = ".json",
                    FolderPath = "Vms\\Template\\Json", //TODO C?u h?nh l?i
                    FileName = input.ID
                    
                });

                input.JsonData = fileJson;
            }
            
            //Generate preview

            var file = await _templateService.GenerateImage(input.JsonContent, input.Size);

            var img =await _uploadService.UploadAsync(file, "Vms\\Template\\Preview"); //TODO duong dan
            input.Preview = img;
        }
    }
}
