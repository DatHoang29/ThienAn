using Microsoft.Extensions.Logging;
using Wolverine;

namespace Modules.TMS.Extensions
{
    /// <summary>
    /// Author: KienLT
    /// Description: Gọi handler VDS in-process qua message bus mà vẫn giữ hai bảo đảm của đường Refit cũ.
    ///
    /// <para><b>Giới hạn thời gian.</b> Refit có timeout 10s/30s; <c>InvokeAsync</c> chạy local thì không (tham số
    /// timeout của Wolverine không áp cho đường local). Một QuestDB không với tới được có thể treo vài phút ở lần
    /// probe đầu, nên chờ tối đa <c>timeout</c> rồi báo lỗi — truy vấn nền vẫn chạy tiếp và tự kết thúc.</para>
    ///
    /// <para><b>Không lộ lỗi nội bộ.</b> Trước đây mọi lỗi phía VDS (400/500/503) bị gói thành một thông báo chung.
    /// Giữ nguyên: lỗi nghiệp vụ đã bản địa hoá (<see cref="AppFriendlyException"/>) đi tiếp, còn lại (SQL, QuestDB,
    /// validator, timeout) ghi log rồi ném <c>Oops.Oh(errorKey)</c>.</para>
    /// Created date: 13/10/2026
    /// </summary>
    public static class VdsMessageBusExtensions
    {
        public static async Task<T> InvokeVdsAsync<T>(this IMessageBus bus, object request, TimeSpan timeout, string errorKey)
            where T : class
        {
            T? result;
            try
            {
                result = await bus.InvokeAsync<T>(request).WaitAsync(timeout);
            }
            catch (AppFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                App.GetService<ILoggerFactory>()?.CreateLogger(typeof(VdsMessageBusExtensions))
                    .LogError(ex, "Gọi VDS in-process thất bại: {Request}", request.GetType().Name);
                throw Oops.Oh(errorKey);
            }

            return result ?? throw Oops.Oh(errorKey);
        }
    }
}
