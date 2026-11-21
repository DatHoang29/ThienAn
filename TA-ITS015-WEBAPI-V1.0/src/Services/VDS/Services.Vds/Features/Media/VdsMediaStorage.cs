using System.Net;
using Services.Vds.Shared.Domain;
using Services.Vds.Configuration;

namespace Services.Vds.Features.Media;

/// <summary>
/// Author: KienLT
/// Description: Tải và lưu media theo QUY ƯỚC ĐƯỜNG DẪN suy từ (loại bản tin, thời điểm phát hiện, loại
/// media) — không có bảng media nào.
///
/// <para>
/// Bố cục: <c>{gốc}/{data|event}/{yyyyMM}/{ddHH}/{camera_ip}/{yyyyMMddHHmmssfff}_{suffix}{ext}</c>.
/// </para>
///
/// <para>
/// <b>Tầng <c>{camera_ip}</c> thêm ngày 28/08/2026.</b> Bên đọc dựng lại đường dẫn từ dữ liệu bản ghi nên
/// đây là HỢP ĐỒNG: đổi bố cục thì <c>Modules.TMS.Report</c> → <c>VdsImageService</c> phải đổi theo, và
/// file đã nằm trên đĩa theo bố cục cũ sẽ không tra ra nữa.
/// </para>
///
/// <para>
/// <b>Sửa lỗi của bản cũ</b>: ở đó đường dẫn tương đối được ghép sẵn một hằng số ĐƯỜNG DẪN TUYỆT ĐỐI của máy
/// lập trình viên, rồi mới <c>Path.Combine</c> với thư mục gốc trong cấu hình. Vì tham số thứ hai là đường
/// dẫn tuyệt đối, <c>Path.Combine</c> trả về chính nó ⇒ cấu hình thư mục gốc KHÔNG có tác dụng gì và mọi ảnh
/// đi vào đường dẫn cứng đó. Ở đây đường dẫn tương đối KHÔNG chứa thư mục gốc.
/// </para>
///
/// Luôn được gọi từ worker nền, không bao giờ chặn luồng xử lý bản tin.
/// Created date: 26/08/2026
/// </summary>
public sealed class VdsMediaStorage
{
    /// <summary>Tên HttpClient trong factory — handler dùng lại, không cạn socket khi tải liên tục.</summary>
    public const string HttpClientName = "vds-media";

    /// <summary>Nhánh lưu ảnh của lượt xe bình thường.</summary>
    public const string DataFolder = "data";

    /// <summary>Nhánh lưu ảnh của mọi loại sự cố.</summary>
    public const string EventFolder = "event";

    private const int CopyBufferSize = 64 * 1024;

    private readonly IHttpClientFactory _factory;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<VdsMediaStorage> _logger;
    private readonly Modules.VDS.Core.Services.Media.VdsImageShareAccess? _shareAccess;

    /// <summary>Đã cảnh báo lỗi xác thực chưa — xem <see cref="EnsureAuthorized"/>.</summary>
    private bool _authWarned;

    public VdsMediaStorage(
        IHttpClientFactory factory, IVdsConfigManager config, ILogger<VdsMediaStorage> logger,
        Modules.VDS.Core.Services.Media.VdsImageShareAccess? shareAccess = null)
    {
        _factory = factory;
        _config = config;
        _logger = logger;
        _shareAccess = shareAccess;
    }

    /// <summary>
    /// Suffix tên file theo loại media. Sáu loại của đặc tả phải cho ra sáu suffix KHÁC NHAU: bản cũ từng để
    /// <c>violation</c> rơi vào nhánh "lấy chữ cái đầu" nên cũng ra <c>v</c> và GHI ĐÈ đúng ảnh
    /// <c>vehicle</c> của cùng bản tin (cùng thời điểm, cùng camera ⇒ cùng tên file).
    ///
    /// Ba suffix <c>v</c>/<c>p</c>/<c>f</c> giữ nguyên vì giao diện đang dựng lại đường dẫn theo quy ước này
    /// và trên đĩa đã có file.
    /// </summary>
    public static string SuffixOf(string? kind) => (kind ?? "").Trim().ToLowerInvariant() switch
    {
        "vehicle" => "v",
        "plate" => "p",
        "full" => "f",
        "violation" => "vi",
        "clip" => "cl",
        "snapshot" => "sn",
        "" => "x",
        var other => Fallback(other),
    };

