using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Base.Enums;

namespace Modules.Integration.Delivery.Senders
{
    /// <summary>
    /// Kênh vận chuyển xuất bản dữ liệu qua giao thức HTTP REST tới đối tác.
    /// Created date: 16/09/2026
    /// Modified date: 19/10/2026 (Gỡ IDeliverySender)
    /// </summary>
    public class HttpDeliverySender(IHttpClientFactory? httpClientFactory = null, ILogger<HttpDeliverySender>? logger = null)
    {
        public ConnectionType ConnectType => ConnectionType.Api;

        /// <summary>
        /// Description: Thực thi gửi gói tin qua HTTP REST (HandleConnection nhánh API).
        ///              Hỗ trợ timeout cấu hình qua HttpTimeoutSec và các header xác thực trong SecurityInfo.
        /// Modified date: 13/11/2026
        /// </summary>
        public virtual async Task<DeliveryResult> Send(DeliveryRequest request, DeliveryContext ctx, CancellationToken ct)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(ctx.PartnerCode) && string.IsNullOrWhiteSpace(ctx.Address))
            {
                return new DeliveryResult { Success = false, Channel = "API", ErrorCode = "NoPartner", ErrorMessage = "Đăng ký không có đối tác.", Warnings = warnings };
            }

            if (string.IsNullOrWhiteSpace(request.JsonPayload))
            {
                return new DeliveryResult { Success = true, Channel = "API", SentByteSize = 0, Warnings = warnings };
            }

            if (!TryBuildEndpointUri(request, ctx, out var endpointUri, out var urlError) || endpointUri == null)
            {
                var errCode = (urlError != null && urlError.Contains("Port", StringComparison.OrdinalIgnoreCase))
                    ? "InvalidConfiguration"
                    : "InvalidUri";
                logger?.LogWarning("❌ [Gửi] {Message}", urlError);
                return new DeliveryResult { Success = false, Channel = "API", ErrorCode = errCode, ErrorMessage = urlError, Warnings = warnings };
            }

            var httpUrl = endpointUri.ToString();
            var payloadBytes = request.PayloadBytes ?? Encoding.UTF8.GetBytes(request.JsonPayload);
            var timeoutSec = 30;

            var reader = new ConnectConfigReader(request.ConnectConfig);

            var timeoutStr = reader.GetString(ConnectKeys.TimeoutSeconds, ConnectKeys.TimeoutSecondsAliases);
            if (!string.IsNullOrWhiteSpace(timeoutStr))
            {
                if (!int.TryParse(timeoutStr, out var parsedTimeout) || parsedTimeout <= 0)
                {
                    var errMsg = $"Cấu hình Timeout [{timeoutStr}] không hợp lệ (phải là số nguyên dương).";
                    logger?.LogWarning("❌ [Gửi] {Message}", errMsg);
                    return new DeliveryResult
                    {
                        Success = false,
                        Channel = "API",
                        ErrorCode = "InvalidConfiguration",
                        ErrorMessage = errMsg,
                        Warnings = warnings
                    };
                }
                timeoutSec = parsedTimeout;
            }

            var bypassSsl = false;
            var bypassStr = reader.GetString(ConnectKeys.BypassSsl, ConnectKeys.BypassSslAliases);
            if (!string.IsNullOrWhiteSpace(bypassStr))
            {
                if (!bool.TryParse(bypassStr, out var parsedBypass))
                {
                    var errMsg = $"Cấu hình BypassSsl [{bypassStr}] không hợp lệ (phải là boolean 'true' hoặc 'false').";
                    logger?.LogWarning("❌ [Gửi] {Message}", errMsg);
                    return new DeliveryResult
                    {
                        Success = false,
                        Channel = "API",
                        ErrorCode = "InvalidConfiguration",
                        ErrorMessage = errMsg,
                        Warnings = warnings
                    };
                }
                bypassSsl = parsedBypass;
            }

