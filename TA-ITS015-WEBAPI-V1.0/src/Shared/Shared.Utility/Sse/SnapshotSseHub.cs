using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace Shared.Utility.Sse;

/// <summary>
/// Author: KienLT
/// Description: Một vòng lấy dữ liệu dùng chung cho mỗi NHÓM kết nối SSE có cùng phạm vi dữ liệu và cùng bộ
/// lọc, thay cho mô hình mỗi kết nối một vòng poll riêng.
///
/// <para><b>Vấn đề đang giải.</b> Trước 22/09/2026 mỗi kết nối SSE chạy vòng poll và câu truy vấn riêng: mở
/// 10 tab là 10 lần đọc QuestDB mỗi nhịp. <c>SemaphoreSlim(4)</c> trong <c>SnapshotSse</c> chỉ chặn số truy vấn
/// chạy đồng thời, nó không hợp nhất gì cả.</para>
///
/// <para><b>Cái này giảm được gì và KHÔNG giảm được gì.</b> Giảm số lần truy vấn BE/DB xuống một lần cho mỗi
/// nhóm mỗi nhịp. KHÔNG giảm số kết nối SSE: mỗi client vẫn nhận dữ liệu qua đường mạng riêng. Muốn nhiều tab
/// của cùng trình duyệt dùng một kết nối thì cần cơ chế chia sẻ ở FE (SharedWorker hoặc BroadcastChannel),
/// không phải việc của BE.</para>
///
/// <para><b>Ranh giới phạm vi dữ liệu.</b> Khóa nhóm luôn bắt đầu bằng <see cref="SnapshotSseScope.DataScopeKey"/>
/// (tenant + kiểu tài khoản), nên hai kết nối khác tenant — hoặc một SuperAdmin và một người dùng thường —
/// KHÔNG bao giờ dùng chung một vòng. Vòng nào không tái lập được danh tính thì bị coi là lỗi và không chạy
/// truy vấn, xem <see cref="SnapshotSseScope.Restore"/>.</para>
///
/// <para><b>Client chậm không chặn client khác.</b> Mỗi kết nối có một channel dung lượng 1 với
/// <see cref="BoundedChannelFullMode.DropOldest"/>: client đọc chậm chỉ mất các snapshot trung gian và luôn
/// nhận bản mới nhất. Producer không bao giờ chờ subscriber.</para>
///
/// <para><b>Vòng đời (sửa 23/09/2026).</b> Ba lỗ hổng của bản đầu, cả ba đều làm client TREO chứ không báo
/// lỗi:</para>
/// <list type="number">
/// <item><see cref="Digest"/> và hàm <c>changeKey</c> nằm NGOÀI try/catch. Một selector cast sai kiểu hay một
/// payload không serialize được là đủ để task producer chết ở trạng thái faulted, trong khi
/// <c>group.Producer</c> vẫn khác null nên nhóm sống tiếp mà không ai lấy dữ liệu — mọi client của nhóm đó
/// ngồi im vô hạn, không có cả heartbeat. Nay lỗi so sánh KHÔNG chặn dữ liệu: coi như "không biết có đổi
/// không" và vẫn phát snapshot.</item>
/// <item>Producer dừng mà không HOÀN TẤT channel của subscriber. Rõ nhất ở nhánh không tái lập được danh
/// tính: client nhận một frame <c>unauthorized</c> rồi treo mãi vì stream chưa kết thúc. Nay mọi đường ra của
/// producer đều đi qua <see cref="StopAndComplete"/>.</item>
/// <item><see cref="CancellationTokenSource"/> của nhóm chỉ được <c>Cancel</c>, không bao giờ <c>Dispose</c>.
/// Nay producer — chủ sở hữu token — dispose ở cuối, sau khi đã thoát hẳn vòng lặp; phía <see cref="Leave"/>
/// chỉ hủy khi nhóm chưa dừng và làm việc đó TRONG lock để không đua với dispose.</item>
/// </list>
///
/// <para><b>Vì sao vẫn phải có <c>SseResult.Stream</c> cho từng kết nối.</b> <c>SseWriter</c> có constructor
/// internal nên không tự khởi tạo được; nó chỉ tồn tại trong pipeline của action. Hub chỉ là nguồn dữ liệu,
/// còn việc ghi ra HTTP vẫn thuộc từng kết nối.</para>
/// Created date: 22/09/2026
/// </summary>
public sealed class SnapshotSseHub(IServiceScopeFactory scopeFactory, ILogger<SnapshotSseHub> logger)
{
    /// <summary>Nhịp heartbeat: sau bấy nhiêu lần nhịp không có dữ liệu mới thì vẫn phát một frame.</summary>
    private const int HeartbeatEveryTicks = 6;

