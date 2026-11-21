using Furion;
using Modules.System.Controllers.Config.Dto;
using Modules.System.Core.Entities;
using Shared.DTO.Constants.Application;
using Shared.Utility.Utilities;
using Wolverine;

namespace Modules.VMS.Report.Core.Helpers
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Helper truy xuất Logo hệ thống cho báo cáo
    /// Created date: 07/10/2026
    /// </summary>
    internal class LogoHelper
    {
        /// <summary>
        /// Author: ChuongLP
        /// Description: Lấy URL logo của hệ thống
        /// Created date: 07/10/2026
        /// </summary>
        internal static string GetLogo()
        {
            var messBus = App.GetService<IMessageBus>();
            var config = messBus.InvokeAsync<List<SysConfig>>(new ListConfigInput() { Code = ConfigConst.SysWebLogo }).Result;
            var logo = config?.FirstOrDefault(x => x.Code == ConfigConst.SysWebLogo)?.Value;
            return BaseUtil.FileServer.GetPath() + "/" + logo;
        }
    }
}