            try
            {
                var contentType = !string.IsNullOrWhiteSpace(request.ContentType) ? request.ContentType : "application/json";
                using var content = new ByteArrayContent(payloadBytes);
                var mediaType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
                if (string.Equals(mediaType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(mediaType.CharSet))
                {
                    mediaType.CharSet = "utf-8";
                }
                content.Headers.ContentType = mediaType;
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, httpUrl) { Content = content };
                AddIdentityHeaders(httpRequest, ctx);

                // Thêm các header xác thực theo chuẩn cấu hình FE / SecurityInfo
                var authType = reader.GetRawString(ConnectKeys.AuthType);
                var token = reader.GetString(ConnectKeys.Token)
                    ?? (request.SecurityInfo != null && request.SecurityInfo.TryGetValue(ConnectKeys.Token, out var secTok) ? secTok : null);
                var apiKey = reader.GetString(ConnectKeys.ApiKey)
                    ?? (request.SecurityInfo != null && request.SecurityInfo.TryGetValue(ConnectKeys.ApiKey, out var secKey) ? secKey : null);
                var user = reader.GetRawString(ConnectKeys.Username, defaultValue: "", aliases: ConnectKeys.UsernameAliases);
                var pass = reader.GetRawString(ConnectKeys.Password, defaultValue: "", aliases: ConnectKeys.PasswordAliases);

                var hasAuthData = !string.IsNullOrWhiteSpace(token)
                    || !string.IsNullOrWhiteSpace(apiKey)
                    || !string.IsNullOrWhiteSpace(user)
                    || !string.IsNullOrWhiteSpace(pass);

                if (string.IsNullOrWhiteSpace(authType) || string.Equals(authType, AuthTypes.None, StringComparison.OrdinalIgnoreCase))
                {
                    if (hasAuthData)
                    {
                        warnings.Add("Cấu hình phương thức xác thực rỗng hoặc None nhưng tồn tại thông tin xác thực (Token/ApiKey/Username/Password); gửi không kèm xác thực.");
                    }
                }
                else if (!AuthTypes.IsSupported(authType))
                {
                    warnings.Add($"Phương thức xác thực AuthType [{authType}] không được hỗ trợ.");
                }
                else if (string.Equals(authType, AuthTypes.Bearer, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        warnings.Add("Phương thức xác thực Bearer yêu cầu Token nhưng cấu hình bị thiếu hoặc rỗng.");
                    }
                    else
                    {
                        var tokenValue = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token : $"Bearer {token}";
                        httpRequest.Headers.TryAddWithoutValidation("Authorization", tokenValue);
                    }
                }
                else if (string.Equals(authType, AuthTypes.ApiKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        warnings.Add("Phương thức xác thực ApiKey yêu cầu ApiKey nhưng cấu hình bị thiếu hoặc rỗng.");
                    }
                    else
                    {
                        var headerName = reader.GetString(ConnectKeys.HeaderName, ConnectKeys.HeaderNameAliases) ?? "X-Api-Key";
                        httpRequest.Headers.TryAddWithoutValidation(headerName, apiKey);
                    }
                }
                else if (string.Equals(authType, AuthTypes.Basic, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                    {
                        warnings.Add("Phương thức xác thực Basic yêu cầu Username và Password nhưng cấu hình bị thiếu hoặc rỗng.");
                    }
                    else
                    {
                        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
                        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Basic {basic}");
                    }
                }

                LogFullRequest(httpRequest, request.JsonPayload);

                using var httpClient = bypassSsl
                    ? new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    })
                    : (httpClientFactory?.CreateClient() ?? new HttpClient());

                httpClient.Timeout = TimeSpan.FromSeconds(timeoutSec);

                using var httpResponse = await httpClient.SendAsync(httpRequest, ct);

