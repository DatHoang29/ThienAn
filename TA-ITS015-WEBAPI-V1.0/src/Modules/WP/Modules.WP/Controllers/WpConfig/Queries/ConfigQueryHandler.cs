
using Modules.WP.Controllers.Config.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Infrastructure;
using Shared.Infrastructure.Services;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.WP.Controllers.Config.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn đọc cấu hình hệ thống WP (Config)
    /// Created date: 03/07/2026
    /// </summary>
    public class ConfigQueryHandler
    {
        private BaseRepository<WpConfig> _configRep;
        private BaseCacheService _cache;

        public ConfigQueryHandler(BaseRepository<WpConfig> configRep, BaseCacheService cache)
        {
            _configRep = configRep;
            _cache = cache;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Config Info theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<WpConfigInfoOutput> HandleAsync(WpConfigInfoInput command)
        {
            var list = await _configRep
                .AsQueryable()
                .Where(x => x.SysFlag == StatusEnum.Enable)
                .ToListAsync();

            var info = new WpConfigInfoOutput();
            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item.Code))
                    continue;

                var code = item.Code.Trim();
                var value = item.Value;

                switch (code)
                {
                    case "sysWebTitle":
                        info.sysWebTitle = value;
                        
                        break;
                    case "sysWebAddress":
                        info.sysWebAddress = value;
                        break;
                    case "sysWebDescription":
                        info.sysWebDescription = value;
                        break;
                    case "sysWebTelephone":
                        info.sysWebTelephone = value;
                        break;
                    case "sysWebCopyright":
                        info.sysWebCopyright = value;
                        break;
                    case "sysWebLogo":
                        info.sysWebLogo = value;
                        break;
                    case "sysWebRepresent":
                        info.sysWebRepresent = value;
                        break;
                    case "sysWebEmail":
                        info.sysWebEmail = value;
                        break;
                    case "sysWebImg1":
                        info.sysWebImg1 = value;
                        break;
                    case "sysWebImg2":
                        info.sysWebImg2 = value;
                        break;
                    case "sysWebImg3":
                        info.sysWebImg3 = value;
                        break;
                    case "sysWebImgTitle1":
                        info.sysWebImgTitle1 = value;
                        break;
                    case "sysWebImgTitle2":
                        info.sysWebImgTitle2 = value;
                        break;
                    case "sysWebImgTitle3":
                        info.sysWebImgTitle3 = value;
                        break;
                    case "sysWebOffice":
                        info.sysWebOffice = value;
                        break;
                    case "sysOfficeAddress":
                        info.sysOfficeAddress = value;
                        break;
                    case "sysOfficeTelephone":
                        info.sysOfficeTelephone = value;
                        break;
                    case "sysBusinessLicense":
                        info.sysBusinessLicense = value;
                        break;
                    case "webCamSum":
                        info.webCamSum = value;
                        break;
                    case "webTotalLength":
                        info.webTotalLength = value;
                        break;
                    case "webTotalToll":
                        info.webTotalToll = value;
                        break;
                    case "webTotalWork":
                        info.webTotalWork = value;
                        break;
                    // Ô truy cập nhanh (quick tiles) trang chủ WP
                    case "webTile1Label":
                        info.webTile1Label = value;
                        break;
                    case "webTile1Path":
                        info.webTile1Path = value;
                        break;
                    case "webTile1Icon":
                        info.webTile1Icon = value;
                        break;
                    case "webTile2Label":
                        info.webTile2Label = value;
                        break;
                    case "webTile2Path":
                        info.webTile2Path = value;
                        break;
                    case "webTile2Icon":
                        info.webTile2Icon = value;
                        break;
                    case "webTile3Label":
                        info.webTile3Label = value;
                        break;
                    case "webTile3Path":
                        info.webTile3Path = value;
                        break;
                    case "webTile3Icon":
                        info.webTile3Icon = value;
                        break;
                    case "webTile4Label":
                        info.webTile4Label = value;
                        break;
                    case "webTile4Path":
                        info.webTile4Path = value;
                        break;
                    case "webTile4Icon":
                        info.webTile4Icon = value;
                        break;
                    // Nút ở phần hero trang chủ WP
                    case "webHeroBtn1Label":
                        info.webHeroBtn1Label = value;
                        break;
                    case "webHeroBtn1Path":
                        info.webHeroBtn1Path = value;
                        break;
                    case "webHeroBtn2Label":
                        info.webHeroBtn2Label = value;
                        break;
                    case "webHeroBtn2Path":
                        info.webHeroBtn2Path = value;
                        break;
                    // Màu giao diện web portal
                    case "webThemePrimary":
                        info.webThemePrimary = value;
                        break;
                    case "webThemeAccent":
                        info.webThemeAccent = value;
                        break;
                }
            }
            return info;
        }
    }
}