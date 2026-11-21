using Mapster;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay.Huong;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoNgay.Queries
{
    public class LLTNQueryHandler : IWolverineHandler
    {
        IMessageBus _messBus;
        private const string reportCode = "2.1.1.3";
        private UserManager _userManager;

        public LLTNQueryHandler(IMessageBus messBus, UserManager userManager)
        {
            _messBus = messBus;
            _userManager = userManager;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTNRenderInput command)
        {
            switch (command.Huong)
            {
                case "1":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNDauVaoRenderInput>());
                case "2":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNDauRaRenderInput>());
                default:
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNDauRaRenderInput>());
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTNExportInput command)
        {
            switch (command.Huong)
            {
                case "1":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNDauVaoExportInput>());
                case "2":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNDauRaExportInput>());
                default:
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNDauRaExportInput>());
            }
        }
    }
}
