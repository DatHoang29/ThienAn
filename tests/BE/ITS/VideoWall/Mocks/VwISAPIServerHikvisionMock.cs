using System.Net;
using System.Text;

namespace Tests.VideoWall.Mocks
{
    /// <summary>
    /// Author: Đạt
    /// Description: Mock Server HTTP độc lập giả lập thiết bị Hikvision Video Wall Controller DS-C66S-H88-CL.
    ///              Mở Web Server HTTP cục bộ trên localhost (mặc định port 18080), hỗ trợ Digest Authentication (RFC 7616)
    ///              và trả về toàn bộ các mẫu XML & JSON đo thật (100% Ground Truth từ API_Postman_Videowall.md).
    /// Created date: 17/08/2026
    /// </summary>
    public partial class VwISAPIServerHikvisionMock : IDisposable
    {
        private const string Ns = "http://www.isapi.org/ver20/XMLSchema";
        private HttpListener _listener = new();
        private CancellationTokenSource? _cts;
        private Task? _listenTask;

        public const string DefaultHost = "127.0.0.1";
        public const int DefaultPort = 18080;
        public static readonly int[] DefaultPorts = [18080, 18081, 18082, 18083];
        public const string DefaultUser = "admin";
        public const string DefaultPassword = "Password123!";
        public const string Realm = "DS-C66S-H88-CL";

        public string BaseUrl => $"http://{DefaultHost}:{DefaultPort}/";

