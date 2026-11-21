using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;

namespace ITS.VMS.Core.Integration.Helpers
{
    /// <summary>
    /// Lấy file ảnh về dạng đường dẫn cục bộ để đưa vào SDK thiết bị. Hỗ trợ 2 môi trường triển khai:
    ///   - CHUNG máy chủ với BE: BE gửi đường dẫn tương đối (vd "\Upload\Vms\...") -> ghép với Nats:StorageBasePath.
    ///   - KHÁC máy chủ: BE gửi HTTP URL -> tải về thư mục tạm (Nats:TempImageDir) trước khi dùng.
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public sealed class VmsFileHelper
    {
        private readonly VmsControlNatsOption _options;
        private readonly VmsLogger _logger;
        private readonly HttpClient _http;

        public VmsFileHelper(VmsControlNatsOption options, VmsLogger logger, HttpClient http)
        {
            _options = options;
            _logger = logger;
            _http = http;
        }

        /// <summary>Thư mục tạm (tuyệt đối) chứa ảnh tải qua HTTP.</summary>
        private string TempDir => ResolveFromBase(_options.TempImageDir, "temp/vms_image");

        /// <summary>
        /// Lấy file ảnh cục bộ để đưa vào SDK, theo thứ tự ưu tiên:
        ///   1. <paramref name="base64"/> có giá trị -> giải mã, ghi file tạm. KHÔNG đi tìm ảnh ở đâu cả.
        ///   2. <paramref name="url"/> là HTTP     -> tải về file tạm.
        ///   3. <paramref name="url"/> tương đối   -> ghép Nats:StorageBasePath (chỉ chạy khi chung máy với BE).
        ///
        /// Trả về (đường dẫn file, có phải file tạm không). Cờ IsTemp để người gọi biết file nào
        /// được phép xoá sau khi gửi — file lấy từ StorageBasePath là file THẬT của BE, tuyệt đối không xoá.
        /// Ném exception khi không lấy được file; người gọi bắt và ghi vào Message của thiết bị đó.
        /// </summary>
        public async Task<(string Path, bool IsTemp)> ResolveAsync(string? base64, string? format, string? url, CancellationToken ct)
        {
            // Ưu tiên cao nhất: ảnh nằm ngay trong gói tin -> không phụ thuộc đường dẫn/máy chủ.
            if (!string.IsNullOrWhiteSpace(base64))
                return (WriteBase64(base64, format), true);

            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("Yêu cầu không có hình ảnh: cả imageBase64 và url đều rỗng.");

            return (await ResolveUrlAsync(url, ct).ConfigureAwait(false), IsHttp(url));
        }

        /// <summary>
        /// Giải mã base64 ra file tạm. Convert.FromBase64String giữ nguyên từng byte nên header 54 byte
        /// và bit depth của BMP được bảo toàn y hệt file gốc bên BE.
        /// </summary>
        private string WriteBase64(string base64, string? format)
        {
            Directory.CreateDirectory(TempDir);

            var ext = SafeExtension(format);
            var name = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}".Substring(0, 26) + "." + ext;
            var filePath = Path.Combine(TempDir, name);

            File.WriteAllBytes(filePath, Convert.FromBase64String(base64));
            return filePath;
        }

        /// <summary>
        /// Đuôi file lấy từ gói tin chỉ được gồm chữ/số, tối đa 5 ký tự; sai thì dùng "bmp".
        /// Chặn trường hợp ImageFormat chứa "\" hoặc ".." làm file tạm bị ghi ra ngoài TempDir.
        /// </summary>
        private static string SafeExtension(string? format)
        {
            var ext = format?.Trim().TrimStart('.').ToLowerInvariant();
            return !string.IsNullOrEmpty(ext) && ext.Length <= 5 && ext.All(char.IsAsciiLetterOrDigit) ? ext : "bmp";
        }

        private static bool IsHttp(string url) =>
            url.TrimStart().StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.TrimStart().StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        /// <summary>Đường dự phòng: lấy ảnh theo đường dẫn (HTTP hoặc thư mục dùng chung với BE).</summary>
        private async Task<string> ResolveUrlAsync(string url, CancellationToken ct)
        {
            url = url.Trim();

            // 1. HTTP/HTTPS -> tải về thư mục tạm.
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return await DownloadAsync(url, ct).ConfigureAwait(false);
            }

