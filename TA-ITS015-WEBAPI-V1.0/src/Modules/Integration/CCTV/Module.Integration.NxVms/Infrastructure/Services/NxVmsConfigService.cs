using Module.Integration.CCTV.Core.Abstracts;

namespace Module.Integration.CCTV.Infrastructure.Services
{
    /// <summary>
    /// Đọc cấu hình NxVMS qua INxVmsConfigProvider.
    /// Interface này được CCTV module implement và đăng ký vào DI —
    /// NxVms.Infrastructure không reference ngược lại CCTV.
    /// Author: ChuongLP
    /// Date: 2026-06-09
    /// </summary>
    public class NxVmsConfigService
    {
        private readonly INxVmsConfigProvider _provider;

        public NxVmsConfigService(INxVmsConfigProvider provider)
        {
            _provider = provider;
        }

        public Task<string> GetBaseUrlAsync() 
        {
            return _provider.GetBaseUrlAsync();
        } 


        public Task<string> GetUsernameAsync()
        {
            return _provider.GetUsernameAsync();
        }

        public Task<string> GetPasswordAsync()
        {
            return _provider.GetPasswordAsync();
        }

        public Task<(string BaseUrl, string Username, string Password)> GetLoginConfigAsync()
        {
            return _provider.GetLoginConfigAsync();
        }
    }
}