using Mapster;
using Modules.WP.Controllers.Config.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Core.Exceptions;
using Modules.WP.Infrastructure;
using Shared.Core.Interfaces;
using Shared.DTO.Constants.Application;
using Shared.DTO.Upload;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.WP.Controllers.Config.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các lệnh ghi (Update/Upload Image) cho cấu hình hệ thống WP
    /// Created date: 03/07/2026
    /// </summary>
    public  class ConfigCommandHandler:IWolverineHandler
    {
        private BaseRepository<WpConfig> _configRep;
        private BaseCacheService _cache;
        private UserManager _userManager;
        private IUploadService _uploadService;


        public ConfigCommandHandler(BaseRepository<WpConfig> configRep,BaseCacheService cache, UserManager userManager, IUploadService uploadService)
        {
            _configRep = configRep;
            _cache = cache;
            _userManager = userManager;
            _uploadService = uploadService;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 Config Update
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpConfigUpdateInput input)
        {
            if (input == null)
                return;
            var props = input.GetType().GetProperties();
            foreach (var p in props)
            {
                var name = p.Name;
                var value = p.GetValue(input)?.ToString();
                await _configRep
                    .AsUpdateable()
                    .SetColumns(x => new WpConfig { Value = value })
                    .Where(x => x.Code == name)
                    .ExecuteCommandAsync();
            }
            _cache.RemoveByPrefixKey(CacheConst.System.SysConfig);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Config Upload Img theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<WPConfigUploadImgOutput> HandleAsync(WPConfigUploadImgInput command)
        {
            if (command.File == null)
                throw Oops.Oh(BaseMsg.WP.Exception.InvalidFile);

            var url = await _uploadService.UploadAsync(
                command.File,
                BaseUploadConfig.WP.Logo,
                command.File.FileName
            );
            return new WPConfigUploadImgOutput
            {
                Url = url,
                Name = command.File.FileName
            };
        }
    }
}
