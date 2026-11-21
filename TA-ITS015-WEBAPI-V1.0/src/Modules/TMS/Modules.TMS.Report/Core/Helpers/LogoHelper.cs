using Furion;
using Modules.System.Controllers.Config.Dto;
using Modules.System.Core.Entities;
using Shared.DTO.Constants.Application;
using Shared.Utility.Utilities;
using Wolverine;

namespace Modules.TMS.Report.Core.Helpers
{
    internal class LogoHelper
    {
        /// <summary>
        /// Lấy logo của trang web
        /// </summary>
        /// <returns></returns>
        internal static string GetLogo()
        {
            var _messBus = App.GetService<IMessageBus>();
            var config =  _messBus.InvokeAsync<List<SysConfig>>(new ListConfigInput() { Code = ConfigConst.SysWebLogo }).Result;
            var logo = config?.FirstOrDefault(x => x.Code == ConfigConst.SysWebLogo)?.Value;
            return BaseUtil.FileServer.GetPath() +"/" +logo;

        }
    }
}