                var responseBody = await httpResponse.Content.ReadAsStringAsync(ct);
                logger?.LogInformation("⬅️ [Gửi] Đối tác trả về {StatusCode}: {ResponseBody}",
                    (int)httpResponse.StatusCode, responseBody);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var statusCode = (int)httpResponse.StatusCode;
                    var errMsg = $"Gửi HTTP tới đối tác [{ctx.PartnerCode}] tại [{httpUrl}] thất bại: {statusCode}. Body: {responseBody}";
                    var detailJson = JsonSerializer.Serialize(new
                    {
                        statusCode,
                        httpUrl,
                        responseBody
                    });
                    logger?.LogWarning("❌ [Gửi] {Message}", errMsg);
                    return new DeliveryResult
                    {
                        Success = false,
                        Channel = "API",
                        StatusCode = statusCode,
                        SentByteSize = payloadBytes.Length,
                        TargetAddressOrPath = httpUrl,
                        ErrorCode = $"Http_{statusCode}",
                        ErrorMessage = errMsg,
                        DetailJson = detailJson,
                        Warnings = warnings
                    };
                }

                if (TryDetectBusinessError(request, responseBody, out var businessErrorMessage))
                {
                    var errMsg = $"Gửi HTTP tới đối tác [{ctx.PartnerCode}] tại [{httpUrl}] trả về HTTP 200 nhưng báo lỗi nghiệp vụ: {businessErrorMessage}";
                    var detailJson = JsonSerializer.Serialize(new
                    {
                        statusCode = (int)httpResponse.StatusCode,
                        httpUrl,
                        responseBody,
                        businessError = businessErrorMessage
                    });
                    logger?.LogWarning("❌ [Gửi] {Message}", errMsg);
                    return new DeliveryResult
                    {
                        Success = false,
                        Channel = "API",
                        StatusCode = (int)httpResponse.StatusCode,
                        SentByteSize = payloadBytes.Length,
                        TargetAddressOrPath = httpUrl,
                        ErrorCode = "BusinessError",
                        ErrorMessage = errMsg,
                        DetailJson = detailJson,
                        Warnings = warnings
                    };
                }

                return new DeliveryResult
                {
                    Success = true,
                    Channel = "API",
                    StatusCode = (int)httpResponse.StatusCode,
                    SentByteSize = payloadBytes.Length,
                    TargetAddressOrPath = httpUrl,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                var detailedError = FormatDetailedHttpException(ex, httpUrl, timeoutSec);
                var detailJson = JsonSerializer.Serialize(new
                {
                    httpUrl,
                    exception = ex.Message,
                    detailedError,
                    innerException = ex.InnerException?.Message
                });
                logger?.LogError(ex, "❌ [Gửi] Lỗi gửi HTTP tới [{Url}]: {Message} ({DetailedError})", httpUrl, ex.Message, detailedError);
                var isPossiblyDelivered = CouldHaveReachedPartner(ex);
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "API",
                    SentByteSize = payloadBytes.Length,
                    TargetAddressOrPath = httpUrl,
                    ErrorCode = "HttpException",
                    ErrorMessage = detailedError,
                    DetailJson = detailJson,
                    IsPossiblyDelivered = isPossiblyDelivered,
                    Warnings = warnings
                };
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đổi ngoại lệ HTTP thành câu lỗi tiếng Việt có gợi ý xử lý, phân biệt lỗi chứng chỉ, bị từ chối kết nối, máy chủ đóng kết nối ngang, hết thời gian chờ, không phân giải được tên miền.
        /// Created date: 16/09/2026
        /// </summary>
        private static string FormatDetailedHttpException(Exception ex, string httpUrl, int timeoutSec)
        {
            var current = ex;
            var allMessages = new List<string>();
            Exception? specificEx = null;

            while (current != null)
            {
                if (!string.IsNullOrWhiteSpace(current.Message) && !allMessages.Contains(current.Message))
                {
                    allMessages.Add(current.Message);
                }

                if (current is System.Security.Authentication.AuthenticationException or
                    System.Net.Sockets.SocketException or
                    System.TimeoutException or
                    TaskCanceledException)
                {
                    specificEx ??= current;
                }

                current = current.InnerException;
            }

            var fullText = string.Join(" -> ", allMessages);

            if (specificEx is System.Security.Authentication.AuthenticationException ||
                fullText.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("TLS", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("hostname mismatch", StringComparison.OrdinalIgnoreCase))
            {
                return $"Lỗi xác thực chứng chỉ SSL/TLS khi kết nối tới [{httpUrl}]: Địa chỉ IP hoặc tên miền không khớp với chứng chỉ máy chủ đối tác (hoặc chứng chỉ không hợp lệ/tự ký). Chi tiết: {fullText}. (Gợi ý: Kiểm tra lại tên miền máy chủ đối tác hoặc bật tùy chọn 'BypassSsl': true nếu là môi trường nội bộ/staging).";
            }

            if (specificEx is System.Net.Sockets.SocketException sockEx && sockEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused ||
                fullText.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("actively refused", StringComparison.OrdinalIgnoreCase))
            {
                return $"Không thể kết nối tới [{httpUrl}]: Kết nối bị từ chối (Connection refused). Vui lòng kiểm tra lại địa chỉ IP, cổng kết nối (Port), hoặc dịch vụ máy chủ đối tác đã được khởi chạy hay chưa.";
            }

            if ((specificEx is System.Net.Sockets.SocketException sockResetEx && (sockResetEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset || sockResetEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionAborted)) ||
                fullText.Contains("Connection reset", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("response ended prematurely", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("forcibly closed by the remote host", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("The socket has been shut down", StringComparison.OrdinalIgnoreCase))
            {
                return $"Máy chủ đối tác đóng kết nối ngang tại [{httpUrl}] — thường do gọi http:// vào cổng chỉ nhận https:// (hoặc ngược lại) hoặc sai cổng. Chi tiết: {fullText}";
            }

            if (specificEx is TaskCanceledException or System.TimeoutException ||
                (specificEx is System.Net.Sockets.SocketException sEx && (sEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut || sEx.SocketErrorCode == System.Net.Sockets.SocketError.HostUnreachable || sEx.SocketErrorCode == System.Net.Sockets.SocketError.NetworkUnreachable)) ||
                fullText.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return $"Hết thời gian chờ phản hồi (Timeout sau {timeoutSec}s) khi kết nối tới [{httpUrl}]. Vui lòng kiểm tra lại đường truyền mạng, firewall hoặc kiểm tra máy chủ đối tác có đang phản hồi không.";
            }

            if (specificEx is System.Net.Sockets.SocketException dnsEx && (dnsEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound || dnsEx.SocketErrorCode == System.Net.Sockets.SocketError.NoData) ||
                fullText.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
                fullText.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase))
            {
                return $"Không thể phân giải tên miền (DNS) cho địa chỉ [{httpUrl}]. Vui lòng kiểm tra lại cấu hình tên miền của đối tác.";
            }

            return $"Lỗi kết nối/gửi HTTP tới [{httpUrl}]: {fullText}";
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Gói có thể đã tới đối tác dù bên gửi không nhận được trả lời: đứt kết nối giữa chừng, hết giờ chờ hoặc bị hủy khi đang chờ.
        ///              Không phân giải được tên miền, không mở được kết nối, lỗi bắt tay TLS hay proxy thì gói chưa rời bên gửi;
        ///              lỗi không phải do mạng (cấu hình sai khi dựng yêu cầu) cũng chưa gửi gì.
        /// Created date: 13/11/2026
        /// </summary>
        private static bool CouldHaveReachedPartner(Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return true;
            }

            if (ex is not HttpRequestException)
            {
                return false;
            }

            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is HttpRequestException
                    {
                        HttpRequestError: HttpRequestError.NameResolutionError
                            or HttpRequestError.ConnectionError
                            or HttpRequestError.SecureConnectionError
                            or HttpRequestError.ProxyTunnelError
                    })
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Thêm header định danh PartnerCode, PacketCode, SerialNbr, ProcessedAt; bên nhận đọc các header này để biết gói của đối tác nào.
        /// Created date: 16/09/2026
        /// </summary>
        private static void AddIdentityHeaders(HttpRequestMessage request, DeliveryContext ctx)
        {
            if (!string.IsNullOrEmpty(ctx.PartnerCode))
                request.Headers.Add("PartnerCode", ctx.PartnerCode);

            if (!string.IsNullOrEmpty(ctx.PacketCode))
                request.Headers.Add("PacketCode", ctx.PacketCode);

            if (ctx.SerialNbr != null)
                request.Headers.Add("SerialNbr", ctx.SerialNbr.Value.ToString(CultureInfo.InvariantCulture));

            if (ctx.ExportedAt != default)
                request.Headers.Add("ProcessedAt", ctx.ExportedAt.ToString("O", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ở mức Debug ghi phương thức, URL, tên header và toàn bộ dữ liệu gửi đi. Chỉ ghi tên header, không ghi giá trị để không lộ thông tin xác thực.
        /// Created date: 16/09/2026
        /// </summary>
        private void LogFullRequest(HttpRequestMessage request, string bodyJson)
        {
            if (logger is null || !logger.IsEnabled(LogLevel.Debug))
                return;

            var headerNames = string.Join(", ", request.Headers.Select(h => h.Key));

            logger.LogDebug(
                "➡️ [Gửi] Request đẩy lên đối tác:\n{Method} {Url}\nHeaders: {HeaderNames}\n\n{Body}",
                request.Method, request.RequestUri, headerNames, bodyJson);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lấy endpoint, địa chỉ, cổng từ cấu hình đối tác và ngữ cảnh gửi rồi ghép URL. Khóa EndPointApiUrl, Endpoint trong cấu hình được ưu tiên hơn cột của đối tác.
        /// Created date: 16/09/2026
        /// </summary>
        public static bool TryBuildEndpointUri(DeliveryRequest request, DeliveryContext ctx, out Uri? uri, out string? error)
        {
            var reader = new ConnectConfigReader(request.ConnectConfig);
            var endpoint = reader.GetString("EndPointApiUrl", "Endpoint") ?? ctx.EndPointApiUrl?.Trim();
            var address = ctx.Address?.Trim();
            var port = ctx.Port;
            var partnerCode = ctx.PartnerCode ?? reader.GetString("PartnerCode") ?? "Unknown";

            return TryBuildEndpointUri(address, port, endpoint, partnerCode, out uri, out error);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghép URL gọi đối tác. Endpoint đã là URL tuyệt đối thì dùng thẳng, không thì ghép từ Address, Port, endpoint; Address không ghi giao thức thì mặc định http.
        ///              Cấu hình sai thì trả false kèm câu lỗi, không ném ngoại lệ.
        /// Created date: 16/09/2026
        /// </summary>
        public static bool TryBuildEndpointUri(string? address, int? port, string? endpoint, string? partnerCode, out Uri? uri, out string? error)
        {
            uri = null;
            error = null;

            address = address?.Trim();
            endpoint = endpoint?.Trim();
            var pCode = partnerCode ?? "Unknown";

            // 1. Nếu endpoint đã là URL tuyệt đối (http/https) -> dùng thẳng, bỏ qua Address/Port
            if (!string.IsNullOrEmpty(endpoint)
                && Uri.TryCreate(endpoint, UriKind.Absolute, out var absolute)
                && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            {
                uri = absolute;
                return true;
            }

            if (string.IsNullOrEmpty(address))
            {
                error = $"Đối tác [{pCode}] chưa cấu hình địa chỉ (Address) và EndPointApiUrl cũng không phải URL tuyệt đối.";
                return false;
            }

            if (port.HasValue && (port.Value < 1 || port.Value > 65535))
            {
                error = $"Cấu hình Port [{port.Value}] không hợp lệ (phải là số nguyên từ 1 đến 65535).";
                return false;
            }

            // 2. Tách giao thức nếu Address đã ghi sẵn
            var scheme = Uri.UriSchemeHttp;
            var host = address;
            if (address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                scheme = Uri.UriSchemeHttps;
                host = address["https://".Length..];
            }
            else if (address.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                host = address["http://".Length..];
            }

            host = host.TrimEnd('/');
            if (string.IsNullOrEmpty(host))
            {
                error = $"Đối tác [{pCode}] có địa chỉ không hợp lệ: '{address}'.";
                return false;
            }

            // 3. Ghép cổng: chỉ thêm khi Address/host chưa tự mang cổng
            var baseUrl = $"{scheme}://{host}";
            if (port.HasValue && !host.Contains(':'))
                baseUrl = $"{baseUrl}:{port.Value}";

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                error = $"Đối tác [{pCode}] có địa chỉ không hợp lệ: '{baseUrl}'.";
                return false;
            }

            // 4. Ghép đường dẫn bằng Uri (tự xử lý dấu '/')
            if (string.IsNullOrEmpty(endpoint))
            {
                uri = baseUri;
                return true;
            }

            if (!Uri.TryCreate(baseUri, endpoint, out var combined))
            {
                error = $"Đối tác [{pCode}] có endpoint không hợp lệ: '{baseUrl}' + '{endpoint}'.";
                return false;
            }

            uri = combined;
            return true;
        }

        /// <summary>
        /// Description: Nhận biết đối tác báo lỗi nghiệp vụ trong thân phản hồi JSON dù mã HTTP 2xx.
        /// Modified date: 20/10/2026
        /// </summary>
        private static bool TryDetectBusinessError(DeliveryRequest request, string responseBody, out string? errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(responseBody))
                return false;

            var reader = new ConnectConfigReader(request.ConnectConfig);
            var successField = reader.GetString(ConnectKeys.SuccessField);
            var contract = reader.GetString(ConnectKeys.ResponseContract);
            var hasSuccessField = !string.IsNullOrWhiteSpace(successField);
            var isAutoContract = string.IsNullOrWhiteSpace(contract) ||
                string.Equals(contract, "Auto", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(contract, "Furion", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(contract, "UnifyResult", StringComparison.OrdinalIgnoreCase);

            if (!hasSuccessField && !isAutoContract)
                return false;

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return false;

                if (hasSuccessField)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, successField, StringComparison.OrdinalIgnoreCase))
                        {
                            var isFalse = prop.Value.ValueKind == JsonValueKind.False;
                            var isErrCode = prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var code) && code != 200 && code != 0;
                            var isErrStr = false;
                            if (prop.Value.ValueKind == JsonValueKind.String)
                            {
                                var strVal = prop.Value.GetString()?.Trim().ToLowerInvariant();
                                isErrStr = strVal is "false" or "failed" or "error" or "fail";
                            }

                            if (isFalse || isErrCode || isErrStr)
                            {
                                string? detailErr = null;
                                var emf = reader.GetString(ConnectKeys.ErrorMessageField);
                                if (!string.IsNullOrWhiteSpace(emf) &&
                                    doc.RootElement.TryGetProperty(emf, out var mProp))
                                {
                                    detailErr = mProp.ToString();
                                }
                                else if (doc.RootElement.TryGetProperty("message", out var m1)) detailErr = m1.ToString();
                                else if (doc.RootElement.TryGetProperty("msg", out var m2)) detailErr = m2.ToString();
                                else if (doc.RootElement.TryGetProperty("errors", out var m3)) detailErr = m3.ToString();

                                string? detailCode = null;
                                var ecf = reader.GetString(ConnectKeys.ErrorCodeField);
                                if (!string.IsNullOrWhiteSpace(ecf) &&
                                    doc.RootElement.TryGetProperty(ecf, out var cProp))
                                {
                                    detailCode = cProp.ToString();
                                }

                                if (!string.IsNullOrWhiteSpace(detailErr))
                                {
                                    errorMessage = !string.IsNullOrWhiteSpace(detailCode) ? $"[{detailCode}] {detailErr}" : detailErr;
                                }
                                else if (isErrCode)
                                {
                                    errorMessage = $"Trường mã trạng thái '{prop.Name}' mang giá trị lỗi {prop.Value.GetRawText()}.";
                                }
                                else
                                {
                                    errorMessage = $"Trường '{prop.Name}' mang giá trị '{prop.Value.GetRawText()}'.";
                                }
                                return true;
                            }
                        }
                    }
                }

                if (isAutoContract)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, "succeeded", StringComparison.OrdinalIgnoreCase) &&
                            prop.Value.ValueKind == JsonValueKind.False)
                        {
                            string? detailErr = null;
                            var emf = reader.GetString(ConnectKeys.ErrorMessageField);
                            if (!string.IsNullOrWhiteSpace(emf) &&
                                doc.RootElement.TryGetProperty(emf, out var mProp))
                            {
                                detailErr = mProp.ToString();
                            }
                            if (string.IsNullOrWhiteSpace(detailErr) && doc.RootElement.TryGetProperty("message", out var m1))
                            {
                                detailErr = m1.ToString();
                            }
                            if (string.IsNullOrWhiteSpace(detailErr) && doc.RootElement.TryGetProperty("errors", out var m2))
                            {
                                detailErr = m2.ToString();
                            }
                            if (string.IsNullOrWhiteSpace(detailErr) && doc.RootElement.TryGetProperty("msg", out var m3))
                            {
                                detailErr = m3.ToString();
                            }
                            if (string.IsNullOrWhiteSpace(detailErr))
                            {
                                detailErr = "Thao tác không thành công";
                            }

                            string? detailCode = null;
                            var ecf = reader.GetString(ConnectKeys.ErrorCodeField);
                            if (!string.IsNullOrWhiteSpace(ecf) &&
                                doc.RootElement.TryGetProperty(ecf, out var cProp))
                            {
                                detailCode = cProp.ToString();
                            }
                            if (string.IsNullOrWhiteSpace(detailCode) && doc.RootElement.TryGetProperty("code", out var c1))
                            {
                                detailCode = c1.ToString();
                            }
                            if (string.IsNullOrWhiteSpace(detailCode) && doc.RootElement.TryGetProperty("statusCode", out var c2))
                            {
                                detailCode = c2.ToString();
                            }

                            errorMessage = !string.IsNullOrWhiteSpace(detailCode) ? $"[{detailCode}] {detailErr}" : detailErr;
                            return true;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Phản hồi không phải JSON -> bỏ qua kiểm tra lỗi nghiệp vụ trong thân phản hồi
            }

            return false;
        }
    }
}
