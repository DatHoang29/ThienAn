using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Integration.CCTV.Core.Abstracts
{
    public interface INxVmsConfigProvider
    {
        Task<string> GetBaseUrlAsync();
        Task<string> GetUsernameAsync();
        Task<string> GetPasswordAsync();

        // Thêm: lấy cả 3 trong 1 lần query
        Task<(string BaseUrl, string Username, string Password)> GetLoginConfigAsync();
    }
}