    /// <summary>Trần backoff khi nguồn dữ liệu lỗi liên tiếp, tính theo <see cref="TickUnit"/>.</summary>
    private const int MaxBackoffTicks = 60;

    /// <summary>
    /// Độ dài một đơn vị của tham số <c>refresh</c>. Mặc định là một giây — tức <c>refresh</c> đúng nghĩa
    /// "số giây" như tài liệu API mô tả.
    ///
    /// <para>Tồn tại để test vòng đời chạy được trong vài chục milli giây thay vì vài chục giây. KHÔNG đổi giá
    /// trị này ở môi trường chạy thật.</para>
    /// </summary>
    public TimeSpan TickUnit { get; init; } = TimeSpan.FromSeconds(1);

    private readonly ConcurrentDictionary<string, Group> _groups = new(StringComparer.Ordinal);

    /// <summary>
    /// Đăng ký một kết nối vào nhóm và nhận luồng snapshot của nhóm đó.
    ///
    /// <para>Nhóm được tạo cùng vòng lấy dữ liệu ở lần đăng ký đầu tiên, và chỉ dừng khi kết nối CUỐI CÙNG
    /// rời nhóm — hoặc khi chính vòng đó gặp lỗi không thể tiếp tục.</para>
    /// </summary>
    /// <param name="scope">Danh tính đã chụp trong request; quyết định phạm vi dữ liệu và khóa nhóm.</param>
    /// <param name="filterKey">Định danh bộ lọc — cùng bộ lọc mới được dùng chung một vòng.</param>
    /// <param name="refresh">Chu kỳ lấy dữ liệu, tính theo <see cref="TickUnit"/> (mặc định: giây).</param>
    /// <param name="fetch">Lấy một snapshot bằng service của scope MỚI do hub cấp, không phải scope request.</param>
    /// <param name="changeKey">
    /// Rút ra phần SỐ LIỆU dùng để biết snapshot có đổi hay không.
    ///
    /// <para><b>Bắt buộc bỏ vỏ bọc.</b> Snapshot của module mang <c>ComputedAt</c> và cửa sổ
    /// <c>From</c>/<c>To</c> tính lại mỗi nhịp, nên nếu so cả đối tượng thì nó LUÔN khác — "chỉ gửi khi đổi"
    /// không bao giờ đúng và heartbeat không bao giờ phát. Đã tái hiện 22/09/2026: số liệu giữ nguyên mà vẫn
    /// gửi 6/6 nhịp, 0 heartbeat.</para>
    ///
    /// <para><c>null</c> nghĩa là so cả đối tượng — chỉ dùng cho payload không có vỏ bọc thời gian.</para>
    /// <para>Selector NÉM LỖI không làm vòng chết: hub ghi log rồi phát snapshot như thể dữ liệu đã đổi.</para>
    /// </param>
    /// <param name="ct">Token của chính kết nối này; hủy nó chỉ rời nhóm, không dừng vòng của nhóm.</param>
    public async IAsyncEnumerable<object> SubscribeAsync(
        SnapshotSseScope scope,
        string filterKey,
        int refresh,
        Func<IServiceProvider, CancellationToken, Task<object>> fetch,
        Func<object, object?>? changeKey = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested) yield break;

        var key = $"{scope.DataScopeKey}|r={refresh}|{filterKey}";
        var reader = Join(key, scope, refresh, fetch, changeKey ?? (x => x), out var subscriber);

