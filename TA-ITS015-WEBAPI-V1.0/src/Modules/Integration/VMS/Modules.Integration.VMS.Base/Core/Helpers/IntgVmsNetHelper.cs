using System.Net.Sockets;

namespace Modules.Integration.VMS.Base.Core.Helpers
{
    /// <summary>
    /// Kiểm tra kết nối tới thiết bị bằng TCP.
    ///
    /// Vì sao KHÔNG dùng BaseHelper.NetworkHp của Shared.Utility: project đó tham chiếu Shared.Reference
    /// nên kéo theo cả Furion + SqlSugar. Tầng Integration cần chạy được ở CẢ 2 nơi — trong WebAPI (có Furion)
    /// và trong dịch vụ ITS.VMS.Worker (không có Furion) — nên phải tự lo phần ping.
    /// Đặt ở tầng Base vì Base chỉ tham chiếu Shared.DTO.
    ///
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public static class IntgVmsNetHelper
    {
        /// <summary>Số ms chờ mặc định khi mở kết nối.</summary>
        public const int DefaultTimeoutMs = 2000;

        /// <summary>
        /// Bản ĐỒNG BỘ dùng cho IntgVmsManager.InitConfig (hàm này đồng bộ nên không await được).
        /// Không ném exception: sai IP / không mở port / quá hạn đều trả false.
        /// </summary>
        public static bool CheckConnect(string? ip, int port, int timeoutMs = DefaultTimeoutMs)
        {
            try
            {
                return CheckConnectAsync(ip, port, timeoutMs).GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Bản ASYNC dùng cho lệnh check-connect của dịch vụ.
        /// Không ném exception: sai IP / không mở port / quá hạn đều trả false.
        /// </summary>
        public static async Task<bool> CheckConnectAsync(string? ip, int port, int timeoutMs = DefaultTimeoutMs,
                                                        CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ip) || port <= 0) return false;

            try
            {
                using var client = new TcpClient();
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(timeoutMs);

                await client.ConnectAsync(ip.Trim(), port, timeoutCts.Token).ConfigureAwait(false);
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
