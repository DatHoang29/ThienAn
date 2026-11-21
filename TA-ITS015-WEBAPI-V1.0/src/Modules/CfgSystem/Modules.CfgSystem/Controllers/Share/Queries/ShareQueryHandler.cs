using Modules.CfgSystem.Controllers.Share.Services;
using Modules.CfgSystem.Core.Dto.OpConfig;
using Modules.CfgSystem.Core.Dto.Share;
using Shared.Utility.Helpers;
using System.Net;

namespace Modules.CfgSystem.Controllers.Share.Queries
{
    public class ShareQueryHandler : IWolverineHandler
    {

        IMessageBus _messBus;
        public ShareQueryHandler(IMessageBus messBus)
        {
            _messBus = messBus;
        }

        /// <summary>
        /// Get user paginated list 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(ShareInput input)
        {
            //TODO: enum
            var opConfig = await _messBus.InvokeAsync<List<SysOpConfigOutput>>(new SysOpConfigInput() { GroupCode = "operation_config" });

            var basePath = opConfig.FirstOrDefault(x => x.Code == "base_url_image")?.Value;
            var user = opConfig.FirstOrDefault(x => x.Code == "base_url_username")?.Value;
            var password = opConfig.FirstOrDefault(x => x.Code == "base_url_password")?.Value;
            var domain = opConfig.FirstOrDefault(x => x.Code == "base_url_domain")?.Value;

            string fullPath = Path.Combine(basePath, input.FilePath ?? "");

            // Nếu là UNC hoặc ổ mạng → dùng username/password
            if (IsNetworkPath(basePath) || IsMappedNetworkDrive(fullPath))
            {
                var credentials = new NetworkCredential(user, password);

                using (new NetworkConnection(basePath, credentials))
                {
                    return ReturnFile(fullPath);
                }
            }

            // Nếu là đường dẫn cục bộ → đọc bình thường
            return ReturnFile(fullPath);
        }


        private FileStreamResult ReturnFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();

            var contentType = BaseHelper.FileHp.FileMimeHelper.GetByExtension(ext);
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            return new FileStreamResult(stream, contentType);
        }



        private bool IsNetworkPath(string path)
        {
            return path.StartsWith(@"\\");
        }

        private bool IsMappedNetworkDrive(string path)
        {
            try
            {
                var root = Path.GetPathRoot(path);
                var drive = new DriveInfo(root);
                return drive.DriveType == DriveType.Network;
            }
            catch
            {
                return false;
            }
        }

    }
}
