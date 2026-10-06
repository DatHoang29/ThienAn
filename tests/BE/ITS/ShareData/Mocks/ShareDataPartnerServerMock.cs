using System.Net;
using System.Text;

namespace Tests.ShareData.Mocks;


/// <summary>
/// Description: Mock server HTTP độc lập giả lập máy chủ tiếp nhận của đối tác ShareData.
///              Mở HttpListener thật trên 127.0.0.1 (mặc định 18090-18093) để tầng gửi của sản phẩm
///              chạy nguyên vẹn qua dây mạng thật thay vì bị chặn ở tầng HttpMessageHandler.
/// Created date: 30/09/2026
/// </summary>
public sealed class ShareDataPartnerServerMock : IDisposable
{
    private HttpListener _listener = new();
    private CancellationTokenSource? _cts;
    private readonly object _gate = new();

    public const string DefaultHost = "127.0.0.1";
    public const int DefaultPort = 18090;
    public static readonly int[] DefaultPorts = [18090, 18091, 18092, 18093];

    /// <summary>Cổng KHÔNG ai lắng nghe — dùng để ép lỗi kết nối thật.</summary>
    public const int ClosedPort = 18099;

    public string BaseUrl => $"http://{DefaultHost}:{DefaultPort}/";

    // --- Kịch bản ---
    public HttpStatusCode DefaultStatusCode { get; set; } = HttpStatusCode.OK;
    public string? DefaultResponseBody { get; set; }

    /// <summary>
    /// Description: Quyết định phản hồi theo LƯỢT GỌI (1-based) và nội dung request.
    ///              Trả null để dùng DefaultStatusCode. Dùng cho kịch bản "hỏng ở trang thứ 3".
    /// Created date: 30/09/2026
    /// </summary>
    public Func<int, PartnerRequestRecord, PartnerResponsePlan?>? Responder { get; set; }

    /// <summary>Các lượt gọi (1-based) bị cắt kết nối giữa chừng, ép bên gửi ăn HttpRequestException.</summary>
    public HashSet<int> AbortOnCall { get; } = [];

    // --- Ghi nhận ---
    public List<PartnerRequestRecord> ReceivedRequests { get; } = [];
    public PartnerRequestRecord? LastRequest
    {
        get { lock (_gate) return ReceivedRequests.Count > 0 ? ReceivedRequests[^1] : null; }
    }
    public int TotalReceivedRequests
    {
        get { lock (_gate) return ReceivedRequests.Count; }
    }

