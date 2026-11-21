using Mapster;
using Microsoft.AspNetCore.Http;
using Modules.WP.Controllers.Post.Dto;
using Modules.WP.Controllers.TrafficLaw.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Core.Exceptions;
using Modules.WP.Infrastructure;
using Shared.Core.Interfaces;
using Shared.Core.Utilities;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.DTO.Upload;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.WP.Controllers.TrafficLaw.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các lệnh ghi (Add/Update/Delete/ChangeStatus/UploadPdf) cho quy định giao thông hệ thống WP
    /// Created date: 03/07/2026
    /// </summary>
    public  class TrafficLawCommandHandler:IWolverineHandler
    {
        private BaseRepository<WpTrafficLaw> _trafficLawRep;
        private BaseCacheService _cache;
        private UserManager _userManager;
        private IUploadService _uploadService;

        public TrafficLawCommandHandler(BaseRepository<WpTrafficLaw> trafficLawRep, BaseCacheService cache, UserManager userManager, IUploadService uploadService)
        {
            _trafficLawRep = trafficLawRep;
            _cache = cache;
            _userManager = userManager;
            _uploadService = uploadService;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 Traffic Law
        /// Created date: 14/08/2026
        /// </summary>



        public async Task HandleAsync(WpAddTrafficLawInput command)
        {
            command.Title = command.Title?.Trim();
            command.Author = command.Author?.Trim();
            command.Code = command.Code?.Trim();
            command.IsPublish ??= BaseEnums.StatusEnum.Disable;
            var isExist = await _trafficLawRep.AsQueryable()
                .AnyAsync(u => u.Code == command.Code );

            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            if (command.IsPublish == BaseEnums.StatusEnum.Enable)
            {
                var hasPublishedRecord = await _trafficLawRep.AsQueryable()
                    .ClearFilter()
                    .AnyAsync(u => u.IsPublish == BaseEnums.StatusEnum.Enable);

                if (hasPublishedRecord)
                {
                    throw Oops.Oh(BaseMsg.WP.Exception.OnlyOnePublishedTrafficLaw);
                }
            }
            command.CreateUId = null;
            await _trafficLawRep.InsertWithDiffLogAsync(command.Adapt<WpTrafficLaw>());
            _cache.RemoveByPrefixKey(CacheConst.WP.WpTrafficLaw);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 Traffic Law
        /// Created date: 14/08/2026
        /// </summary>


        public async Task HandleAsync(WpUpdateTrafficLawInput command)
        {
            command.Title = command.Title?.Trim();
            command.Author = command.Author?.Trim();
            command.IsPublish ??= BaseEnums.StatusEnum.Disable;

            if (string.IsNullOrWhiteSpace(command.Code))
            {
                throw Oops.Oh(BaseMsg.BaseValidation.Required, BaseMsg.BaseEntity.Code);
            }
            var isExist = await _trafficLawRep.AsQueryable()
                .AnyAsync(u => u.Code == command.Code && u.ID != command.ID);

            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            var lawPos = await _trafficLawRep.AsQueryable()
               .ClearFilter()
               .Where(u => u.ID == command.ID)
               .FirstAsync() 
               ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            if (command.IsPublish == BaseEnums.StatusEnum.Enable)
            {
                await _trafficLawRep.AsUpdateable()
                    .SetColumns(x => new WpTrafficLaw
                    {
                        IsPublish = BaseEnums.StatusEnum.Disable
                    })
                    .Where(x =>
                        x.IsPublish == BaseEnums.StatusEnum.Enable &&
                        x.ID != command.ID)
                    .ExecuteCommandAsync();
                lawPos.IsPublish = BaseEnums.StatusEnum.Enable;
            }
            else
            {
                var isOnlyPublished = await _trafficLawRep.AsQueryable()
                    .Where(x => x.IsPublish == BaseEnums.StatusEnum.Enable)
                    .CountAsync();

                if (lawPos.IsPublish == BaseEnums.StatusEnum.Enable && isOnlyPublished == 1)
                {
                    throw Oops.Oh(BaseMsg.WP.Exception.MustKeepOnePublishedTrafficLaw);
                }

                lawPos.IsPublish = BaseEnums.StatusEnum.Disable;
            }
            await _trafficLawRep.UpdateWithDiffLogAsync(command.Adapt(lawPos));
            _cache.RemoveByPrefixKey(CacheConst.WP.WpTrafficLaw);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 Traffic Law (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>


        public async Task HandleAsync(WpDeleteTrafficLawInput command)
        {
            var sysTrafficLaw = await _trafficLawRep.GetFirstAsync(u => u.ID == command.ID)
                 ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            if(sysTrafficLaw.IsPublish == BaseEnums.StatusEnum.Enable)
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse); //sửa lại message
            await _trafficLawRep.SoftDeleteAsync<WpTrafficLaw>(sysTrafficLaw);
            _cache.RemoveByPrefixKey(CacheConst.WP.WpTrafficLaw);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều Traffic Law (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>

        public async Task HandleAsync(List<WpDeleteTrafficLawInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _trafficLawRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            var hasPublishedRecord = await _trafficLawRep.AsQueryable()
                .AnyAsync(u => ids.Contains(u.ID) && u.IsPublish == BaseEnums.StatusEnum.Enable);
            if (hasPublishedRecord)
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse); //sửa lại message

            await _trafficLawRep.SoftDeleteAsync<WpTrafficLaw>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.WP.WpTrafficLaw);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Change Stattus Traffic Law theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<WpChangeStattusTrafficLawOutput> HandleAsync(WpChangeStattusTrafficLawInput command)
        {
            var entity = await _trafficLawRep.AsQueryable()
                .ClearFilter()
                .FirstAsync(x => x.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            if (command.IsPublish == BaseEnums.StatusEnum.Enable)
            {
                await _trafficLawRep.AsUpdateable()
                     .Where(x =>
                        x.IsPublish == BaseEnums.StatusEnum.Enable &&
                        x.ID != command.ID)
                    .SetColumns(x => new WpTrafficLaw
                    {
                        IsPublish = BaseEnums.StatusEnum.Disable
                    })
                    .ExecuteCommandAsync();
                entity.IsPublish = BaseEnums.StatusEnum.Enable;
            }
            else
            {
                var isOnlyPublished = await _trafficLawRep.AsQueryable()
                    .Where(x => x.IsPublish == BaseEnums.StatusEnum.Enable)
                    .CountAsync();

                if (entity.IsPublish == BaseEnums.StatusEnum.Enable && isOnlyPublished == 1)
                {
                    throw Oops.Oh(BaseMsg.WP.Exception.MustKeepOnePublishedTrafficLaw);
                }

                entity.IsPublish = BaseEnums.StatusEnum.Disable;
            }

            await _trafficLawRep.UpdateAsync(entity);

            _cache.RemoveByPrefixKey(CacheConst.WP.WpTrafficLaw);

            return new WpChangeStattusTrafficLawOutput
            {
                ID = entity.ID,
                IsPublish = entity.IsPublish.Value
            };
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Upload Pdf theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<WpUploadPdfOutput> HandleAsync(WpUploadPdfInput command)
        {
            if (command.PdfFile == null || command.PdfFile.Length == 0)
                throw Oops.Oh(BaseMsg.WP.Exception.InvalidFile);

            var extension = Path.GetExtension(command.PdfFile.FileName);

            if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
                throw Oops.Oh(BaseMsg.WP.Exception.OnlyPdfAllowed);

            var url = await _uploadService.UploadAsync(
                command.PdfFile,
                BaseUploadConfig.WP.Pdf,
                command.PdfFile.FileName
            );

            return new WpUploadPdfOutput
            {
                Url = url,
                FileName = command.PdfFile.FileName
            };
        }

    }
}