            // 2. Đường dẫn tuyệt đối sẵn (dịch vụ chạy cùng máy, BE gửi full path).
            // IsPathFullyQualified: "\Upload\..." của BE là đường dẫn tương đối theo WebRoot, không phải gốc ổ đĩa.
            if (Path.IsPathFullyQualified(url) && File.Exists(url))
                return url;

            // 3. Đường dẫn tương đối -> ghép với StorageBasePath.
            if (string.IsNullOrWhiteSpace(_options.StorageBasePath))
                throw new InvalidOperationException(
                    $"Đường dẫn ảnh '{url}' là tương đối nhưng chưa cấu hình Nats:StorageBasePath. " +
                    "Cấu hình thư mục lưu trữ dùng chung, hoặc để BE gửi HTTP URL.");

            var relative = url.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var basePath = Path.GetFullPath(_options.StorageBasePath.Trim());
            var full = Path.GetFullPath(Path.Combine(basePath, relative));

            // Chặn "..": đường dẫn tương đối phải nằm trong thư mục lưu trữ dùng chung.
            var baseWithSep = Path.EndsInDirectorySeparator(basePath) ? basePath : basePath + Path.DirectorySeparatorChar;
            if (!full.StartsWith(baseWithSep, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Đường dẫn ảnh '{url}' nằm ngoài Nats:StorageBasePath.");

            if (!File.Exists(full))
                throw new FileNotFoundException($"Không tìm thấy file ảnh: {full}");

            return full;
        }

        /// <summary>Tải ảnh qua HTTP về thư mục tạm, trả về đường dẫn file vừa lưu.</summary>
        private async Task<string> DownloadAsync(string url, CancellationToken ct)
        {
            Directory.CreateDirectory(TempDir);

            var ext = Path.GetExtension(new Uri(url).LocalPath);
            if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".bmp";

            // Tên file gồm mốc thời gian + 8 ký tự guid: tránh 2 thiết bị tải trùng tên khi chạy song song.
            var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}".Substring(0, 26) + ext;
            var filePath = Path.Combine(TempDir, fileName);

            using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Tải ảnh thất bại ({(int)response.StatusCode}) từ {url}");

            await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs, ct).ConfigureAwait(false);

            return filePath;
        }

        /// <summary>
        /// Xoá file tạm sau khi gửi xong. CHỈ gọi cho file do ResolveAsync tạo ra (IsTemp = true);
        /// gọi cho file lấy từ StorageBasePath sẽ xoá mất ảnh gốc của BE.
        /// Không ném exception — xoá được hay không cũng không ảnh hưởng kết quả gửi thiết bị.
        /// </summary>
        public void DeleteTemp(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch (Exception ex)
            {
                // File còn bị SDK giữ handle -> để CleanupTemp lần khởi động sau dọn.
                _logger.Warning(LogCategory.Config, $"[File] Chưa xoá được ảnh tạm '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Dọn file ảnh tạm cũ hơn TempImageRetainHours. Gọi 1 lần lúc khởi động để thư mục tạm không phình đĩa.
        /// </summary>
        public void CleanupTemp()
        {
            try
            {
                if (!Directory.Exists(TempDir)) return;

                var limit = DateTime.Now.AddHours(-Math.Max(1, _options.TempImageRetainHours));
                var removed = 0;

                foreach (var file in Directory.EnumerateFiles(TempDir))
                {
                    try
                    {
                        if (File.GetLastWriteTime(file) >= limit) continue;
                        File.Delete(file);
                        removed++;
                    }
                    catch { /* file đang bị khóa -> để lần sau */ }
                }

                if (removed > 0)
                    _logger.Info(LogCategory.Config, $"[File] Đã dọn {removed} ảnh tạm cũ tại {TempDir}.");
            }
            catch (Exception ex)
            {
                _logger.Warning(LogCategory.Config, $"[File] Dọn ảnh tạm lỗi: {ex.Message}");
            }
        }

        /// <summary>Chuẩn hóa đường dẫn tương đối về tuyệt đối tính từ thư mục chạy exe.</summary>
        private static string ResolveFromBase(string? path, string fallback)
        {
            var value = string.IsNullOrWhiteSpace(path) ? fallback : path.Trim();
            return Path.IsPathRooted(value) ? value : Path.Combine(AppContext.BaseDirectory, value);
        }
    }
}