    /// <summary>
    /// Description: Mở HttpListener trên các cổng chỉ định (mặc định 18090-18093).
    /// Created date: 30/09/2026
    /// </summary>
    public void Start(params int[] ports)
    {
        if (_listener.IsListening)
            return;

        var targetPorts = ports != null && ports.Length > 0 ? ports : DefaultPorts;
        _listener.Prefixes.Clear();
        foreach (var port in targetPorts)
            _listener.Prefixes.Add($"http://{DefaultHost}:{port}/");

        try
        {
            _listener.Start();
        }
        catch (HttpListenerException ex)
        {
            throw new InvalidOperationException(
                $"ShareDataPartnerServerMock không lắng nghe được trên [{string.Join(", ", targetPorts)}]. " +
                $"Có thể tiến trình testhost cũ đang chiếm cổng — chạy 'Get-Process testhost | Stop-Process -Force' rồi thử lại. Chi tiết: {ex.Message}", ex);
        }

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ListenLoop(_cts.Token));
    }

    /// <summary>
    /// Description: Đưa kịch bản và toàn bộ ghi nhận về mặc định. Gọi ở ĐẦU mỗi bài test có đổi kịch bản.
    /// Created date: 30/09/2026
    /// </summary>
    public void ResetDefaults()
    {
        lock (_gate)
        {
            DefaultStatusCode = HttpStatusCode.OK;
            DefaultResponseBody = null;
            Responder = null;
            AbortOnCall.Clear();
            ReceivedRequests.Clear();
        }
    }

    /// <summary>
    /// Description: Thiết lập mã phản hồi và thân phản hồi mặc định cho mock server (SV-4).
    /// Created date: 06/10/2026
    /// </summary>
    public void SetResponse(int port, int statusCode, string body)
    {
        lock (_gate)
        {
            DefaultStatusCode = (HttpStatusCode)statusCode;
            DefaultResponseBody = body;
        }
    }

    /// <summary>
    /// Description: Lấy danh sách request đã nhận được, có thể lọc theo cổng (SV-4).
    /// Created date: 06/10/2026
    /// </summary>
    public List<PartnerRequestRecord> GetReceivedRequests(int? port = null)
    {
        lock (_gate)
        {
            return port.HasValue
                ? ReceivedRequests.Where(r => r.Port == port.Value).ToList()
                : [.. ReceivedRequests];
        }
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequest(context);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested || !_listener.IsListening)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception)
            {
                // Tiếp tục phục vụ các request khác
            }
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;

        try
        {
            byte[] body = [];
            if (req.HasEntityBody)
            {
                using var buffer = new MemoryStream();
                await req.InputStream.CopyToAsync(buffer);
                body = buffer.ToArray();
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in req.Headers)
                headers[key] = req.Headers[key] ?? string.Empty;

            var record = new PartnerRequestRecord(
                req.Url?.Port ?? DefaultPort,
                req.HttpMethod,
                req.Url?.AbsolutePath ?? "/",
                req.RawUrl ?? "/",
                headers,
                body,
                req.ContentType);

            int callIndex;
            Func<int, PartnerRequestRecord, PartnerResponsePlan?>? responder;
            bool abort;
            HttpStatusCode defaultStatus;
            string? defaultBody;
            lock (_gate)
            {
                ReceivedRequests.Add(record);
                callIndex = ReceivedRequests.Count;
                responder = Responder;
                abort = AbortOnCall.Contains(callIndex);
                defaultStatus = DefaultStatusCode;
                defaultBody = DefaultResponseBody;
            }

            if (abort)
            {
                res.Abort();
                return;
            }

            var plan = responder?.Invoke(callIndex, record)
                       ?? new PartnerResponsePlan(defaultStatus, defaultBody);

            res.StatusCode = (int)plan.StatusCode;
            res.ContentType = plan.ContentType;
            var bytes = Encoding.UTF8.GetBytes(plan.Body ?? string.Empty);
            res.ContentLength64 = bytes.Length;
            if (bytes.Length > 0)
                await res.OutputStream.WriteAsync(bytes);
            res.Close();
        }
        catch (Exception)
        {
            try
            {
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
                res.Close();
            }
            catch { }
        }
    }

    private bool _disposed;

    /// <summary>
    /// Description: Giải phóng HttpListener để lần chạy sau không bị chiếm cổng.
    /// Created date: 30/09/2026
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _cts?.Cancel(); } catch { }
        try
        {
            if (_listener.IsListening)
                _listener.Stop();
            _listener.Prefixes.Clear();
            _listener.Close();
        }
        catch { }
        try { _cts?.Dispose(); } catch { }
    }

    #region Helper Types

    /// <summary>
    /// Description: Bản ghi một request đã nhận được — đủ để bài test soi method, đường dẫn, header và thân bản tin.
    /// Created date: 30/09/2026
    /// </summary>
    public sealed record PartnerRequestRecord(
        int Port,
        string Method,
        string Path,
        string RawUrl,
        IReadOnlyDictionary<string, string> Headers,
        byte[] BodyBytes,
        string? ContentType)
    {
        public string Body => Encoding.UTF8.GetString(BodyBytes);
    }

    /// <summary>
    /// Description: Phản hồi mà mock server sẽ trả về cho một lượt gọi.
    /// Created date: 30/09/2026
    /// </summary>
    public sealed record PartnerResponsePlan(
        HttpStatusCode StatusCode,
        string? Body = null,
        string ContentType = "application/json; charset=utf-8");

    #endregion
}
