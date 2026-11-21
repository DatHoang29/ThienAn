using Shared.Infrastructure.Services;
using Wolverine;
using Mapster;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNam;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNam.Huong;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoNam.Queries
{
    public class LLTNamQueryHandler
    {
        IMessageBus _messBus;
        UserManager _userManager;

        public LLTNamQueryHandler(IMessageBus messBus, UserManager userManager)
        {
            _messBus = messBus;
            _userManager = userManager;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTNamRenderInput command)
        {
            switch (command.Huong)
            {
                case "1":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNamDVRenderInput>());
                case "2":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNamDRRenderInput>());
                default:
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNamDRRenderInput>());
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(LLTNamExportInput command)
        {
            switch (command.Huong)
            {
                case "1":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNamDVExportInput>());
                case "2":
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNamDRExportInput>());
                default:
                    return await _messBus.InvokeAsync<FileStreamResult>(command.Adapt<LLTNamDRExportInput>());
            }
        }
    }
}