        try
        {
            // Disconnect completes only this subscriber's channel. Reading a completed channel ends
            // normally, so expected cancellation does not throw into the debugger.
            using var cancellation = ct.Register(static state =>
                ((Channel<object>)state!).Writer.TryComplete(), subscriber);
            await foreach (var item in reader.ReadAllAsync())
            {
                if (ct.IsCancellationRequested) yield break;
                yield return item;
            }
        }
        finally
        {
            Leave(key, subscriber);
        }
    }

    /// <summary>Số nhóm đang chạy — chỉ dùng cho kiểm chứng và chẩn đoán.</summary>
    public int ActiveGroupCount => _groups.Count;

    /// <summary>Số kết nối đang đăng ký ở một nhóm; <c>0</c> nếu nhóm không còn.</summary>
    public int SubscriberCount(string groupKey)
        => _groups.TryGetValue(groupKey, out var group) ? group.Count : 0;

    private ChannelReader<object> Join(string key, SnapshotSseScope scope, int refresh,
        Func<IServiceProvider, CancellationToken, Task<object>> fetch, Func<object, object?> changeKey,
        out Channel<object> subscriber)
    {
        // Dung lượng 1 + DropOldest: giữ đúng bản mới nhất, và producer không bao giờ bị subscriber chặn.
        var channel = Channel.CreateBounded<object>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
        subscriber = channel;

        // Vòng thử lại để không rơi vào cửa sổ đua với Leave/StopAndComplete: một kết nối có thể GetOrAdd đúng
        // nhóm mà phía kia vừa quyết định là dừng nhưng CHƯA kịp gỡ khỏi dictionary. Khi đó nhóm đã Retired và
        // producer sắp bị hủy, nên phải lấy nhóm khác thay vì đăng ký vào một nhóm chết.
        while (true)
        {
            var group = _groups.GetOrAdd(key, k => new Group(k));
            lock (group.Gate)
            {
                if (!group.Retired)
                {
                    group.Subscribers.Add(channel);
                    // Vòng chỉ khởi động ở kết nối ĐẦU TIÊN của nhóm.
                    group.Producer ??= StartProducer(group, scope, refresh, fetch, changeKey);

                    // Có bản mới nhất thì gửi ngay, để client vừa nối không phải chờ trọn một nhịp. Đọc trong
                    // lock để không lấy được Latest của một nhóm đang bị gỡ.
                    if (group.Latest is { } latest) channel.Writer.TryWrite(latest);
                    return channel.Reader;
                }
            }

            // Tự gỡ nhóm chết thay vì chờ phía kia làm: nếu chỉ `continue`, GetOrAdd sẽ trả lại đúng nhóm đó
            // và vòng này quay tại chỗ, đốt CPU cho tới khi bên kia kịp gỡ.
            _groups.TryRemove(new KeyValuePair<string, Group>(key, group));
            Thread.Yield();
        }
    }

    /// <summary>
    /// Khởi động vòng lấy dữ liệu của nhóm, CHẶN dòng chảy ExecutionContext.
    ///
    /// <para><b>Bắt buộc phải chặn.</b> <c>IHttpContextAccessor.HttpContext</c> nằm trong một
    /// <c>AsyncLocal</c> giữ REFERENCE tới một holder, và setter của nó gán <c>holder.Context = null</c> trên
    /// holder ĐANG CÓ trước khi tạo holder mới. Nếu producer thừa dòng chảy từ request đã mở nó thì nó dùng
    /// chung holder đó, nên lúc <c>SnapshotSseScope.Restore</c> gán HttpContext, <b>request cha mất HttpContext</b>
    /// — mất luôn cả filter tenant của chính request đó. Đã tái hiện 22/09/2026.</para>
    ///
    /// <para>KHÔNG được <c>await</c> bên trong vùng <c>SuppressFlow</c>: <c>AsyncFlowControl.Dispose()</c>
    /// phải chạy trên đúng context đã tạo nó, mà sau <c>await</c> thì có thể đã đổi thread. Ở đây chỉ TẠO
    /// task rồi thoát vùng ngay.</para>
    /// </summary>
    private Task StartProducer(Group group, SnapshotSseScope scope, int refresh,
        Func<IServiceProvider, CancellationToken, Task<object>> fetch, Func<object, object?> changeKey)
    {
        using (ExecutionContext.SuppressFlow())
        {
            return Task.Run(() => RunAsync(group, scope, refresh, fetch, changeKey));
        }
    }

    private void Leave(string key, Channel<object> subscriber)
    {
        // Hoàn tất channel của chính kết nối này trong MỌI trường hợp — kể cả khi nhóm đã bị gỡ trước đó.
        subscriber.Writer.TryComplete();

        if (!_groups.TryGetValue(key, out var group)) return;

        var last = false;
        lock (group.Gate)
        {
            group.Subscribers.Remove(subscriber);
            // Chỉ kết nối CUỐI CÙNG rời nhóm mới dừng vòng. Đánh dấu Retired NGAY TRONG LOCK để một kết nối
            // đang Join không thể đăng ký vào nhóm này nữa — đó là cửa sổ đua đã từng để lại stream không có
            // producer. Hủy token cũng làm trong lock: producer dispose token này ở cuối vòng đời của nó, và
            // Stopped chỉ được đặt trong cùng lock, nên ở đây không thể hủy một CTS đã dispose.
            if (group.Subscribers.Count == 0 && !group.Retired)
            {
                group.Retired = true;
                last = true;
                if (!group.Stopped) group.Cts.Cancel();
            }
        }

        if (!last) return;
        _groups.TryRemove(new KeyValuePair<string, Group>(key, group));
        logger.LogDebug("[SnapshotSseHub] Nhom {Key} khong con ket noi; da dung vong lay du lieu.", key);
    }

    /// <summary>
    /// Dừng nhóm hẳn: gỡ khỏi dictionary và HOÀN TẤT channel của mọi kết nối còn lại.
    ///
    /// <para>Hoàn tất channel là phần quan trọng: nó làm <c>await foreach</c> của từng kết nối kết thúc bình
    /// thường, response HTTP đóng, và client biết đường mà nối lại. Không hoàn tất thì client treo im lặng —
    /// tệ hơn một lỗi, vì không có gì để nhìn thấy.</para>
    ///
    /// <para>Chỉ producer gọi hàm này, và gọi ở <c>finally</c> nên mọi đường ra đều đi qua đây.</para>
    /// </summary>
    private void StopAndComplete(Group group, string reason)
    {
        Channel<object>[] targets;
        lock (group.Gate)
        {
            if (group.Stopped) return;
            group.Stopped = true;
            group.Retired = true;
            targets = [.. group.Subscribers];
            group.Subscribers.Clear();
            // Producer là chủ sở hữu token và vừa thoát khỏi vòng lặp, nên đây là chỗ duy nhất được dispose.
            group.Cts.Dispose();
        }

        _groups.TryRemove(new KeyValuePair<string, Group>(group.Key, group));
        foreach (var target in targets) target.Writer.TryComplete();
        logger.LogDebug("[SnapshotSseHub] Nhom {Key} dung: {Reason}; da dong {Count} ket noi.",
            group.Key, reason, targets.Length);
    }

    private async Task RunAsync(Group group, SnapshotSseScope scope, int refresh,
        Func<IServiceProvider, CancellationToken, Task<object>> fetch, Func<object, object?> changeKey)
    {
        var ct = group.Cts.Token;
        var reason = "huy";
        try
        {
            reason = await PumpAsync(group, scope, refresh, fetch, changeKey, ct);
        }
        catch (Exception ex)
        {
            // Không để task chết ở trạng thái faulted mà không ai biết: log rồi vẫn đi qua finally để đóng
            // kết nối. Trước 23/09/2026 đường này để client treo vô hạn.
            reason = "loi khong mong doi";
            logger.LogError(ex, "[SnapshotSseHub] Nhom {Key} dung vi loi khong mong doi.", group.Key);
        }
        finally
        {
            StopAndComplete(group, reason);
        }
    }

    /// <summary>Vòng lấy dữ liệu; trả về lý do dừng để ghi log.</summary>
    private async Task<string> PumpAsync(Group group, SnapshotSseScope scope, int refresh,
        Func<IServiceProvider, CancellationToken, Task<object>> fetch, Func<object, object?> changeKey,
        CancellationToken ct)
    {
        var failures = 0;
        var quietTicks = 0;

        while (!ct.IsCancellationRequested)
        {
            object payload;
            try
            {
                using var serviceScope = scopeFactory.CreateScope();
                try
                {
                    // PHẢI tái lập danh tính trước lần chạm DB đầu tiên của async-context này, nếu không filter
                    // tenant sẽ không được gắn và truy vấn vượt phạm vi tenant mà không báo lỗi.
                    if (!scope.Restore(serviceScope.ServiceProvider))
                    {
                        logger.LogError("[SnapshotSseHub] Nhom {Key} khong tai lap duoc danh tinh; dung vong de khong "
                            + "truy van vuot pham vi tenant.", group.Key);
                        Broadcast(group, SnapshotSseSignals.Unauthorized);
                        return "khong tai lap duoc danh tinh";
                    }

                    // fetch khai trả object không null; trả null là vi phạm hợp đồng nên coi như nhịp lỗi thay
                    // vì đẩy null xuống tận chỗ ghi frame.
                    if (ct.IsCancellationRequested) return "huy";

                    payload = await fetch(serviceScope.ServiceProvider, ct)
                        ?? throw new InvalidOperationException("Ham fetch cua SSE tra ve null.");
                    failures = 0;
                }
                finally
                {
                    // Bỏ HttpContext giả TRƯỚC khi scope bị dispose, nếu không async-context của producer còn
                    // giữ một context mà RequestServices đã đóng.
                    SnapshotSseScope.Clear(serviceScope.ServiceProvider);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return "huy"; }
            catch (Exception ex)
            {
                failures = Math.Min(failures + 1, 6);
                logger.LogWarning(ex, "[SnapshotSseHub] Nhom {Key}: nhip doc that bai lan thu {Failures}.", group.Key, failures);
                Broadcast(group, SnapshotSseSignals.Unavailable);
                await DelayAsync(Math.Min(MaxBackoffTicks, refresh * Math.Pow(2, failures)), ct);
                continue;
            }

            // Chỉ phát khi SỐ LIỆU đổi — so trên phần đã bỏ vỏ bọc, vì ComputedAt và cửa sổ thời gian tính
            // lại mỗi nhịp nên so cả đối tượng thì lúc nào cũng "đổi". Không đổi thì vẫn phát heartbeat theo
            // chu kỳ để client và proxy biết kết nối còn sống.
            var digest = SafeDigest(group, payload, changeKey);
            if (digest is null || digest != group.LastDigest)
            {
                group.LastDigest = digest;
                lock (group.Gate) group.Latest = payload;
                quietTicks = 0;
                Broadcast(group, payload);
            }
            else if (++quietTicks >= HeartbeatEveryTicks)
            {
                quietTicks = 0;
                Broadcast(group, SnapshotSseSignals.Heartbeat);
            }

            await DelayAsync(refresh, ct);
        }
        return "huy";
    }

    private Task DelayAsync(double ticks, CancellationToken ct)
        => SseCancellation.DelayAsync(TickUnit * ticks, ct);

    /// <summary>Đẩy một giá trị cho mọi kết nối trong nhóm; không chờ ai đọc.</summary>
    private static void Broadcast(Group group, object payload)
    {
        Channel<object>[] targets;
        lock (group.Gate) targets = [.. group.Subscribers];
        foreach (var target in targets) target.Writer.TryWrite(payload);
    }

    /// <summary>
    /// Dấu vân tay nội dung, hoặc <c>null</c> khi KHÔNG tính được.
    ///
    /// <para><c>null</c> nghĩa là "không biết có đổi hay không", và người gọi coi như đã đổi để vẫn phát dữ
    /// liệu. Chọn hướng này vì lỗi ở đây là lỗi của selector hoặc của bộ serialize — dữ liệu vừa lấy về vẫn
    /// tốt, không có lý gì vì một phép so sánh mà giữ client không có dữ liệu. Cái mất duy nhất là tối ưu
    /// "chỉ gửi khi đổi" và heartbeat, nên log ở mức cảnh báo và chỉ log MỘT lần cho mỗi nhóm để không ngập
    /// log mỗi nhịp.</para>
    /// </summary>
    private string? SafeDigest(Group group, object payload, Func<object, object?> changeKey)
    {
        try
        {
            return Digest(changeKey(payload));
        }
        catch (Exception ex)
        {
            if (!group.DigestFaultLogged)
            {
                group.DigestFaultLogged = true;
                logger.LogWarning(ex, "[SnapshotSseHub] Nhom {Key}: khong so sanh duoc snapshot nen phat moi nhip; "
                    + "heartbeat tam thoi khong hoat dong cho nhom nay.", group.Key);
            }
            return null;
        }
    }

    private static string Digest(object? payload)
    {
        if (payload is null) return "";
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private sealed class Group(string key)
    {
        public string Key { get; } = key;
        public object Gate { get; } = new();
        public List<Channel<object>> Subscribers { get; } = new();
        public CancellationTokenSource Cts { get; } = new();
        public Task? Producer { get; set; }
        public string? LastDigest { get; set; }
        public object? Latest { get; set; }

        /// <summary>Đã quyết định dừng; không nhận thêm subscriber. Chỉ đọc/ghi trong <see cref="Gate"/>.</summary>
        public bool Retired { get; set; }

        /// <summary>Producer đã thoát và đã dispose <see cref="Cts"/>. Chỉ đọc/ghi trong <see cref="Gate"/>.</summary>
        public bool Stopped { get; set; }

        /// <summary>Đã ghi log lỗi so sánh snapshot; dùng để log đúng một lần cho mỗi nhóm.</summary>
        public bool DigestFaultLogged { get; set; }

        public int Count { get { lock (Gate) return Subscribers.Count; } }
    }
}
