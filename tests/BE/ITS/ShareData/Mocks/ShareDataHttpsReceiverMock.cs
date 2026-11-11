using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Tests.ShareData.Mocks;

/// <summary>
/// Author: DatHQ
/// Description: Cổng HTTPS thật trên 127.0.0.1, chứng chỉ tự ký cấp cho tên miền khác địa chỉ gọi, giống WebAPI staging
///              (chứng chỉ cấp cho tên miền, Worker gọi bằng IP). Mỗi yêu cầu được chuyển nguyên phương thức, đường dẫn, header
///              và thân vào WebAPI chạy trong tiến trình test, nên đi đúng controller nhận thật; ghi lại đường dẫn và mã đối tác đã nhận.
/// Created date: 11/11/2026
/// </summary>
public sealed class ShareDataHttpsReceiverMock : IAsyncDisposable
{
    /// <summary>Tên miền ghi trên chứng chỉ; Worker gọi bằng IP nên tên không khớp.</summary>
    public const string CertificateHost = "sharedata-receiver.invalid";

    /// <summary>Địa chỉ Worker khai ở cột Address của đối tác (kèm https:// như seed kiểm tải).</summary>
    public const string Address = "https://127.0.0.1";

    private static readonly string[] SkippedHeaders = ["Host", "Content-Type", "Content-Length", "Connection", "Transfer-Encoding"];

    private readonly WebApplication _app;
    private readonly Func<HttpClient> _apiClientFactory;
    private readonly string _contentRoot;

    /// <summary>Yêu cầu đã nhận được qua HTTPS, theo đúng thứ tự nhận.</summary>
    public ConcurrentQueue<ReceivedRequest> Requests { get; } = new();

    /// <summary>Cổng hệ điều hành cấp khi mở cổng.</summary>
    public int Port { get; private set; }

    /// <summary>Việc chạy trước khi chuyển yêu cầu vào WebAPI, để test chèn thao tác vận hành đúng lúc một trang đang gửi dở.</summary>
    public Func<ReceivedRequest, Task>? BeforeForward { get; set; }

    /// <summary>Một yêu cầu đã nhận: đường dẫn kèm chuỗi truy vấn và mã đối tác ở header.</summary>
    public sealed record ReceivedRequest(string PathAndQuery, string? PartnerCode);

    private ShareDataHttpsReceiverMock(WebApplication app, Func<HttpClient> apiClientFactory, string contentRoot)
    {
        _app = app;
        _apiClientFactory = apiClientFactory;
        _contentRoot = contentRoot;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Mở cổng HTTPS trên cổng trống của 127.0.0.1; mọi yêu cầu chuyển vào WebAPI qua apiClientFactory.
    ///              Không đọc appsettings của thư mục chạy test để cấu hình Kestrel của WebAPI không chen vào.
    /// Created date: 11/11/2026
    /// </summary>
    public static async Task<ShareDataHttpsReceiverMock> StartAsync(Func<HttpClient> apiClientFactory)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"sharedata-https-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRoot);
        var options = new WebApplicationOptions { ContentRootPath = contentRoot, Args = [] };
        var builder = WebApplication.CreateSlimBuilder(options);
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();
        var certificate = CreateCertificate();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(certificate)));

        var app = builder.Build();
        var mock = new ShareDataHttpsReceiverMock(app, apiClientFactory, contentRoot);
        app.Run(mock.ForwardAsync);
        await app.StartAsync();

        var boundAddress = app.Urls.First();
        var boundUri = new Uri(boundAddress);
        mock.Port = boundUri.Port;
        return mock;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Ghi lại yêu cầu rồi chuyển nguyên vào WebAPI, trả lại đúng mã và thân phản hồi của WebAPI.
    /// Created date: 11/11/2026
    /// </summary>
    private async Task ForwardAsync(HttpContext context)
    {
        var request = context.Request;
        var pathAndQuery = $"{request.Path}{request.QueryString}";
        var partnerCode = request.Headers["PartnerCode"].FirstOrDefault();
        var received = new ReceivedRequest(pathAndQuery, partnerCode);
        Requests.Enqueue(received);
        if (BeforeForward != null)
        {
            await BeforeForward(received);
        }

        using var body = new MemoryStream();
        await request.Body.CopyToAsync(body);
        var bodyBytes = body.ToArray();
        var method = new HttpMethod(request.Method);
        using var forward = new HttpRequestMessage(method, pathAndQuery) { Content = new ByteArrayContent(bodyBytes) };
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            forward.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
        }

        foreach (var header in request.Headers)
        {
            if (SkippedHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var values = header.Value.ToArray();
            forward.Headers.TryAddWithoutValidation(header.Key, values);
        }

        using var client = _apiClientFactory();
        using var response = await client.SendAsync(forward);
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        context.Response.StatusCode = (int)response.StatusCode;
        var responseType = response.Content.Headers.ContentType;
        if (responseType != null)
        {
            context.Response.ContentType = responseType.ToString();
        }

        await context.Response.Body.WriteAsync(responseBytes);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Chứng chỉ tự ký cho CertificateHost, dùng cho xác thực máy chủ; nạp lại từ PKCS#12 để Kestrel dùng được khóa riêng.
    /// Created date: 11/11/2026
    /// </summary>
    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={CertificateHost}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var alternativeNames = new SubjectAlternativeNameBuilder();
        alternativeNames.AddDnsName(CertificateHost);
        var alternativeNameExtension = alternativeNames.Build();
        request.CertificateExtensions.Add(alternativeNameExtension);
        var serverAuth = new Oid("1.3.6.1.5.5.7.3.1");
        var usages = new OidCollection { serverAuth };
        var usageExtension = new X509EnhancedKeyUsageExtension(usages, false);
        request.CertificateExtensions.Add(usageExtension);

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = DateTimeOffset.UtcNow.AddDays(1);
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);
        var pkcs12 = certificate.Export(X509ContentType.Pkcs12);
        return X509CertificateLoader.LoadPkcs12(pkcs12, null);
    }

    /// <summary>Đóng cổng HTTPS và xóa thư mục gốc tạm.</summary>
    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        Directory.Delete(_contentRoot, recursive: true);
    }
}