        /// <summary>
        /// Author: Đạt
        /// Description: Khởi động HttpListener lắng nghe trên các port (mặc định 18080, 18081, 18082, 18083)
        /// Created date: 17/08/2026
        /// </summary>
        public void Start(params int[] ports)
        {
            if (_listener.IsListening)
                return;

            var targetPorts = ports != null && ports.Length > 0 ? ports : DefaultPorts;

            var started = false;
            try
            {
                _listener.Prefixes.Clear();
                foreach (var port in targetPorts)
                {
                    _listener.Prefixes.Add($"http://*:{port}/");
                }
                _listener.Start();
                started = true;
            }
            catch (HttpListenerException)
            {
                try
                {
                    _listener.Close();
                }
                catch
                {
                }
                _listener = new HttpListener();
            }

            if (!started)
            {
                _listener.Prefixes.Clear();
                foreach (var port in targetPorts)
                {
                    _listener.Prefixes.Add($"http://{DefaultHost}:{port}/");
                }

                try
                {
                    _listener.Start();
                }
                catch (HttpListenerException ex)
                {
                    throw new InvalidOperationException(
                        $"MockServer không thể lắng nghe trên các port [{string.Join(", ", targetPorts)}]. " +
                        $"Có thể tiến trình testhost cũ đang chạy ngầm chiếm cổng. Hãy chạy 'Stop-Process -Name testhost -Force' để giải phóng. Chi tiết: {ex.Message}", ex);
                }
            }

            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Vòng lặp nhận HTTP connection từ Web Server cục bộ và điều phối xử lý request bất đồng bộ
        /// Created date: 17/08/2026
        /// </summary>
        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
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
                    // Tiếp tục vòng lặp cho các request khác
                }
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Xử lý request ISAPI theo đường dẫn URL, phương thức HTTP và các cờ kịch bản giả lập
        /// Created date: 17/08/2026
        /// </summary>
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            try
            {
                var reqPort = req.Url?.Port ?? DefaultPort;
                var path = req.Url?.AbsolutePath ?? "/";
                var method = req.HttpMethod;
                var rawUrl = req.RawUrl ?? path;
                ReceivedRequests.Add($"[{reqPort}] {method} {rawUrl}");

                // Chụp body THÔ dạng byte trước khi bất kỳ route nào đọc InputStream. Phải là byte
                // chứ không phải string: chốt "không có BOM" chỉ kiểm được ở mức byte, mà BOM lọt
                // vào là thiết bị thật trả badXmlFormat (mục G.2 tài liệu 09B).
                if (req.HasEntityBody && !path.Contains("/transData", StringComparison.OrdinalIgnoreCase))
                {
                    using var bodyBuffer = new MemoryStream();
                    await req.InputStream.CopyToAsync(bodyBuffer);
                    LastReceivedBodyBytes = bodyBuffer.ToArray();
                    LastReceivedBody = Encoding.UTF8.GetString(LastReceivedBodyBytes);
                    LastReceivedContentType = req.ContentType;
                }

                TotalReceivedRequests++;

                // ─── 0.00. Giả lập lỗi tạm thời (503 Service Unavailable / 408 / 500) phục vụ kiểm thử retry ───
                if (TransientErrorCount > 0 && TransientErrorCalls < TransientErrorCount)
                {
                    TransientErrorCalls++;
                    await WriteXmlResponseAsync(res, TransientStatusCode, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>4</statusCode>
                          <statusString>Temporary Error</statusString>
                          <subStatusCode>serviceUnavailable</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // ─── 0.0. Giả lập thiết bị không phản hồi / rớt mạng / timeout ───
                if (SimulateUnreachable || SimulateUnreachablePorts.Contains(reqPort))
                {
                    context.Response.Abort();
                    return;
                }

                // ─── 0. Giả lập lỗi trên từng Controller/Port cụ thể ───
                if (SimulateFailurePorts.Contains(reqPort))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.InternalServerError, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>4</statusCode>
                          <statusString>Invalid Operation</statusString>
                          <subStatusCode>devicePortFailure</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // ─── 1. Giả lập Khóa IP do sai mật khẩu liên tiếp (§A.2) ───
                if (IsLockedOut || (FailedAuthLockoutThreshold > 0 && ConsecutiveFailedAuthCount >= FailedAuthLockoutThreshold))
                {
                    IsLockedOut = true;
                    await WriteXmlResponseAsync(res, HttpStatusCode.Forbidden, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>3</statusCode>
                          <statusString>Device Locked</statusString>
                          <subStatusCode>ipAddressLocked</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // M1: /SDK/activateStatus — liveness, KHÔNG yêu cầu digest (KB-01 #1)
                if (method == "GET" && path.TrimStart('/').Equals("SDK/activateStatus", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
                        <Activated xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
                          <Activated>true</Activated>
                        </Activated>
                        """);
                    return;
                }

                // ─── 2. Xử lý Digest Authentication & Nonce Expiry (stale="true") ───
                if (RequireDigestAuth && !HandleDigestAuth(req, res, method, path))
                    return;

                // ─── 2. Giả lập các loại lỗi ISAPI chuẩn theo Mục G trong tài liệu ───
                if (SimulateMalformedXmlResponse)
                {
                    res.StatusCode = (int)HttpStatusCode.OK;
                    res.ContentType = "application/xml; charset=utf-8";
                    var bytes = Encoding.UTF8.GetBytes("INVALID_XML_PAYLOAD_UNCLOSED_TAG_<ResponseStatus");
                    res.ContentLength64 = bytes.Length;
                    await res.OutputStream.WriteAsync(bytes);
                    res.Close();
                    return;
                }

                if (SimulateMethodNotAllowed)
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>4</statusCode>
                          <statusString>Invalid Operation</statusString>
                          <subStatusCode>methodNotAllowed</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                if (SimulateBadXmlFormat)
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>5</statusCode>
                          <statusString>Invalid XML Format</statusString>
                          <subStatusCode>badXmlFormat</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                if (SimulateBadParameters)
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>6</statusCode>
                          <statusString>Invalid XML Content</statusString>
                          <subStatusCode>badParameters</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                if (SimulateInvalidOperation || SimulateDeviceFailure)
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>4</statusCode>
                          <statusString>Invalid Operation</statusString>
                          <subStatusCode>invalidOperation</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // ─── 3. Router xử lý từng Endpoint ISAPI chuẩn theo Template Matcher ───
                if (await DispatchIsapiRouteAsync(context, method, path))
                    return;

                // H. Default ResponseStatus OK cho các API ghi khác
                await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <ResponseStatus version="1.0" xmlns="{{Ns}}">
                      <requestURL>{{path}}</requestURL>
                      <statusCode>1</statusCode>
                      <statusString>OK</statusString>
                      <subStatusCode>ok</subStatusCode>
                    </ResponseStatus>
                    """);
            }
            catch (Exception)
            {
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
                res.Close();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi dữ liệu XML UTF-8 vào OutputStream phản hồi của HttpListenerResponse
        /// Created date: 17/08/2026
        /// </summary>
        private static async Task WriteXmlResponseAsync(HttpListenerResponse res, HttpStatusCode status, string xml)
        {
            res.StatusCode = (int)status;
            res.ContentType = "application/xml; charset=utf-8";
            var bytes = Encoding.UTF8.GetBytes(xml);
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
            res.Close();
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi dữ liệu JSON UTF-8 vào OutputStream phản hồi của HttpListenerResponse
        /// Created date: 17/08/2026
        /// </summary>
        private static async Task WriteJsonResponseAsync(HttpListenerResponse res, HttpStatusCode status, string json)
        {
            res.StatusCode = (int)status;
            res.ContentType = "application/json; charset=utf-8";
            var bytes = Encoding.UTF8.GetBytes(json);
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
            res.Close();
        }

        private static async Task WriteBinaryResponseAsync(HttpListenerResponse res, HttpStatusCode status, string contentType, byte[] data)
        {
            res.StatusCode = (int)status;
            res.ContentType = contentType;
            res.ContentLength64 = data.Length;
            await res.OutputStream.WriteAsync(data);
            res.Close();
        }

        private bool _disposed;

        /// <summary>
        /// Author: Đạt
        /// Description: Giải phóng Web Server cục bộ HttpListener và tài nguyên bất đồng bộ của MockServer
        /// Created date: 17/08/2026
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _cts?.Cancel();
            }
            catch { }

            try
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
                _listener.Prefixes.Clear();
                _listener.Close();
            }
            catch { }

            try
            {
                _cts?.Dispose();
            }
            catch { }

            GC.SuppressFinalize(this);
        }
    }
}
