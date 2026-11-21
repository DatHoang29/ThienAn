using Mapster;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoCa;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoCa.Huong;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoCa.Queries
{
    public class LLTCQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private UserManager _userManager;

        public LLTCQueryHandler(IMessageBus messBus, UserManager userManager)
        {
            _messBus = messBus;
            _userManager = userManager;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTCRenderInput command)
        {
            switch (command.Huong)
            {
                case "1":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTCDVRenderInput>());
                case "2":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTCDRRenderInput>());
                default:
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTCDRRenderInput>());
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTCExportInput command)
        {
            switch (command.Huong)
            {
                case "1":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTCDVExportInput>());
                case "2":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTCDRExportInput>());
                default:
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTCDRExportInput>());
            }
        }
    }
}