    /// <summary>
    /// Loại lạ (thiết bị thêm loại mới): lấy tối đa 3 ký tự chữ/số đầu. Dài hơn một ký tự nên KHÔNG đụng ba
    /// suffix một ký tự đang dùng.
    /// </summary>
    private static string Fallback(string kind)
    {
        var chars = kind.Where(char.IsLetterOrDigit).Take(3).ToArray();
        return chars.Length == 0 ? "x" : new string(chars).ToLowerInvariant();
    }

    /// <summary>
    /// Đuôi file suy từ kiểu nội dung của phản hồi.
    ///
    /// Bắt buộc phải suy từ đó chứ không từ URL: đường dẫn media của đặc tả KHÔNG có đuôi, nên suy từ URL sẽ
    /// luôn ra <c>.jpeg</c> và đặt sai tên cho loại <c>clip</c> (video).
    /// </summary>
    public static string ExtensionFromMime(string? mime, string fallback = ".jpeg")
        => (mime ?? "").Trim().ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpeg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            "video/mp4" => ".mp4",
            "video/mpeg" => ".mpeg",
            "video/x-msvideo" => ".avi",
            "video/quicktime" => ".mov",
            "application/octet-stream" or "" => fallback,
            var other => GuessFromSubtype(other, fallback),
        };

    private static string GuessFromSubtype(string mime, string fallback)
    {
        var slash = mime.IndexOf('/');
        if (slash < 0 || slash == mime.Length - 1) return fallback;

        var subtype = new string(mime[(slash + 1)..].TakeWhile(char.IsLetterOrDigit).ToArray());
        return subtype.Length == 0 ? fallback : "." + subtype;
    }

    /// <summary>
    /// Chọn nhánh lưu theo loại bản tin: lượt xe bình thường vào nhánh dữ liệu, mọi loại khác vào nhánh sự cố.
    ///
    /// Quy tên loại về bộ mã hiện hành TRƯỚC khi so: payload phiên bản cũ gửi tên loại khác cho cùng một ý
    /// nghĩa, so chuỗi thô thì ảnh lượt xe bình thường rơi sang nhánh sự cố mà không có lỗi nào để thấy.
    ///
    /// Loại rỗng/không rõ coi như sự cố: ảnh nằm ở nhánh sự cố vẫn đọc được, còn lẫn vào nhánh dữ liệu thì
    /// trộn với ảnh của luồng đếm lưu lượng.
    /// </summary>
    public static string FolderOf(string? eventType)
        => Modules.VDS.Core.Helper.VdsBaseHelper.MediaPathHp.Branch(eventType);

    /// <summary>
    /// Đường dẫn TƯƠNG ĐỐI (dưới thư mục gốc) của một media — tất định theo (loại bản tin, thời điểm, camera,
    /// loại media) nên bên đọc dựng lại được mà không cần bảng media.
    ///
    /// <para><b>Camera nằm ở một TẦNG THƯ MỤC RIÊNG</b>, không nhập vào tên file: một thư mục giờ trên tuyến
    /// 10 camera có thể chứa hàng nghìn file, tách theo camera thì liệt kê và dọn theo camera mới làm được.
    /// Camera rỗng (chưa tra được) đi vào <see cref="UnknownCameraFolder"/> thay vì đổ chung vào tầng giờ —
    /// trộn vào đó thì không phân biệt được ảnh chưa biết camera với ảnh của bố cục cũ.</para>
    /// </summary>
    public static string BuildRelativePath(
        DateTime detectTime, string? eventType, string? cameraIp, string? kind, string ext = ".jpeg")
    {
        var stem = detectTime.ToString("yyyyMMddHHmmssfff");
        var extension = string.IsNullOrEmpty(ext)
            ? ".jpeg"
            : ext.StartsWith('.') ? ext : "." + ext;

        return $"{FolderOf(eventType)}/{detectTime:yyyyMM}/{detectTime:ddHH}"
             + $"/{CameraFolderOf(cameraIp)}/{stem}_{SuffixOf(kind)}{extension}";
    }

    /// <summary>Thư mục dùng khi bản tin chưa tra được camera.</summary>
    public const string UnknownCameraFolder = "unknown";

    /// <summary>
    /// Tên thư mục của một camera, làm sạch để dùng được làm MỘT đoạn đường dẫn.
    ///
    /// <para>Giá trị vào đây là <c>TmsEquipment.Ip</c> của danh mục (mapper gán từ camera đã tra được), không
    /// phải chuỗi thô của thiết bị. Vẫn làm sạch vì đây là chỗ chuỗi biến thành đường dẫn trên đĩa: chỉ giữ
    /// chữ, số, dấu chấm, gạch ngang và gạch dưới, còn lại đổi thành <c>_</c>. Nhờ vậy dấu phân cách hay
    /// <c>..</c> không thể trèo ra khỏi thư mục gốc dù danh mục có bị khai sai.</para>
    /// </summary>
    public static string CameraFolderOf(string? cameraIp)
    {
        return Modules.VDS.Core.Helper.VdsBaseHelper.MediaPathHp.CameraFolder(cameraIp);
    }

    /// <summary>
    /// Tải và lưu MỘT media. Trả false nếu thất bại (đã log). Không ném lỗi ra ngoài trừ khi bị huỷ.
    ///
    /// Ghi ra file <c>.tmp</c> rồi ĐỔI TÊN sang file đích: tiến trình dừng giữa lúc tải thì không để lại một
    /// file ảnh dở mà bên đọc tưởng là ảnh thật.
    /// </summary>
    public async Task<bool> SaveOneAsync(VdsMediaJob job, CancellationToken ct)
    {
        var runtime = _config.Current;
        try
        {
            return _shareAccess is null ? await SaveOneCoreAsync(job, runtime, ct)
                : await _shareAccess.RunAsync(runtime.Media.ResolveRoot(), () => SaveOneCoreAsync(job, runtime, ct), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Media] Cannot access configured VDS storage for {Vendor}/{Kind}.", job.VendorEventId, job.Kind);
            return false;
        }
    }

    private async Task<bool> SaveOneCoreAsync(VdsMediaJob job, ServiceConfig runtime, CancellationToken ct)
    {
        var options = runtime.Media;
        var ingestion = runtime.Ingestion;

        var url = AbsoluteUrl(job.Url, ingestion.ApiBaseUrl);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsTrustedOrigin(uri, ingestion.ApiBaseUrl)
            || (uri.Scheme == "http" && !ingestion.AllowInsecureHttp))
        {
            _logger.LogWarning("[Media] Bỏ media của bản tin {Vendor}: URL không hợp lệ '{Url}'.",
                job.VendorEventId, job.Url);
            return false;
        }

        var tmp = "";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Cùng một chỗ gắn header xác thực với luồng SSE và API lịch sử: đổi kiểu xác thực thì cả ba
            // đường đổi theo.
            ingestion.ApplyAuth(request);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.EffectiveDownloadTimeoutSeconds));

            using var response = await _factory
                .CreateClient(HttpClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);

            if (!EnsureAuthorized(response, job)) return false;
            response.EnsureSuccessStatusCode();

            // Đuôi file phải suy SAU khi có phản hồi; vẫn lùi về đuôi trong URL nếu server không khai kiểu.
            var urlExtension = Path.GetExtension(uri.AbsolutePath);
            var extension = ExtensionFromMime(
                response.Content.Headers.ContentType?.MediaType,
                string.IsNullOrEmpty(urlExtension) ? ".jpeg" : urlExtension);

            var relative = BuildRelativePath(
                job.DetectTime, job.EventType, job.CameraIp, job.Kind, extension);
            var absolute = Path.Combine(
                options.ResolveRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

            var maxBytes = options.EffectiveMaxBytes;

            // Kiểm kích thước KHAI TRƯỚC khi tạo thư mục và mở file: không tạo rác cho file sẽ bị bỏ.
            if (response.Content.Headers.ContentLength is long declared && declared > maxBytes)
            {
                _logger.LogWarning(
                    "[Media] Bỏ file quá lớn ({Size} byte > {Max}) của bản tin {Vendor}, loại {Kind}.",
                    declared, maxBytes, job.VendorEventId, job.Kind);
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
            tmp = absolute + "." + Guid.NewGuid().ToString("N") + ".tmp";

            await using (var source = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false))
            await using (var destination = new FileStream(
                             tmp, FileMode.Create, FileAccess.Write, FileShare.None,
                             CopyBufferSize, useAsync: true))
            {
                var buffer = new byte[CopyBufferSize];
                long total = 0;
                int read;

                // Vẫn phải đếm khi đang tải: server có thể KHÔNG khai độ dài, lúc đó phép kiểm ở trên vô hiệu
                // và một file khổng lồ sẽ ghi hết vào đĩa.
                while ((read = await source.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > maxBytes)
                    {
                        _logger.LogWarning(
                            "[Media] Bỏ file vượt {Max} byte khi đang tải (bản tin {Vendor}, loại {Kind}).",
                            maxBytes, job.VendorEventId, job.Kind);
                        return false;
                    }

                    await destination.WriteAsync(buffer.AsMemory(0, read), timeout.Token).ConfigureAwait(false);
                }
            }

            File.Move(tmp, absolute, overwrite: true);   // đổi tên nguyên tử
            tmp = "";

            _logger.LogDebug("[Media] Đã lưu {Relative} (bản tin {Vendor}, loại {Kind}).",
                relative, job.VendorEventId, job.Kind);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Media] Tải media của bản tin {Vendor}, loại {Kind} thất bại; bỏ qua.",
                job.VendorEventId, job.Kind);
            return false;
        }
        finally
        {
            // Dọn file tạm nếu chưa kịp đổi tên.
            if (tmp.Length > 0)
            {
                try
                {
                    if (File.Exists(tmp)) File.Delete(tmp);
                }
                catch
                {
                    // Không xoá được file tạm thì cũng không có gì làm thêm; lần tải sau sẽ ghi đè.
                }
            }
        }
    }

    /// <summary>
    /// Nói rõ khi lỗi là do XÁC THỰC chứ không do mạng: media dùng cùng khoá với luồng SSE, nên bị từ chối ở
    /// đây nghĩa là khoá sai hoặc thiếu quyền đọc — hàng nghìn dòng "tải thất bại" không giúp gì. Log MỘT lần
    /// cho mỗi tiến trình để không ngập log khi hàng đợi còn nhiều việc.
    /// </summary>
    private bool EnsureAuthorized(HttpResponseMessage response, VdsMediaJob job)
    {
        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)) return true;

        if (!_authWarned)
        {
            _authWarned = true;
            _logger.LogError(
                "[Media] Máy chủ trả {Status} khi tải media (bản tin {Vendor}) — khoá sai hoặc thiếu quyền " +
                "đọc sự kiện. Mọi lượt tải sẽ thất bại cho tới khi khai lại khoá.",
                (int)response.StatusCode, job.VendorEventId);
        }

        return false;
    }

    /// <summary>URL tương đối của thiết bị được ghép với địa chỉ gốc của API.</summary>
    public static string AbsoluteUrl(string url, string? apiBaseUrl)
    {
        var value = (url ?? "").Trim();
        if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return value;

        return (apiBaseUrl ?? "").TrimEnd('/') + "/" + value.TrimStart('/');
    }

    public static bool IsTrustedOrigin(Uri uri, string? apiBaseUrl) =>
        uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo)
        && Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var origin)
        && uri.Scheme == origin.Scheme && uri.IdnHost == origin.IdnHost && uri.Port == origin.Port;
}
