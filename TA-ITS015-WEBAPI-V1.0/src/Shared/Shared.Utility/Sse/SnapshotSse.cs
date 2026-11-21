using Furion;
using Microsoft.Extensions.Localization;


using Shared.Infrastructure.Sse;

namespace Shared.Utility.Sse;

/// <summary>
/// Author: KienLT
/// Description: Cửa vào SSE snapshot dùng chung cho các module. Mỗi kết nối vẫn có <see cref="SseResult"/> riêng, nhưng dữ liệu
/// đến từ MỘT vòng lấy dữ liệu dùng chung cho cả nhóm kết nối cùng phạm vi và cùng bộ lọc.
///
/// <para><b>Đổi từ 22/09/2026.</b> Trước đó mỗi kết nối chạy vòng poll riêng: mở 10 tab là 10 lần đọc mỗi
/// nhịp, và <c>SemaphoreSlim(4)</c> chỉ chặn số truy vấn đồng thời chứ không hợp nhất gì. Nay việc lấy dữ
/// liệu chuyển sang <see cref="SnapshotSseHub"/>; lớp này chỉ còn lo phần HTTP.</para>
///
/// <para><b>Vì sao vẫn cần lớp này.</b> <c>SseWriter</c> có constructor internal nên chỉ tồn tại trong
/// pipeline của action — không thể để hub tự ghi ra HTTP. Và localizer phải lấy Ở ĐÂY, nơi còn request scope,
/// vì hub là singleton nên không biết ngôn ngữ của từng kết nối.</para>
///
/// <para><b>Giới hạn phải biết:</b> việc này giảm số lần truy vấn BE/DB, KHÔNG giảm số kết nối SSE. Mỗi
/// client vẫn nhận dữ liệu qua đường mạng riêng. Muốn nhiều tab cùng trình duyệt chia một kết nối thì phải
/// làm ở FE (SharedWorker / BroadcastChannel).</para>
/// Created date: 08/09/2026
/// </summary>
public static class SnapshotSse
{
    /// <summary>
    /// Mở một luồng SSE lấy dữ liệu từ vòng dùng chung của nhóm.
    /// </summary>
    /// <param name="filterKey">
    /// Định danh BỘ LỌC của kết nối. Hai kết nối chỉ dùng chung một vòng khi chuỗi này giống nhau, nên nó
    /// phải chứa mọi tham số ảnh hưởng tới dữ liệu trả về. Khóa phạm vi (tenant, kiểu tài khoản) do hub tự
    /// thêm vào trước — người gọi không cần và không được tự ghép.
    /// </param>
    /// <param name="fetch">
    /// Lấy một snapshot bằng service của scope MỚI mà hub cấp. KHÔNG được bắt (capture) service của request
    /// trong closure này: vòng dùng chung sống lâu hơn request đã mở nó.
    /// </param>
    /// <param name="refresh">Chu kỳ lấy dữ liệu, tính bằng giây.</param>
    /// <param name="changeKey">
    /// Rút ra phần SỐ LIỆU để hub biết snapshot có đổi hay không. Phải bỏ <c>ComputedAt</c> và cửa sổ thời
    /// gian — chúng tính lại mỗi nhịp nên nếu so cả đối tượng thì "chỉ gửi khi đổi" không bao giờ đúng và
    /// heartbeat không bao giờ phát. Bỏ trống chỉ khi payload không có vỏ bọc thời gian.
    /// </param>
    public static SseResult Poll<T>(string filterKey, Func<IServiceProvider, CancellationToken, Task<T>> fetch,
        int refresh, Func<T, object?>? changeKey = null, string unavailableKey = "lz.exception.vds.sseUnavailable", string unauthorizedKey = "lz.exception.vds.sseScopeUnavailable")
        where T : notnull
    {
        if (refresh is < 1 or > 60) throw new ArgumentOutOfRangeException(nameof(refresh));

        // Chụp MỘT LẦN khi mở stream: bên trong vòng lặp thì request scope có thể đã đóng.
        // Bọc try/catch vì lớp này còn được gọi TRỰC TIẾP trong unit test, nơi không có host Furion.
        var localizer = Resolve(() => App.GetService<IStringLocalizer>());
        var unavailable = Message(localizer, unavailableKey);
        var unauthorized = Message(localizer, unauthorizedKey);
        var scope = SnapshotSseScope.Capture(Resolve(() => App.HttpContext));
        var hub = Resolve(() => App.GetService<SnapshotSseHub>());

        return SseConnection.Stream(async (writer, ct) =>
        {
            if (hub is null)
            {
                // Không có host (unit test) thì không có hub: chạy vòng tối giản để lớp này vẫn kiểm được.
                await PollWithoutHubAsync(writer, fetch, refresh, unavailable, ct);
                return;
            }

            await foreach (var payload in hub.SubscribeAsync(scope, filterKey, refresh,
                async (sp, token) => await fetch(sp, token),
                changeKey is null ? null : x => changeKey((T)x), ct))
            {
                if (ct.IsCancellationRequested) return;
                var frame = SnapshotSseSignals.ToFrame(payload, unavailable, unauthorized);
                await writer.WriteAsync(frame, ct);
                if (frame.IsLast) return;
            }
        });
    }

    /// <summary>
    /// Vòng dự phòng khi không có hub — chỉ xảy ra ngoài host, tức là trong unit test.
    ///
    /// <para>Giữ lại để test vẫn kiểm được hành vi ghi frame và hủy kết nối mà không phải dựng cả container.
    /// Không dùng ở môi trường chạy thật.</para>
    /// </summary>
    private static async Task PollWithoutHubAsync<T>(SseWriter writer,
        Func<IServiceProvider, CancellationToken, Task<T>> fetch, int refresh, string unavailable, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            T data;
            try { data = await fetch(EmptyProvider.Instance, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch
            {
                if (ct.IsCancellationRequested) return;
                await writer.WriteAsync(SseFrame.Fail(unavailable, 503).WithEvent("unavailable"), ct);
                await SseCancellation.DelayAsync(TimeSpan.FromSeconds(refresh), ct);
                continue;
            }
            if (ct.IsCancellationRequested) return;
            if (data is SseFrame frame)
            {
                await writer.WriteAsync(frame, ct);
                if (frame.IsLast) return;
            }
            else await writer.WriteAsync(data, ct);
            await SseCancellation.DelayAsync(TimeSpan.FromSeconds(refresh), ct);
        }
    }

    /// <summary>Câu thông báo đã dịch, hoặc chính khóa nếu chạy ngoài host.</summary>
    private static string Message(IStringLocalizer? localizer, string key)
        => Resolve(() => localizer?[key].Value) ?? key;

    /// <summary>Lấy dịch vụ từ container nếu có; trả <c>null</c> khi chạy ngoài host (unit test).</summary>
    private static T? Resolve<T>(Func<T?> get) where T : class
    {
        try { return get(); }
        catch { return null; }
    }

    /// <summary>Provider rỗng cho vòng dự phòng; mọi <c>GetService</c> trả null.</summary>
    private sealed class EmptyProvider : IServiceProvider
    {
        internal static readonly EmptyProvider Instance = new();
        public object? GetService(Type serviceType) => null;
    }
}
