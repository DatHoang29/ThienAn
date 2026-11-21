using Microsoft.Extensions.DependencyInjection;
using Module.CCTV.Infrastructure.Services.Device;
using Module.Integration.CCTV.Core.Abstracts;
using Module.Integration.CCTV.Infrastructure.Services;

namespace Module.CCTV.Infrastructure.Services.Vendor
{
    public class VendorService : IScoped
    {
        private readonly IServiceProvider _sp;
        private readonly DeviceService _config;

        public VendorService(IServiceProvider sp, DeviceService config)
        {
            _sp = sp;
            _config = config;
        }

        /// <summary>
        /// Lấy BaseCctvService phù hợp.
        /// vendorCode null → đọc từ SysOpConfig key "cctv_vendor".
        /// Author: ChuongLP
        /// Date: 2024-06-10
        /// </summary>
        public async Task<BaseCctvService> GetAsync(string? vendorCode = null)
        {
            var vendor = vendorCode ?? await _config.GetValueAsync("cctv_vendor");

            return vendor.Trim().ToLower() switch
            {
                "nxvms" => _sp.GetRequiredService<NxVmsService>(), // Khi truyền "nxvms" thì sẽ lấy instance của NxVmsService từ DI container.
                // Thêm vendor mới tại đây:
                // "hikvision" => _sp.GetRequiredService<HikvisionService>(),
                // "milestone"  => _sp.GetRequiredService<MilestoneService>(),
                _ => throw Oops.Oh($"[{vendor}] chưa được hỗ trợ. " + $"Kiểm tra lại cấu hình cctv_vendor trong SysOpConfig.")
            };
        }
    }
}
