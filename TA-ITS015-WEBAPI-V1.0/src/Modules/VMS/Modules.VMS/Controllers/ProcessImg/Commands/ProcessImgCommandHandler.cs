
using Furion.FriendlyException;
using Mapster;
using Modules.VMS.Controllers.ProcessImg.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.VMS.Controllers.ProcessImg.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng VmsProcessImg
    /// Created date: 14/10/2024
    /// </summary>
    public class ProcessImgCommandHandler:IWolverineHandler
    {
        private BaseRepository<VmsProcessImg> _baseRepository;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public ProcessImgCommandHandler(BaseRepository<VmsProcessImg> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._userManager = userManager;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng VmsProcessImg
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="input">Đối tượng AddVmsProcessImgInput</param>
        /// <returns></returns>
        public async Task HandleAsync(AddVmsProcessImgInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await this._baseRepository.IsAnyAsync(msg => msg.IsDelete == null  ))
                throw Oops.Oh(BaseMsg.BaseEntity.Name + " " + BaseMsg.BaseException.Exist);

            await this._baseRepository.InsertWithDiffLogAsync(command.Adapt<VmsProcessImg>());
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật dữ liệu cho bảng VmsProcessImg
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="command">Đối tượng UpdateVmsProcessImgInput</param>
        /// <returns></returns>
        public async Task HandleAsync(UpdateVmsProcessImgInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await this._baseRepository.IsAnyAsync(msg => msg.IsDelete == null  ))
                throw Oops.Oh(BaseMsg.BaseEntity.Name + " " + BaseMsg.BaseException.Exist);

            VmsProcessImg msg = await this._baseRepository.GetByIdAsync(command.ID);
            if (msg == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // TODO: Check authorization
            //if (!this._userManager.SuperAdmin || msg.CreateUId != this._userManager.UserId)
            //    throw Oops.Oh(VmsProcessImgMsg.BaseException.NoPermission);

            await this._baseRepository.UpdateWithDiffLogAsync(command.Adapt(msg));
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa một record trong bảng VmsProcessImg
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="command">Đối tượng DeleteVmsProcessImgInput</param>
        /// <returns></returns>
        public async Task HandleAsync(DeleteVmsProcessImgInput command)
        {
            VmsProcessImg msg = await this._baseRepository.GetFirstAsync(msg => msg.ID == command.ID);
            if (msg == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // TODO: Check authorization
            //if (!this._userManager.SuperAdmin || msg.CreateUId != this._userManager.UserId)
            //    throw Oops.Oh(VmsProcessImgMsg.BaseException.NoPermission);

            await this._baseRepository.SoftDeleteAsync<VmsProcessImg>(msg);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng VmsProcessImg
        /// Created date: 14/10/2024
        /// </summary>
        /// <param name="command">Danh sách DeleteVmsProcessImgInput</param>
        /// <returns></returns>
        public async Task HandleAsync(List<DeleteVmsProcessImgInput> command)
        {
            List<string> ids = command.Select(item  => item.ID).ToList();
            List<VmsProcessImg> msgList = await this._baseRepository.AsQueryable().Where(msg => ids.Contains(msg.ID)).ToListAsync();

            if (msgList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // TODO: Check authorization
            //if (!this._userManager.SuperAdmin || msgList.Any(msg => msg.CreateUId != this._userManager.UserId))
            //    throw Oops.Oh(VmsProcessImgMsg.BaseException.NoPermission);

            await this._baseRepository.SoftDeleteAsync<VmsProcessImg>(msg => ids.Contains(msg.ID));
        }
    }
}
