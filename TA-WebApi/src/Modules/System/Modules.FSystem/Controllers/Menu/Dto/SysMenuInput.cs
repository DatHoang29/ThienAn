using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapster;
using Modules.System.Controllers.Menu.Dto;
using Modules.System.Core.Entities;

namespace Modules.FSystem.Controllers.Menu.Dto
{
    public class SysMenuMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config = TypeAdapterConfig.GlobalSettings;
            config.ForType<SysMenu, MenuOutput>()
                .Map(t => t.Meta.Title, o => o.Title)
                .Map(t => t.Meta.Icon, o => o.Icon)
                .Map(t => t.Meta.IsIframe, o => o.IsIframe)
                .Map(t => t.Meta.IsLink, o => o.OutLink)
                .Map(t => t.Meta.IsHide, o => o.IsHide)
                .Map(t => t.Meta.IsKeepAlive, o => o.IsKeepAlive)
                .Map(t => t.Meta.IsFullScreen, o => o.IsFullscreen ?? false)
                .Map(t => t.Meta.IsAffix, o => o.IsAffix);
        }
    }
}
