using Mapster;
using Modules.VMS.Controllers.Message.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.VMS.Controllers.Message.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    public class VmsMessageCommandHandler : IWolverineHandler
    {
        private BaseRepository<VmsMessage> _messageRepo;
        private BaseCacheService _cache;

        public VmsMessageCommandHandler(BaseRepository<VmsMessage> messageRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._messageRepo = messageRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới một record vào bảng
        /// Created date: 01/12/2024
        /// </summary>
        public async Task HandleAsync(AddVmsMessageInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            // Chỉ loại Message (Type = "1") mới dùng Content2. Loại khác (Phrase...) không lưu Content2 để tránh dữ liệu thừa.
            if (command.Type != "1")
                command.Content2 = null;

            if (await this._messageRepo.IsAnyAsync(msg => msg.IsDelete == null && msg.Code == command.Code && msg.Type == command.Type && msg.Lang == command.Lang))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            await this._messageRepo.InsertWithDiffLogAsync(command.Adapt<VmsMessage>());
            _cache.RemoveByPrefixKey(CacheConst.Vms.VmsMessage); // ChuongLP : Xóa cache để cập nhật dữ liệu mới nhất
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record trong bảng
        /// Created date: 01/12/2024
        /// </summary>
        public async Task HandleAsync(UpdateVmsMessageInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            // Chỉ loại Message (Type = "1") mới dùng Content2. Loại khác (Phrase...) không lưu Content2 để tránh dữ liệu thừa.
            if (command.Type != "1")
                command.Content2 = null;

            if (await this._messageRepo.IsAnyAsync(msg => msg.IsDelete == null && msg.Code == command.Code && msg.Type == command.Type && msg.Lang == command.Lang && msg.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var msg = await this._messageRepo.GetByIdAsync(command.ID);

            if (msg == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            command.Adapt(msg);

            // Mapster bật IgnoreNullValues nên Adapt bỏ qua null ⇒ gán tay để xoá được Content2 khi đổi sang loại khác Message.
            msg.Content2 = command.Content2;

            await this._messageRepo.UpdateWithDiffLogAsync(msg);

            _cache.RemoveByPrefixKey(CacheConst.Vms.VmsMessage); // ChuongLP - 01/04/2026
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record trong bảng
        /// Created date: 01/12/2024
        /// </summary>
        public async Task HandleAsync(DeleteVmsMessageInput command)
        {
            var msg = await this._messageRepo.GetFirstAsync(msg => msg.ID == command.ID);

            if (msg == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._messageRepo.SoftDeleteAsync<VmsMessage>(msg);

            _cache.RemoveByPrefixKey(CacheConst.Vms.VmsMessage); // ChuongLP - 01/04/2026
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record trong bảng
        /// Created date: 01/12/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteVmsMessageInput> command)
        {
            List<string> ids = command.Select(item  => item.ID).ToList();

            List<VmsMessage> msgList = await this._messageRepo.AsQueryable().Where(msg => ids.Contains(msg.ID)).ToListAsync();

            if (msgList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._messageRepo.SoftDeleteAsync<VmsMessage>(msg => ids.Contains(msg.ID));

            _cache.RemoveByPrefixKey(CacheConst.Vms.VmsMessage); // ChuongLP - 01/04/2026
        }
    }
}
