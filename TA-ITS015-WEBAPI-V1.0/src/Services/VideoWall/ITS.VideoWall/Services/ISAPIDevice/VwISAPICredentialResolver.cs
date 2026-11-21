using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Options;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    /// <summary>
    /// Nguồn tài khoản Digest gửi xuống thiết bị.
    /// Ưu tiên Account/PassWord từ bản ghi VwController, fallback sang cấu hình chung
    /// VideoWall:Device khi bản ghi chưa khai báo.
    /// Khi cấu hình "VideoWall:Device:IsLocal" = true: cưỡng chế ghi đè tài khoản từ cấu hình.
    ///
    /// PHẢI giữ ISingleton: IHttpClientFactory dựng chuỗi handler trong scope riêng và cache lại
    /// khoảng 2 phút, nên VwISAPIDigestHandler không được phụ thuộc bất cứ gì theo request.
    /// </summary>
    public class VwISAPICredentialResolver(
        IOptions<VwDeviceOptions> options,
        IConfiguration? configuration = null) 
    {
        private readonly IOptions<VwDeviceOptions> _options = options;
        private readonly IConfiguration? _configuration = configuration;

        public (string? Account, string? Password) Resolve(VwController? controller)
        {
            var device = _options?.Value?.Device ?? new VwDeviceConnectionOptions();
            var isLocal = _configuration != null && string.Equals(_configuration["VideoWall:Device:IsLocal"], "true", StringComparison.OrdinalIgnoreCase);

            if (isLocal)
            {
                var localAccount = !string.IsNullOrWhiteSpace(device.Account) ? device.Account : controller?.Account;
                var localPassword = !string.IsNullOrWhiteSpace(device.Password) ? device.Password : controller?.PassWord;
                return (localAccount, localPassword);
            }

            if (controller == null)
                return (device.Account, device.Password);

            var account = string.IsNullOrWhiteSpace(controller.Account) ? device.Account : controller.Account;
            var password = string.IsNullOrWhiteSpace(controller.PassWord) ? device.Password : controller.PassWord;

            return (account, password);
        }

        public bool HasCredential(VwController? controller)
        {
            var (account, password) = Resolve(controller);
            return !string.IsNullOrWhiteSpace(account) && !string.IsNullOrWhiteSpace(password);
        }
    }
}
