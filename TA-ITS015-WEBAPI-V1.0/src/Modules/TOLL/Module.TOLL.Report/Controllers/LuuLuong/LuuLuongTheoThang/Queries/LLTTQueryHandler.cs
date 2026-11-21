using Shared.Infrastructure.Services;
using Wolverine;
using Mapster;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoThang;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoThang.Huong;


namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoThang.Queries
{
    public class LLTTQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        UserManager _userManager;

        public LLTTQueryHandler(IMessageBus messBus, UserManager userManager)
        {
            _messBus = messBus;
            _userManager = userManager;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTTRenderInput command)
        {
            switch (command.Huong)
            {
                case "1":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTTDVRenderInput>());
                case "2":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTTDRRenderInput>());
                default:
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTTDRRenderInput>());
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTTExportInput command)
        {
            switch (command.Huong)
            {
                case "1":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTTDVExportInput>());
                case "2":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTTDRExportInput>());
                default:
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTTDRExportInput>());
            }
        }
    }
}
