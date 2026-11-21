using Mapster;
using Module.CCTV.Core.Helper;
using Modules.CCTV.Controllers.Template.Dto;
using Modules.CCTV.Core.Entities;
using Modules.CCTV.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.CCTV.Controllers.Dashboard.Commands
{
    public class TemplateCommandHandler : IWolverineHandler
    {
        private BaseRepository<CctvTemplate> _cctvTemplateRep;
        private BaseCacheService _cache;

        public TemplateCommandHandler(BaseRepository<CctvTemplate> cctvTemplateRep, BaseCacheService cache)
        {
            _cctvTemplateRep = cctvTemplateRep;
            _cache = cache;
        }

        /// <summary>
        /// Thêm mới 1 Template.
        /// Author: ChuongLP
        /// Date: 2026-06-04
        /// </summary>
        public async Task<TemplateCctvOutput> HandleAsync(AddTemplateInput command)
        {
            if (await _cctvTemplateRep.IsAnyAsync(u => u.Name == command.Name))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Name]);


            var now = DateTime.Now;

            var entity = new CctvTemplate
            {
                ID = Guid.NewGuid().ToString(),
                Name = command.Name,
                Remark = command.Remark,
                Status = command.Status,
                Item = TemplateItemHelper.SerializeItems(command.Items),   // ← convert tại đây
                CreateUId = App.User.GetUsername(),
                CreateTime = now,
            };

            await _cctvTemplateRep.InsertWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvTemplate);

            return DataOutput(entity, command.Items); // Trả về luôn Items đã convert để tránh phải convert lại khi lấy từ DB lên (vì đã ghi đè toàn bộ JSON rồi)
        }

        /// <summary>
        /// Chỉnh sửa 1 Template.
        /// Author: ChuongLP
        /// Date: 2026-06-04
        /// </summary>
        public async Task<TemplateCctvOutput> HandleAsync(UpdateTemplateInput command)
        {
            if (await _cctvTemplateRep.IsAnyAsync(u => u.Name == command.Name && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var entity = await _cctvTemplateRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.NotExist);

            entity.Name = command.Name;
            entity.Remark = command.Remark;
            entity.Status = command.Status;
            entity.Item = TemplateItemHelper.SerializeItems(command.Items);   // ← ghi đè toàn bộ JSON
            entity.UpdateUId = App.User.GetUsername();
            entity.UpdateTime = DateTime.Now;

            await _cctvTemplateRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvTemplate);

            return DataOutput(entity, command.Items); // ← convert lại tại đây
        }

        /// <summary>
        /// Xóa 1 Template.
        /// Author: ChuongLP
        /// Date: 2026-06-04
        /// </summary>
        public async Task HandleAsync(DeleteTemplateInput command)
        {
            var entity = await _cctvTemplateRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.NotExist);

            await _cctvTemplateRep.DeleteAsync(l => l.ID == entity.ID);
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvTemplate);
        }

        /// <summary>
        /// Xử lý dữ liệu đầu ra cho các Template.
        /// Author: ChuongLP
        /// Date: 2026-06-04
        /// </summary>
        private static TemplateCctvOutput DataOutput(CctvTemplate entity, List<TemplateSlot>? items)
        {
            return new()
            {
                ID = entity.ID,
                Name = entity.Name,
                Remark = entity.Remark,
                Status = entity.Status,
                Items = items ?? new List<TemplateSlot>(),
                CreateTime = entity.CreateTime,
                CreateUId = entity.CreateUId,
                UpdateTime = entity.UpdateTime,
                UpdateUId = entity.UpdateUId,
            };
        }
    }
}
