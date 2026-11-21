using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Imaging;
using Shared.Infrastructure.Extensions;

namespace Modules.VMS.Controllers.Current.Commands
{
    public class DefaultCommandHandler : IScoped
    {
        private readonly BaseRepository<VmsCurrent> _baseRepository;
        private readonly ImageService _imageService;

        public DefaultCommandHandler(BaseRepository<VmsCurrent> baseRepository, ImageService imageService)
        {
            _baseRepository = baseRepository;
            _imageService = imageService;
        }

        /// <summary>
        /// Upsert n?i dung m?c ð?nh (template) cho thi?t b? VMS theo EquipmentId
        /// Author: ChuongLP
        /// Date: 2026-07-14
        /// </summary>
        public async Task HandleAsync(VmsDefaultCurrentInput command)
        {
            command.Code = command.Code?.Trim();

            // N?u DefaultUrl là base64 (?nh v?a preview) -> lýu file, l?y path týõng ð?i
            string? defaultUrl = command.DefaultUrl;
            if (_imageService.IsBase64String(defaultUrl))
                defaultUrl = await _imageService.SaveImageAsync(defaultUrl, Guid.NewGuid().ToString("N"), "upload\\vms\\template\\preview");

            // Ch?n cùng m?t d?ng v?i lu?ng reset (ýu tiên d?ng ðang gi? default, r?i d?ng c? nh?t)
            // ð? "s?a default" và "reset" luôn thao tác trên ðúng cùng m?t d?ng VmsCurrent.
            var entity = await _baseRepository.AsQueryable()
                .Where(x => x.EquipmentId == command.EquipmentId)
                .OrderBy(x => x.DefaultUrl, OrderByType.Desc)
                .OrderBy(x => x.CreateTime, OrderByType.Asc)
                .FirstAsync();
            if (entity == null)
            {
                // Chýa có d?ng -> t?o m?i (ch? default content)
                entity = new VmsCurrent
                {
                    EquipmentId = command.EquipmentId,
                    Code = command.Code,
                    Name = command.Name,
                    DefaultRowData = command.DefaultRowData,
                    DefaultUrl = defaultUrl,
                    DefaultColor = command.DefaultColor,
                    DefaultBackColor = command.DefaultBackColor,
                    DefaultFont = command.DefaultFont,
                    Size = command.Size,
                };
                await _baseRepository.InsertWithDiffLogAsync(entity);
            }
            else
            {
                // Ð? có -> ch? c?p nh?t Default*, GI? NGUYÊN RowData/Url (n?i dung live)
                entity.DefaultRowData = command.DefaultRowData;
                entity.DefaultUrl = defaultUrl;
                entity.DefaultColor = command.DefaultColor;
                entity.DefaultBackColor = command.DefaultBackColor;
                entity.DefaultFont = command.DefaultFont;
                if (!string.IsNullOrWhiteSpace(command.Size)) entity.Size = command.Size;
                if (!string.IsNullOrWhiteSpace(command.Name)) entity.Name = command.Name;
                await _baseRepository.UpdateWithDiffLogAsync(entity);
            }
            // KHÔNG broadcast xu?ng thi?t b?: ðây là n?i dung m?c ð?nh (template), không phát live.
        }


        /// <summary>
        /// Xóa n?i dung m?c ð?nh (template) cho thi?t b? VMS theo ID
        /// Author: ChuongLP
        /// Date: 2026-07-14
        /// </summary>
        public async Task HandleAsync(DeleteVmsDefaultCurrentInput command)
        {
            var entity = await _baseRepository.GetFirstAsync(x => x.ID == command.ID);
            if (entity == null) throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            entity.DefaultRowData = null;
            entity.DefaultUrl = null;
            entity.DefaultColor = null;
            entity.DefaultBackColor = null;
            entity.DefaultFont = null;
            await _baseRepository.UpdateWithDiffLogAsync(entity);
        }
    }
}
